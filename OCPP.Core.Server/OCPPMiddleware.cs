using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library;
using Serilog;
using Serilog.Events;

namespace OCPP.Core.Server
{
    public partial class OcppMiddleware
    {
        // Supported OCPP protocols (in order)
        private const string ProtocolOcpp16 = "ocpp1.6";
        private const string ProtocolOcpp20 = "ocpp2.0";
        private static readonly string[] SupportedProtocols = { ProtocolOcpp20, ProtocolOcpp16 /*, "ocpp1.5" */};

        // RegExp for splitting ocpp message parts
        // ^\[\s*(\d)\s*,\s*\"([^"]*)\"\s*,(?:\s*\"(\w*)\"\s*,)?\s*(.*)\s*\]$
        // Third block is optional, because responses don't have an action
        private const string MessageRegExp = "^\\[\\s*(\\d)\\s*,\\s*\"([^\"]*)\"\\s*,(?:\\s*\"(\\w*)\"\\s*,)?\\s*(.*)\\s*\\]$";

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly OcppCoreContext _dbContext;
        // Dictionary with status objects for each charge point
        
        
        private static readonly Dictionary<string, ChargePointStatus> ChargePointStatusDict = new Dictionary<string, ChargePointStatus>();

        // Dictionary for processing asynchronous API calls
        private readonly Dictionary<string, OcppMessage> _requestQueue = new Dictionary<string, OcppMessage>();
        

        public OcppMiddleware(RequestDelegate next, ILogger logger, IConfiguration configuration,IServiceProvider serviceProvider,OcppCoreContext dbContext)
    
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _dbContext = dbContext;
        }

