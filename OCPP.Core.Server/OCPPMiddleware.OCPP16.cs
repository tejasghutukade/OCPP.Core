using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OCPP.Core.Database;
using OCPP.Core.Library;
using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Server
{
    public partial class OcppMiddleware
    {
        /// <summary>
        /// Waits for new OCPP V1.6 messages on the open websocket connection and delegates processing to a controller
        /// </summary>
        private async Task Receive16(ChargePointStatus chargePointStatus, HttpContext context)
        {
            ILogger logger = _logger;
            //Declaring here so there's only one dbContext instance per connection
            var controller16 = _serviceProvider.GetService<ControllerOcpp16>();
            controller16!.SetChargePointStatus(chargePointStatus);
            
            
            byte[] buffer = new byte[1024 * 4];
            MemoryStream memStream = new MemoryStream(buffer.Length);

            while (chargePointStatus.WebSocket.State == WebSocketState.Open)
            {
               
                
                WebSocketReceiveResult result = await chargePointStatus.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType != WebSocketMessageType.Close)
                {
                    logger.Verbose("OCPPMiddleware.Receive16 => Receiving segment: {Count} bytes (EndOfMessage={EndOfMessage} / MsgType={MessageType})", result.Count, result.EndOfMessage, result.MessageType);
                    memStream.Write(buffer, 0, result.Count);

                    if (result.EndOfMessage)
                    {
                        // read complete message into byte array
                        byte[] bMessage = memStream.ToArray();
                        // reset memory stream für next message
                        memStream = new MemoryStream(buffer.Length);

                        DumpLog(bMessage);

                        string ocppMessage = Encoding.UTF8.GetString(bMessage);

                        Match match = Regex.Match(ocppMessage, _messageRegExp);
                        if (!match.Success && match.Groups.Count < 3)
                        {
                            logger.Warning("OCPPMiddleware.Receive16 => Error in RegEx-Matching: Msg={OcppMessage})", ocppMessage);
                            continue;
                        }
    
                        
                        var processStaus = await ProcessMessage16(chargePointStatus,controller16, match,logger);
                        if(!processStaus)
                        {
                            logger.Warning("OCPPMiddleware.Receive16 => Error in processing: Msg={OcppMessage})", ocppMessage);
                        }
                    }
                    
                }
                else
                {
                    logger.Information("OCPPMiddleware.Receive16 => WebSocket Closed: CloseStatus={CloseStatus} / MessageType={MessageType}", result?.CloseStatus, result?.MessageType);
                    await chargePointStatus.WebSocket.CloseOutputAsync((WebSocketCloseStatus)3001, string.Empty, CancellationToken.None);
                }
            }
            logger.Information("OCPPMiddleware.Receive16 => Websocket closed: State={State} / CloseStatus={CloseStatus}", chargePointStatus.WebSocket.State, chargePointStatus.WebSocket.CloseStatus);
            ChargePointStatus? dummy;
            _chargePointStatusDict.Remove(chargePointStatus.ExtId, out dummy);
        }

        /// <summary>
        /// Waits for new OCPP V1.6 messages on the open websocket connection and delegates processing to a controller
        /// </summary>
        private async Task Reset16(ChargePointStatus? chargePointStatus, HttpContext apiCallerContext)
        {
            ILogger logger = _logger;

            ResetRequest resetRequest = new ResetRequest();
            resetRequest.Type = ResetRequestType.Soft;
            string jsonResetRequest = JsonConvert.SerializeObject(resetRequest);

            OcppMessage msgOut = new OcppMessage();
            msgOut.MessageType = "2";
            msgOut.Action = "Reset";
            msgOut.UniqueId = Guid.NewGuid().ToString("N");
            msgOut.JsonPayload = jsonResetRequest;
            msgOut.TaskCompletionSource = new TaskCompletionSource<string>();

            // store HttpContext with MsgId for later answer processing (=> send anwer to API caller)
            _requestQueue.Add(msgOut.UniqueId, msgOut);

            // Send OCPP message with optional logging/dump
            await SendOcpp16Message(msgOut, logger, chargePointStatus.WebSocket);

            // Wait for asynchronous chargepoint response and processing
            string apiResult = await msgOut.TaskCompletionSource.Task;

            // 
            apiCallerContext.Response.StatusCode = 200;
            apiCallerContext.Response.ContentType = "application/json";
            await apiCallerContext.Response.WriteAsync(apiResult);
        }

        /// <summary>
        /// Sends a Unlock-Request to the chargepoint
        /// </summary>
        private async Task UnlockConnector16(ChargePointStatus? chargePointStatus, HttpContext apiCallerContext)
        {
            ILogger logger = _logger;

            UnlockConnectorRequest unlockConnectorRequest = new UnlockConnectorRequest();
            unlockConnectorRequest.ConnectorId = 0;

            string jsonResetRequest = JsonConvert.SerializeObject(unlockConnectorRequest);

            OcppMessage msgOut = new OcppMessage();
            msgOut.MessageType = "2";
            msgOut.Action = "UnlockConnector";
            msgOut.UniqueId = Guid.NewGuid().ToString("N");
            msgOut.JsonPayload = jsonResetRequest;
            msgOut.TaskCompletionSource = new TaskCompletionSource<string>();

            // store HttpContext with MsgId for later answer processing (=> send anwer to API caller)
            _requestQueue.Add(msgOut.UniqueId, msgOut);

            // Send OCPP message with optional logging/dump
            await SendOcpp16Message(msgOut, logger, chargePointStatus.WebSocket);

            // Wait for asynchronous chargepoint response and processing
            string apiResult = await msgOut.TaskCompletionSource.Task;

            // 
            apiCallerContext.Response.StatusCode = 200;
            apiCallerContext.Response.ContentType = "application/json";
            await apiCallerContext.Response.WriteAsync(apiResult);
        }

        private async Task SendOcpp16Message(OcppMessage msg, ILogger logger, WebSocket webSocket)
        {
            string? ocppTextMessage = null;

            if (string.IsNullOrEmpty(msg.ErrorCode))
            {
                if (msg.MessageType == "2")
                {
                    // OCPP-Request
                    ocppTextMessage = $"[{msg.MessageType},\"{msg.UniqueId}\",\"{msg.Action}\",{msg.JsonPayload}]";
                }
                else
                {
                    // OCPP-Response
                    ocppTextMessage = $"[{msg.MessageType},\"{msg.UniqueId}\",{msg.JsonPayload}]";
                }
            }
            else
            {
                ocppTextMessage =
                    $"[{msg.MessageType},\"{msg.UniqueId}\",\"{msg.ErrorCode}\",\"{msg.ErrorDescription}\",{"{}"}]";
            }
            logger.Verbose("OCPPMiddleware.OCPP16 => SendOcppMessage: {OcppTextMessage}", ocppTextMessage);

            if (string.IsNullOrEmpty(ocppTextMessage))
            {
                // invalid message
                ocppTextMessage =
                    $"[{"4"},\"{string.Empty}\",\"{ErrorCodes.ProtocolError}\",\"{string.Empty}\",{"{}"}]";
            }

            string dumpDir = _configuration.GetValue<string>("MessageDumpDir");
            if (!string.IsNullOrWhiteSpace(dumpDir))
            {
                // Write outgoing message into dump directory
                string path = Path.Combine(dumpDir,
                    $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff")}_ocpp16-out.txt");
                try
                {
                    File.WriteAllText(path, ocppTextMessage);
                }
                catch (Exception exp)
                {
                    logger.Error(exp, "OCPPMiddleware.SendOcpp16Message=> Error dumping message to path: '{Path}'", path);
                }
            }

            byte[] binaryMessage = Encoding.UTF8.GetBytes(ocppTextMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(binaryMessage, 0, binaryMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }


        private async Task<bool> ProcessMessage16(ChargePointStatus chargePointStatus,ControllerOcpp16 controller16 ,Match match,ILogger logger)
        {
            string messageTypeId = match.Groups[1].Value;
            string uniqueId = match.Groups[2].Value;
            string action = match.Groups[3].Value;
            string jsonPaylod = match.Groups[4].Value;
            
            logger.Information("OCPPMiddleware.Receive16 => OCPP-Message: Type={MessageTypeId} / ID={UniqueId} / Action={Action})", messageTypeId, uniqueId, action);
            
            bool isMessageProcessed = false;
            
            OcppMessage msgIn = new OcppMessage(messageTypeId, uniqueId, action, jsonPaylod);
            if (msgIn.MessageType == "2")
            {
                // Request from chargepoint to OCPP server
                            
                OcppMessage msgOut = await controller16.ProcessRequest(msgIn);

                // Send OCPP message with optional logging/dump
                await SendOcpp16Message(msgOut, logger, chargePointStatus.WebSocket);
                isMessageProcessed =  true;
            }
            else if (msgIn.MessageType == "3" || msgIn.MessageType == "4")
            {
                // Process answer from chargepoint
                if (!_requestQueue.ContainsKey(msgIn.UniqueId))
                {
                    //Fetch from database and add to queue and process
                    //PENDING
                }
                controller16.ProcessAnswer(msgIn, _requestQueue[msgIn.UniqueId]);
                _requestQueue.Remove(msgIn.UniqueId);
                isMessageProcessed =  true;
            }
            
            //Fetch New Messages and send
            var messagesTobeSent =  controller16.FetchRequestForChargePoint();
            if(messagesTobeSent ==null) return isMessageProcessed;

            foreach (var messageToBeSent in messagesTobeSent)
            {
                await SendOcpp16Message(messageToBeSent.Message, logger, chargePointStatus.WebSocket);
                messageToBeSent.SendRequest.Status = nameof(SendRequestStatus.Sent);
                await controller16.UpdateSendRequestStatus(messageToBeSent.SendRequest);
            }
            
            
            return isMessageProcessed;
            
        }
    }
}
