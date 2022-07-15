using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OCPP.Core.Library;
using OCPP.Core.Library.Messages_OCPP20;
using Serilog;

namespace OCPP.Core.Server
{
    public partial class OcppMiddleware
    {
        /// <summary>
        /// Waits for new OCPP V2.0 messages on the open websocket connection and delegates processing to a controller
        /// </summary>
        private async Task Receive20(ChargePointStatus? chargePointStatus, HttpContext context)
        {
            ILogger logger = _logger;
            
            var controller20 = _serviceProvider.GetService<ControllerOcpp20>();

            byte[] buffer = new byte[1024 * 4];
            MemoryStream memStream = new MemoryStream(buffer.Length);

            while (chargePointStatus.WebSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await chargePointStatus.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType != WebSocketMessageType.Close)
                {
                    logger.Verbose("OCPPMiddleware.Receive20 => Receiving segment: {Count} bytes (EndOfMessage={EndOfMessage} / MsgType={MessageType})", result.Count, result.EndOfMessage, result.MessageType);
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
                                $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff")}_ocpp20-in.txt");
                            try
                            {
                                // Write incoming message into dump directory
                                File.WriteAllBytes(path, bMessage);
                            }
                            catch(Exception exp)
                            {
                                logger.Error(exp, "OCPPMiddleware.Receive20 => Error dumping incoming message to path: '{Path}'", path);
                            }
                        }

                        string ocppMessage = Encoding.UTF8.GetString(bMessage);