        public async Task Invoke(HttpContext context , OcppAuth auth)
        {
            if(_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Verbose("OCPPMiddleware => Websocket request: Path='{Path}'", context.Request.Path);

            if (context.Request.Path.StartsWithSegments("/OCPP"))
            {
                
                string?[] parts = context.Request.Path.Value?.Split('/')??Array.Empty<string>();
                var chargepointIdentifier = GetChargePointIdentifierFromContext(parts);
                if(string.IsNullOrEmpty(chargepointIdentifier)) {
                    await context.Response.WriteAsync("Invalid chargepoint identifier");
                    return;
                }
                
                
                
                var authHeader = context.Request.Headers["Authorization"].ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(authHeader) || true)
                {
                    //Disabled for testing
                    //var cred = Encoding.ASCII.GetString(Convert.FromBase64String(authHeader[6..])).Split(':');
                    var clientCert = context.Connection.ClientCertificate;
                    
                    //var chargePoint = auth.Authenticate(chargepointIdentifier,cred[0] ,cred[1], clientCert);
                    var chargePoint = auth.Authenticate(chargepointIdentifier,"dfg" ,"dfg", clientCert);
                    if (chargePoint == null)
                    {
                        _logger.Warning("OCPPMiddleware => Authentication Failure found no chargepoint or the credentials did not match for identifier  identifier :  '{ChargepointIdentifier}'", chargepointIdentifier);
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    

                    var chargePointStatus = new ChargePointStatus(chargePoint);
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        // Match supported sub protocols
                        var subProtocol = context.WebSockets.WebSocketRequestedProtocols.FirstOrDefault(p => SupportedProtocols.Contains(p));
                     
                        
                        if (string.IsNullOrEmpty(subProtocol))
                        {
                            // Not matching protocol! => failure
                            var protocols = string.Empty;
                            foreach (var p in context.WebSockets.WebSocketRequestedProtocols)
                            {
                                if (string.IsNullOrEmpty(protocols)) protocols += ",";
                                protocols += p;
                            }
                            _logger.Warning("OCPPMiddleware => No supported sub-protocol in '{Protocols}' from charge station '{ChargepointIdentifier}'", protocols, chargepointIdentifier);
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return;
                        }
                       
                        chargePointStatus.Protocol = subProtocol;

                        bool statusSuccess = false;
                        try
                        {
                            _logger.Verbose("OCPPMiddleware => Store/Update status object");

                            lock (ChargePointStatusDict)
                            {
                                // Check if this chargepoint already/still hat a status object
                                if (ChargePointStatusDict.ContainsKey(chargepointIdentifier))
                                {
                                    // exists => check status
                                    if (ChargePointStatusDict[chargepointIdentifier].WebSocket.State != WebSocketState.Open)
                                    {
                                        // Closed or aborted => remove
                                        ChargePointStatusDict.Remove(chargepointIdentifier);
                                    }
                                }

                                ChargePointStatusDict.Add(chargepointIdentifier, chargePointStatus);
                                statusSuccess = true;
                            }
                        }
                        catch(Exception exp)
                        {
                            _logger.Error(exp, "OCPPMiddleware => Error storing status object in dictionary => refuse connection");
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        }

                        if (statusSuccess)
                        {
                            // Handle socket communication
                            _logger.Verbose("OCPPMiddleware => Waiting for message...");

                            using var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol);
                            _logger.Verbose("OCPPMiddleware => WebSocket connection with charge point '{ChargePointIdentifier}'", chargepointIdentifier);
                            chargePointStatus.WebSocket = webSocket;

                            if (subProtocol == ProtocolOcpp20)
                            {
                                // OCPP V2.0
                                await Receive20(chargePointStatus);
                            }
                            else
                            {
                                // OCPP V1.6
                                await Receive16(chargePointStatus);
                            }
                        }
                    }
                    else
                    {
                        // no websocket request => failure
                        _logger.Warning("OCPPMiddleware => Non-Websocket request");
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }

            }
            else if (context.Request.Path.StartsWithSegments("/API"))
            {
                // Check authentication (X-API-Key)
                var apiKeyConfig = _configuration.GetValue<string>("ApiKey");
                if (!string.IsNullOrWhiteSpace(apiKeyConfig))
                {
                    // ApiKey specified => check request
                    var apiKeyCaller = context.Request.Headers["X-API-Key"].FirstOrDefault();
                    if (apiKeyConfig == apiKeyCaller)
                    {
                        // API-Key matches
                        _logger.Information("OCPPMiddleware => Success: X-API-Key matches");
                    }
                    else
                    {
                        // API-Key does NOT matches => authentication failure!!!
                        _logger.Warning("OCPPMiddleware => Failure: Wrong X-API-Key! Caller='{ApiKeyCaller}'", apiKeyCaller);
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        return;
                    }
                }
                else
                {
                    // No API-Key configured => no authentication
                    _logger.Warning("OCPPMiddleware => No X-API-Key configured!");
                }

                // format: /API/<command>[/chargepointId]
                string?[] urlParts = context.Request.Path.Value!.Split('/');

                if (urlParts.Length >= 3)
                {
                    var cmd = urlParts[2];
                    var urlChargePointId = (urlParts.Length >= 4) ? urlParts[3] : null;
                    _logger.Verbose("OCPPMiddleware => cmd='{Cmd}' / id='{UrlChargePointId}' / FullPath='{FullPath}')", cmd, urlChargePointId, context.Request.Path.Value);

                    switch (cmd)
                    {
                        case "Status":
                            try
                            {
                                var statusList = new List<ChargePointStatus?>();
                                lock (ChargePointStatusDict)
                                {
                                    statusList.AddRange(ChargePointStatusDict.Values);
                                }
                                var jsonStatus = JsonConvert.SerializeObject(statusList);
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync(jsonStatus);
                            }
                            catch (Exception exp)
                            {
                                _logger.Error(exp, "OCPPMiddleware => Error: {Message}", exp.Message);
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            }

                            break;
                        case "Reset" when !string.IsNullOrEmpty(urlChargePointId):
                            try
                            {
                                ChargePointStatus? status;
                                var valueFound = false;        
                                lock (ChargePointStatusDict)
                                {
                                    if (ChargePointStatusDict.TryGetValue(urlChargePointId, out status))
                                    {
                                        valueFound = true;
                                    }
                                }

                                if (valueFound && status !=null)
                                {
                                    // Send message to chargepoint
                                    if (status.Protocol == ProtocolOcpp20)
                                    {
                                        // OCPP V2.0
                                        await Reset20(status, context);
                                    }
                                    else
                                    {
                                        // OCPP V1.6
                                        await Reset16(status, context);
                                    }
                                }else
                                {
                                    // Chargepoint offline
                                    _logger.Error("OCPPMiddleware SoftReset => Chargepoint offline: {UrlChargePointId}", urlChargePointId);
                                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                }

                                
                            }
                            catch (Exception exp)
                            {
                                _logger.Error(exp, "OCPPMiddleware SoftReset => Error: {Message}", exp.Message);
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            }

                            break;
                        case "Reset":
                            _logger.Error("OCPPMiddleware SoftReset => Missing chargepoint ID");
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            break;
                        case "UnlockConnector" when !string.IsNullOrEmpty(urlChargePointId):
                            try
                            {
                                lock (ChargePointStatusDict)
                                {
                                    if (ChargePointStatusDict.TryGetValue(urlChargePointId, out var status))
                                    {
                                        // Send message to chargepoint
                                        if (status.Protocol == ProtocolOcpp20)
                                        {
                                            // OCPP V2.0
                                            UnlockConnector20(status, context);
                                        }
                                        else
                                        {
                                            // OCPP V1.6
                                            UnlockConnector16(status, context);
                                        }
                                    }
                                    else
                                    {
                                        // Chargepoint offline
                                        _logger.Error("OCPPMiddleware UnlockConnector => Chargepoint offline: {UrlChargePointId}", urlChargePointId);
                                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                _logger.Error(exp, "OCPPMiddleware UnlockConnector => Error: {Message}", exp.Message);
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            }

                            break;
                        case "UnlockConnector":
                            _logger.Error("OCPPMiddleware UnlockConnector => Missing chargepoint ID");
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            break;
                        default:
                            // Unknown action/function
                            _logger.Warning("OCPPMiddleware => action/function: {Cmd}", cmd);
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            break;
                    }
                }
            }
            else if (context.Request.Path.StartsWithSegments("/"))
            {
                try
                {
                    bool showIndexInfo = _configuration.GetValue<bool>("ShowIndexInfo");
                    if (showIndexInfo)
                    {
                        _logger.Verbose("OCPPMiddleware => Index status page");

                        context.Response.ContentType = "text/plain";
                        lock (ChargePointStatusDict)
                        { 
                            context.Response.WriteAsync(
                                $"Running...\r\n\r\n{ChargePointStatusDict.Values.Count} chargePoints connected").Wait();
                        }
                    }
                    else
                    {
                        _logger.Information("OCPPMiddleware => Root path with deactivated index page");
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                catch (Exception exp)
                {
                    _logger.Error(exp, "OCPPMiddleware => Error: {Message}", exp.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
            }
            else
            {
                _logger.Warning("OCPPMiddleware => Bad path request");
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        private string? GetChargePointIdentifierFromContext(string?[] parts)
        {
            if(parts.Length < 3)return null;
            
            var chargepointIdentifier = string.IsNullOrWhiteSpace(parts[^1]) ? parts[^2] : parts[^1];
            // Known chargepoint?
            return string.IsNullOrWhiteSpace(chargepointIdentifier) ? null : chargepointIdentifier;
        }

        private async void DumpLog(byte[] bMessage)
        {
            var dumpDir = _configuration.GetValue<string>("MessageDumpDir");
            if (string.IsNullOrWhiteSpace(dumpDir)) return;
            var path = Path.Combine(dumpDir,
                $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-ffff}_ocpp16-in.txt");
            try
            {
                // Write incoming message into dump directory
                await File.WriteAllBytesAsync(path, bMessage);
            }
            catch (Exception exp)
            {
                _logger.Error(exp, "OCPPMiddleware.Receive16 => Error dumping incoming message to path: '{Path}'", path);
            }
        }
    }

   
    public static class OcppMiddlewareExtensions
    {
        public static IApplicationBuilder UseOcppMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OcppMiddleware>();
        }
    }
}
