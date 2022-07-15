using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
        private static string _messageRegExp = "^\\[\\s*(\\d)\\s*,\\s*\"([^\"]*)\"\\s*,(?:\\s*\"(\\w*)\"\\s*,)?\\s*(.*)\\s*\\]$";

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        // Dictionary with status objects for each charge point
        
        
        private static Dictionary<string, ChargePointStatus> _chargePointStatusDict = new Dictionary<string, ChargePointStatus>();

        // Dictionary for processing asynchronous API calls
        private Dictionary<string, OcppMessage> _requestQueue = new Dictionary<string, OcppMessage>();

        public OcppMiddleware(RequestDelegate next, ILogger logger, IConfiguration configuration,IServiceProvider serviceProvider)
    
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task Invoke(HttpContext context , OcppAuth auth)
        {
            if(_logger.IsEnabled(LogEventLevel.Debug))
                _logger.Verbose("OCPPMiddleware => Websocket request: Path='{Path}'", context.Request.Path);

            if (context.Request.Path.StartsWithSegments("/OCPP"))
            {
                
                string?[]? parts = context.Request.Path.Value?.Split('/')??Array.Empty<string>();
                string? chargepointIdentifier = GetChargePointIdentifierFromContext(parts);
                if(string.IsNullOrEmpty(chargepointIdentifier)) {
                    await context.Response.WriteAsync("Invalid chargepoint identifier");
                    return;
                }
                
                
                
                string authHeader = context.Request.Headers["Authorization"].ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(authHeader))
                {
                    string[] cred = Encoding.ASCII.GetString(Convert.FromBase64String(authHeader.Substring(6))).Split(':');
                    X509Certificate2? clientCert = context.Connection.ClientCertificate;
                    ChargePoint? chargePoint = auth.Authenticate(chargepointIdentifier,cred[0] ,cred[1], clientCert);
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
                        string? subProtocol = context.WebSockets.WebSocketRequestedProtocols.FirstOrDefault(p => SupportedProtocols.Contains(p));
                     
                        
                        if (string.IsNullOrEmpty(subProtocol))
                        {
                            // Not matching protocol! => failure
                            string protocols = string.Empty;
                            foreach (string p in context.WebSockets.WebSocketRequestedProtocols)
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

                            lock (_chargePointStatusDict)
                            {
                                // Check if this chargepoint already/still hat a status object
                                if (_chargePointStatusDict.ContainsKey(chargepointIdentifier))
                                {
                                    // exists => check status
                                    if (_chargePointStatusDict[chargepointIdentifier]!.WebSocket.State != WebSocketState.Open)
                                    {
                                        // Closed or aborted => remove
                                        _chargePointStatusDict.Remove(chargepointIdentifier);
                                    }
                                }

                                _chargePointStatusDict.Add(chargepointIdentifier, chargePointStatus);
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

                            using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol))
                            {
                                _logger.Verbose("OCPPMiddleware => WebSocket connection with charge point '{ChargePointIdentifier}'", chargepointIdentifier);
                                chargePointStatus.WebSocket = webSocket;

                                if (subProtocol == ProtocolOcpp20)
                                {
                                    // OCPP V2.0
                                    await Receive20(chargePointStatus, context);
                                }
                                else
                                {
                                    // OCPP V1.6
                                    await Receive16(chargePointStatus, context);
                                }
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
                string apiKeyConfig = _configuration.GetValue<string>("ApiKey");
                if (!string.IsNullOrWhiteSpace(apiKeyConfig))
                {
                    // ApiKey specified => check request
                    string? apiKeyCaller = context.Request.Headers["X-API-Key"].FirstOrDefault();
                    if (apiKeyConfig == apiKeyCaller)
                    {
                        // API-Key matches
                        _logger.Information("OCPPMiddleware => Success: X-API-Key matches");
                    }
                    else
                    {
                        // API-Key does NOT matches => authentication failure!!!
                        _logger.Warning("OCPPMiddleware => Failure: Wrong X-API-Key! Caller='{0}'", apiKeyCaller);
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        return;
                    }
                }
                else
                {
                    // No API-Key configured => no authenticatiuon
                    _logger.Warning("OCPPMiddleware => No X-API-Key configured!");
                }

                // format: /API/<command>[/chargepointId]
                string?[] urlParts = context.Request.Path.Value!.Split('/');

                if (urlParts.Length >= 3)
                {
                    string? cmd = urlParts[2];
                    string? urlChargePointId = (urlParts.Length >= 4) ? urlParts[3] : null;
                    _logger.Verbose("OCPPMiddleware => cmd='{0}' / id='{1}' / FullPath='{2}')", cmd, urlChargePointId, context.Request.Path.Value);

                    if (cmd == "Status")
                    {
                        try
                        {
                            List<ChargePointStatus?> statusList = new List<ChargePointStatus?>();
                            foreach (ChargePointStatus? status in _chargePointStatusDict.Values)
                            {
                                statusList.Add(status);
                            }
                            string jsonStatus = JsonConvert.SerializeObject(statusList);
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(jsonStatus);
                        }
                        catch (Exception exp)
                        {
                            _logger.Error(exp, "OCPPMiddleware => Error: {0}", exp.Message);
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        }
                    }
                    else if (cmd == "Reset")
                    {
                        if (!string.IsNullOrEmpty(urlChargePointId))
                        {
                            try
                            {
                                ChargePointStatus? status = null;
                                bool valueFound = false;        
                                lock (_chargePointStatusDict)
                                {
                                    if (_chargePointStatusDict.TryGetValue(urlChargePointId, out status))
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
                                _logger.Error(exp, "OCPPMiddleware SoftReset => Error: {0}", exp.Message);
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            }
                        }
                        else
                        {
                            _logger.Error("OCPPMiddleware SoftReset => Missing chargepoint ID");
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                    }
                    else if (cmd == "UnlockConnector")
                    {
                        if (!string.IsNullOrEmpty(urlChargePointId))
                        {
                            try
                            {
                                ChargePointStatus? status = null;
                                if (_chargePointStatusDict.TryGetValue(urlChargePointId, out status))
                                {
                                    // Send message to chargepoint
                                    if (status.Protocol == ProtocolOcpp20)
                                    {
                                        // OCPP V2.0
                                        await UnlockConnector20(status, context);
                                    }
                                    else
                                    {
                                        // OCPP V1.6
                                        await UnlockConnector16(status, context);
                                    }
                                }
                                else
                                {
                                    // Chargepoint offline
                                    _logger.Error("OCPPMiddleware UnlockConnector => Chargepoint offline: {0}", urlChargePointId);
                                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                }
                            }
                            catch (Exception exp)
                            {
                                _logger.Error(exp, "OCPPMiddleware UnlockConnector => Error: {0}", exp.Message);
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            }
                        }
                        else
                        {
                            _logger.Error("OCPPMiddleware UnlockConnector => Missing chargepoint ID");
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                    }
                    else
                    {
                        // Unknown action/function
                        _logger.Warning("OCPPMiddleware => action/function: {0}", cmd);
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
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
                        await context.Response.WriteAsync(string.Format("Running...\r\n\r\n{0} chargepoints connected", _chargePointStatusDict.Values.Count));
                    }
                    else
                    {
                        _logger.Information("OCPPMiddleware => Root path with deactivated index page");
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                catch (Exception exp)
                {
                    _logger.Error(exp, "OCPPMiddleware => Error: {0}", exp.Message);
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
            string? chargepointIdentifier = null;
            if(parts.Length < 3)return null;
            
            chargepointIdentifier = string.IsNullOrWhiteSpace(parts[parts.Length - 1]) ? parts[parts.Length - 2] : parts[parts.Length - 1];
            // Known chargepoint?
            return string.IsNullOrWhiteSpace(chargepointIdentifier) ? null : chargepointIdentifier;
        }

        private async void DumpLog(byte[] bMessage)
        {
            var dumpDir = _configuration.GetValue<string>("MessageDumpDir");
            if (!string.IsNullOrWhiteSpace(dumpDir))
            {
                var path = Path.Combine(dumpDir,
                    $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff")}_ocpp16-in.txt");
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
    }

   
    public static class OcppMiddlewareExtensions
    {
        public static IApplicationBuilder UseOcppMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OcppMiddleware>();
        }
    }
}