                        Match match = Regex.Match(ocppMessage, _messageRegExp);
                        if (match != null && match.Groups != null && match.Groups.Count >= 3)
                        {
                            string messageTypeId = match.Groups[1].Value;
                            string uniqueId = match.Groups[2].Value;
                            string action = match.Groups[3].Value;
                            string jsonPaylod = match.Groups[4].Value;
                            logger.Information("OCPPMiddleware.Receive20 => OCPP-Message: Type={MessageTypeId} / ID={UniqueId} / Action={Action})", messageTypeId, uniqueId, action);

                            OcppMessage msgIn = new OcppMessage(messageTypeId, uniqueId, action, jsonPaylod);
                            if (msgIn.MessageType == "2")
                            {
                                // Request from chargepoint to OCPP server
                                OcppMessage msgOut = await controller20.ProcessRequest(msgIn);

                                // Send OCPP message with optional logging/dump
                                await SendOcpp20Message(msgOut, logger, chargePointStatus.WebSocket);
                            }
                            else if (msgIn.MessageType == "3" || msgIn.MessageType == "4")
                            {
                                // Process answer from chargepoint
                                if (_requestQueue.ContainsKey(msgIn.UniqueId))
                                {
                                    controller20.ProcessAnswer(msgIn, _requestQueue[msgIn.UniqueId]);
                                    _requestQueue.Remove(msgIn.UniqueId);
                                }
                                else
                                {
                                    logger.Error("OCPPMiddleware.Receive20 => HttpContext from caller not found / Msg: {OcppMessage}", ocppMessage);
                                }
                            }
                            else
                            {
                                // Unknown message type
                                logger.Error("OCPPMiddleware.Receive20 => Unknown message type: {MessageType} / Msg: {OcppMessage}", msgIn.MessageType, ocppMessage);
                            }
                        }
                        else
                        {
                            logger.Warning("OCPPMiddleware.Receive20 => Error in RegEx-Matching: Msg={OcppMessage})", ocppMessage);
                        }
                    }
                }
                else
                {
                    logger.Information("OCPPMiddleware.Receive20 => Receive: unexpected result: CloseStatus={CloseStatus} / MessageType={MessageType}", result?.CloseStatus, result?.MessageType);
                    await chargePointStatus.WebSocket.CloseOutputAsync((WebSocketCloseStatus)3001, string.Empty, CancellationToken.None);
                }
            }
            logger.Information("OCPPMiddleware.Receive20 => Websocket closed: State={State} / CloseStatus={CloseStatus}", chargePointStatus.WebSocket.State, chargePointStatus.WebSocket.CloseStatus);
            ChargePointStatus? dummy;
            _chargePointStatusDict.Remove(chargePointStatus.ExtId, out dummy);
        }

        /// <summary>
        /// Sends a (Soft-)Reset to the chargepoint
        /// </summary>
        private async Task Reset20(ChargePointStatus? chargePointStatus, HttpContext apiCallerContext)
        {
            ILogger logger = _logger;
            

            ResetRequest resetRequest = new ResetRequest();
            resetRequest.Type = ResetEnumType.OnIdle;
            resetRequest.CustomData = new CustomDataType();
            resetRequest.CustomData.VendorId = ControllerOcpp20.VendorId;

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
            await SendOcpp20Message(msgOut, logger, chargePointStatus.WebSocket);

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
        private async Task UnlockConnector20(ChargePointStatus? chargePointStatus, HttpContext apiCallerContext)
        {
            ILogger logger = _logger;

            UnlockConnectorRequest unlockConnectorRequest = new UnlockConnectorRequest();
            unlockConnectorRequest.EvseId = 0;
            unlockConnectorRequest.CustomData = new CustomDataType();
            unlockConnectorRequest.CustomData.VendorId = ControllerOcpp20.VendorId;

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
            await SendOcpp20Message(msgOut, logger, chargePointStatus.WebSocket);

            // Wait for asynchronous chargepoint response and processing
            string apiResult = await msgOut.TaskCompletionSource.Task;

            // 
            apiCallerContext.Response.StatusCode = 200;
            apiCallerContext.Response.ContentType = "application/json";
            await apiCallerContext.Response.WriteAsync(apiResult);
        }

        private async Task SendOcpp20Message(OcppMessage msg, ILogger logger, WebSocket webSocket)
        {
            string ocppTextMessage = null;

            if (string.IsNullOrEmpty(msg.ErrorCode))
            {
                if (msg.MessageType == "2")
                {
                    // OCPP-Request
                    ocppTextMessage = string.Format("[{0},\"{1}\",\"{2}\",{3}]", msg.MessageType, msg.UniqueId, msg.Action, msg.JsonPayload);
                }
                else
                {
                    // OCPP-Response
                    ocppTextMessage = string.Format("[{0},\"{1}\",{2}]", msg.MessageType, msg.UniqueId, msg.JsonPayload);
                }
            }
            else
            {
                ocppTextMessage = string.Format("[{0},\"{1}\",\"{2}\",\"{3}\",{4}]", msg.MessageType, msg.UniqueId, msg.ErrorCode, msg.ErrorDescription, "{}");
            }
            logger.Verbose("OCPPMiddleware.OCPP20 => SendOcppMessage: {0}", ocppTextMessage);

            if (string.IsNullOrEmpty(ocppTextMessage))
            {
                // invalid message
                ocppTextMessage = string.Format("[{0},\"{1}\",\"{2}\",\"{3}\",{4}]", "4", string.Empty, ErrorCodes.ProtocolError, string.Empty, "{}");
            }

            string dumpDir = _configuration.GetValue<string>("MessageDumpDir");
            if (!string.IsNullOrWhiteSpace(dumpDir))
            {
                // Write outgoing message into dump directory
                string path = Path.Combine(dumpDir, string.Format("{0}_ocpp20-out.txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff")));
                try
                {
                    File.WriteAllText(path, ocppTextMessage);
                }
                catch (Exception exp)
                {
                    logger.Error(exp, "OCPPMiddleware.SendOcpp20Message=> Error dumping message to path: '{0}'", path);
                }
            }

            byte[] binaryMessage = UTF8Encoding.UTF8.GetBytes(ocppTextMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(binaryMessage, 0, binaryMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
