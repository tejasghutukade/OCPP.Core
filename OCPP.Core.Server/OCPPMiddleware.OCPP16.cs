using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OCPP.Core.Server
{
    public partial class OCPPMiddleware
    {
        /// <summary>
        /// Waits for new OCPP V1.6 messages on the open websocket connection and delegates processing to a controller
        /// </summary>
        private async Task Receive16(ChargePointStatus chargePointStatus, HttpContext context)
        {
            ILogger logger = _logFactory.CreateLogger("OCPPMiddleware.OCPP16");
            
            

            byte[] buffer = new byte[1024 * 4];
            MemoryStream memStream = new MemoryStream(buffer.Length);

            while (chargePointStatus.WebSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await chargePointStatus.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType != WebSocketMessageType.Close)
                {
                    logger.LogTrace("OCPPMiddleware.Receive16 => Receiving segment: {Count} bytes (EndOfMessage={EndOfMessage} / MsgType={MessageType})", result.Count, result.EndOfMessage, result.MessageType);
                    memStream.Write(buffer, 0, result.Count);

                    if (result.EndOfMessage)
                    {
                        // read complete message into byte array
                        byte[] bMessage = memStream.ToArray();
                        // reset memory stream für next message
                        memStream = new MemoryStream(buffer.Length);

                        string dumpDir = _configuration.GetValue<string>("MessageDumpDir");
                        if (!string.IsNullOrWhiteSpace(dumpDir))
                        {
                            string path = Path.Combine(dumpDir,
                                $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff")}_ocpp16-in.txt");
                            try
                            {
                                // Write incoming message into dump directory
                                await File.WriteAllBytesAsync(path, bMessage);
                            }
                            catch (Exception exp)
                            {
                                logger.LogError(exp, "OCPPMiddleware.Receive16 => Error dumping incoming message to path: '{Path}'", path);
                            }
                        }

                        string ocppMessage = Encoding.UTF8.GetString(bMessage);

                        Match match = Regex.Match(ocppMessage, MessageRegExp);
                        if (!match.Success && match.Groups.Count < 3)
                        {
                            logger.LogWarning("OCPPMiddleware.Receive16 => Error in RegEx-Matching: Msg={OcppMessage})", ocppMessage);
                            continue;
                        }
    
                        string messageTypeId = match.Groups[1].Value;
                        string uniqueId = match.Groups[2].Value;
                        string action = match.Groups[3].Value;
                        string jsonPaylod = match.Groups[4].Value;
                        logger.LogInformation("OCPPMiddleware.Receive16 => OCPP-Message: Type={MessageTypeId} / ID={UniqueId} / Action={Action})", messageTypeId, uniqueId, action);
                        ControllerOCPP16 controller16 = new ControllerOCPP16(_configuration, logger, chargePointStatus, _dbContext);
                        OCPPMessage msgIn = new OCPPMessage(messageTypeId, uniqueId, action, jsonPaylod);
                        if (msgIn.MessageType == "2")
                        {
                            // Request from chargepoint to OCPP server
                            
                            OCPPMessage msgOut = controller16.ProcessRequest(msgIn);

                            // Send OCPP message with optional logging/dump
                            await SendOcpp16Message(msgOut, logger, chargePointStatus.WebSocket);
                        }
                        else if (msgIn.MessageType == "3" || msgIn.MessageType == "4")
                        {
                            // Process answer from chargepoint
                            if (!_requestQueue.ContainsKey(msgIn.UniqueId))
                            {
                                logger.LogError("OCPPMiddleware.Receive16 => HttpContext from caller not found / Msg: {OcppMessage}", ocppMessage);
                                continue;
                            }
                            controller16.ProcessAnswer(msgIn, _requestQueue[msgIn.UniqueId]);
                            _requestQueue.Remove(msgIn.UniqueId);
                        }
                        else
                        {
                            // Unknown message type
                            logger.LogError("OCPPMiddleware.Receive16 => Unknown message type: {MessageType} / Msg: {OcppMessage}", msgIn.MessageType, ocppMessage);
                        }
                    }
                    
                    //Fetch New Messages and send
                    //await SendOcpp16Message(msgOut, logger, chargePointStatus.WebSocket);
                }
                else
                {
                    logger.LogInformation("OCPPMiddleware.Receive16 => WebSocket Closed: CloseStatus={CloseStatus} / MessageType={MessageType}", result?.CloseStatus, result?.MessageType);
                    await chargePointStatus.WebSocket.CloseOutputAsync((WebSocketCloseStatus)3001, string.Empty, CancellationToken.None);
                }
            }
            logger.LogInformation("OCPPMiddleware.Receive16 => Websocket closed: State={State} / CloseStatus={CloseStatus}", chargePointStatus.WebSocket.State, chargePointStatus.WebSocket.CloseStatus);
            ChargePointStatus? dummy;
            _chargePointStatusDict.Remove(chargePointStatus.ExtId, out dummy);
        }

        /// <summary>
        /// Waits for new OCPP V1.6 messages on the open websocket connection and delegates processing to a controller
        /// </summary>
        private async Task Reset16(ChargePointStatus? chargePointStatus, HttpContext apiCallerContext)
        {
            ILogger logger = _logFactory.CreateLogger("OCPPMiddleware.OCPP16");

            Messages_OCPP16.ResetRequest resetRequest = new Messages_OCPP16.ResetRequest();
            resetRequest.Type = Messages_OCPP16.ResetRequestType.Soft;
            string jsonResetRequest = JsonConvert.SerializeObject(resetRequest);

            OCPPMessage msgOut = new OCPPMessage();
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
            ILogger logger = _logFactory.CreateLogger("OCPPMiddleware.OCPP16");

            Messages_OCPP16.UnlockConnectorRequest unlockConnectorRequest = new Messages_OCPP16.UnlockConnectorRequest();
            unlockConnectorRequest.ConnectorId = 0;

            string jsonResetRequest = JsonConvert.SerializeObject(unlockConnectorRequest);

            OCPPMessage msgOut = new OCPPMessage();
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

        private async Task SendOcpp16Message(OCPPMessage msg, ILogger logger, WebSocket webSocket)
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
            logger.LogTrace("OCPPMiddleware.OCPP16 => SendOcppMessage: {OcppTextMessage}", ocppTextMessage);

            if (string.IsNullOrEmpty(ocppTextMessage))
            {
                // invalid message
                ocppTextMessage =
                    $"[{"4"},\"{string.Empty}\",\"{Messages_OCPP16.ErrorCodes.ProtocolError}\",\"{string.Empty}\",{"{}"}]";
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
                    logger.LogError(exp, "OCPPMiddleware.SendOcpp16Message=> Error dumping message to path: '{Path}'", path);
                }
            }

            byte[] binaryMessage = Encoding.UTF8.GetBytes(ocppTextMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(binaryMessage, 0, binaryMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
