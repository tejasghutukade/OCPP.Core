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
        private async Task Receive20(ChargePointStatus chargePointStatus)
        {
            ILogger logger = _logger;
            
            var controller20 = _serviceProvider.GetService<ControllerOcpp20>();
            
            if (controller20 == null)
            {
                _logger.Fatal("ControllerOcpp16 not found");
                throw new Exception("ControllerOcpp16 not found");
            }
            
            byte[] buffer = new byte[1024 * 4];
            MemoryStream memStream = new MemoryStream(buffer.Length);

            while (chargePointStatus.WebSocket.State == WebSocketState.Open)
            {
                var result = await chargePointStatus.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType != WebSocketMessageType.Close)
                {
                    logger.Verbose("OCPPMiddleware.Receive20 => Receiving segment: {Count} bytes (EndOfMessage={EndOfMessage} / MsgType={MessageType})", result.Count, result.EndOfMessage, result.MessageType);
                    memStream.Write(buffer, 0, result.Count);

                    if (!result.EndOfMessage) continue;
                    // read complete message into byte array
                    byte[] bMessage = memStream.ToArray();
                    // reset memory stream für next message
                    memStream = new MemoryStream(buffer.Length);

                    var dumpDir = _configuration.GetValue<string>("MessageDumpDir");
                    if (!string.IsNullOrWhiteSpace(dumpDir))
                    {
                        var path = Path.Combine(dumpDir,
                            $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-ffff}_ocpp20-in.txt");
                        try
                        {
                            // Write incoming message into dump directory
                            await File.WriteAllBytesAsync(path, bMessage);
                        }
                        catch(Exception exp)
                        {
                            logger.Error(exp, "OCPPMiddleware.Receive20 => Error dumping incoming message to path: '{Path}'", path);
                        }
                    }

                    var ocppMessage = Encoding.UTF8.GetString(bMessage);

                    var match = Regex.Match(ocppMessage, MessageRegExp);
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        var messageTypeId = match.Groups[1].Value;
                        var uniqueId = match.Groups[2].Value;
                        var action = match.Groups[3].Value;
                        var jsonPaylod = match.Groups[4].Value;
                        logger.Information("OCPPMiddleware.Receive20 => OCPP-Message: Type={MessageTypeId} / ID={UniqueId} / Action={Action})", messageTypeId, uniqueId, action);

                        var msgIn = new OcppMessage(messageTypeId, uniqueId, action, jsonPaylod);
                        switch (msgIn.MessageType)
                        {
                            case "2":
                            {
                                // Request from chargepoint to OCPP server
                                var msgOut = await controller20.ProcessRequest(msgIn);

                                // Send OCPP message with optional logging/dump
                                await SendOcpp20Message(msgOut, logger, chargePointStatus.WebSocket);
                                break;
                            }
                            case "3":
                            case "4":
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

                                break;
                            }
                            default:
                                // Unknown message type
                                logger.Error("OCPPMiddleware.Receive20 => Unknown message type: {MessageType} / Msg: {OcppMessage}", msgIn.MessageType, ocppMessage);
                                break;
                        }
                    }else{
                        logger.Warning("OCPPMiddleware.Receive20 => Error in RegEx-Matching: Msg={OcppMessage})", ocppMessage);
                    }
                }
                else
                {
                    logger.Information("OCPPMiddleware.Receive20 => Receive: unexpected result: CloseStatus={CloseStatus} / MessageType={MessageType}", result.CloseStatus, result.MessageType);
                    await chargePointStatus.WebSocket.CloseOutputAsync((WebSocketCloseStatus)3001, string.Empty, CancellationToken.None);
                }
            }
            logger.Information("OCPPMiddleware.Receive20 => Websocket closed: State={State} / CloseStatus={CloseStatus}", chargePointStatus.WebSocket.State, chargePointStatus.WebSocket.CloseStatus);
            ChargePointStatus? dummy;
            ChargePointStatusDict.Remove(chargePointStatus.ExtId, out dummy);
        }

        /// <summary>
        /// Sends a (Soft-)Reset to the chargepoint
        /// </summary>
        private async Task Reset20(ChargePointStatus chargePointStatus, HttpContext apiCallerContext)
        {
            var resetRequest = new ResetRequest
            {
                Type = ResetEnumType.OnIdle,
                CustomData = new CustomDataType
                {
                    VendorId = ControllerOcpp20.VendorId
                }
            };

            var jsonResetRequest = JsonConvert.SerializeObject(resetRequest);

            var msgOut = new OcppMessage
            {
                MessageType = "2",
                Action = "Reset",
                UniqueId = Guid.NewGuid().ToString("N"),
                JsonPayload = jsonResetRequest,
                TaskCompletionSource = new TaskCompletionSource<string>()
            };

            // store HttpContext with MsgId for later answer processing (=> send answer to API caller)
            _requestQueue.Add(msgOut.UniqueId, msgOut);

            // Send OCPP message with optional logging/dump
            await SendOcpp20Message(msgOut, _logger, chargePointStatus.WebSocket);

            // Wait for asynchronous chargepoint response and processing
            var apiResult = await msgOut.TaskCompletionSource.Task;

            // 
            apiCallerContext.Response.StatusCode = 200;
            apiCallerContext.Response.ContentType = "application/json";
            await apiCallerContext.Response.WriteAsync(apiResult);
        }

        /// <summary>
        /// Sends a Unlock-Request to the chargepoint
        /// </summary>
        private async void UnlockConnector20(ChargePointStatus chargePointStatus, HttpContext apiCallerContext)
        {
            var unlockConnectorRequest = new UnlockConnectorRequest
            {
                EvseId = 0,
                CustomData = new CustomDataType
                {
                    VendorId = ControllerOcpp20.VendorId
                }
            };

            var jsonResetRequest = JsonConvert.SerializeObject(unlockConnectorRequest);

            var msgOut = new OcppMessage
            {
                MessageType = "2",
                Action = "UnlockConnector",
                UniqueId = Guid.NewGuid().ToString("N"),
                JsonPayload = jsonResetRequest,
                TaskCompletionSource = new TaskCompletionSource<string>()
            };

            // store HttpContext with MsgId for later answer processing (=> send answer to API caller)
            _requestQueue.Add(msgOut.UniqueId, msgOut);

            // Send OCPP message with optional logging/dump
            await SendOcpp20Message(msgOut, _logger, chargePointStatus.WebSocket);

            // Wait for asynchronous chargepoint response and processing
            var apiResult = await msgOut.TaskCompletionSource.Task;

            // 
            apiCallerContext.Response.StatusCode = 200;
            apiCallerContext.Response.ContentType = "application/json";
            await apiCallerContext.Response.WriteAsync(apiResult);
        }

        private async Task SendOcpp20Message(OcppMessage msg, ILogger logger, WebSocket webSocket)
        {
            string? ocppTextMessage;

            if (string.IsNullOrEmpty(msg.ErrorCode))
            {
                ocppTextMessage = msg.MessageType == "2" ? $"[{msg.MessageType},\"{msg.UniqueId}\",\"{msg.Action}\",{msg.JsonPayload}]"
                    : $"[{msg.MessageType},\"{msg.UniqueId}\",{msg.JsonPayload}]";
            }
            else
            {
                ocppTextMessage =
                    $"[{msg.MessageType},\"{msg.UniqueId}\",\"{msg.ErrorCode}\",\"{msg.ErrorDescription}\",{{}}]";
            }
            logger.Verbose("OCPPMiddleware.OCPP20 => SendOcppMessage: {OcppTextMessage}", ocppTextMessage);

            if (string.IsNullOrEmpty(ocppTextMessage))
            {
                // invalid message
                ocppTextMessage =
                    $"[4,\"{string.Empty}\",\"{ErrorCodes.ProtocolError}\",\"{string.Empty}\",{{}}]";
            }

            var dumpDir = _configuration.GetValue<string>("MessageDumpDir");
            if (!string.IsNullOrWhiteSpace(dumpDir))
            {
                // Write outgoing message into dump directory
                var path = Path.Combine(dumpDir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-ffff}_ocpp20-out.txt");
                try
                {
                    await File.WriteAllTextAsync(path, ocppTextMessage);
                }
                catch (Exception exp)
                {
                    logger.Error(exp, "OCPPMiddleware.SendOcpp20Message=> Error dumping message to path: '{Path}'", path);
                }
            }

            byte[] binaryMessage = Encoding.UTF8.GetBytes(ocppTextMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(binaryMessage, 0, binaryMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
