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
using OCPP.Core.Database;
using OCPP.Core.Library;
using OCPP.Core.Library.Messages_OCPP16;
using OCPP.Core.Library.Messages_OCPP16.OICS;
using Serilog;


namespace OCPP.Core.Server
{
    public partial class OcppMiddleware
    {
        /// <summary>
        /// Waits for new OCPP V1.6 messages on the open websocket connection and delegates processing to a controller
        /// </summary>
        private async Task Receive16(ChargePointStatus chargePointStatus)
        {
            //Declaring here so there's only one dbContext instance per connection
            var controller16 = new ControllerOcpp16(_configuration,_logger,_dbContext);
            if (controller16 == null)
            {
                _logger.Fatal("ControllerOcpp16 not found");
                throw new Exception("ControllerOcpp16 not found");
            }

            controller16.SetChargePointStatus(chargePointStatus);
            
            
            var buffer = new byte[1024 * 4];
            var memStream = new MemoryStream(buffer.Length);

            while (chargePointStatus.WebSocket.State == WebSocketState.Open)
            {
               
                
                var result = await chargePointStatus.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType != WebSocketMessageType.Close)
                {
                    _logger.Verbose("OCPPMiddleware.Receive16 => Receiving segment: {Count} bytes (EndOfMessage={EndOfMessage} / MsgType={MessageType})", result.Count, result.EndOfMessage, result.MessageType);
                    memStream.Write(buffer, 0, result.Count);

                    if (!result.EndOfMessage) continue;
                    // read complete message into byte array
                    var bMessage = memStream.ToArray();
                    // reset memory stream für next message
                    memStream = new MemoryStream(buffer.Length);

                    DumpLog(bMessage);

                    var ocppMessage = Encoding.UTF8.GetString(bMessage);

                    var match = Regex.Match(ocppMessage, MessageRegExp);
                    if (!match.Success && match.Groups.Count < 3)
                    {
                        _logger.Warning("OCPPMiddleware.Receive16 => Error in RegEx-Matching: Msg={OcppMessage})", ocppMessage);
                        continue;
                    }
    
                        
                    var processStatus = await ProcessMessage16(chargePointStatus,controller16, match,_logger);
                    if(!processStatus)
                    {
                        _logger.Warning("OCPPMiddleware.Receive16 => Error in processing: Msg={OcppMessage})", ocppMessage);
                    }

                }
                else
                {
                    _logger.Information("OCPPMiddleware.Receive16 => WebSocket Closed: CloseStatus={CloseStatus} / MessageType={MessageType}", result.CloseStatus, result.MessageType);
                    await chargePointStatus.WebSocket.CloseOutputAsync((WebSocketCloseStatus)3001, string.Empty, CancellationToken.None);
                }
            }
            _logger.Information("OCPPMiddleware.Receive16 => Websocket closed: State={State} / CloseStatus={CloseStatus}", chargePointStatus.WebSocket.State, chargePointStatus.WebSocket.CloseStatus);
            ChargePointStatus? dummy;
            ChargePointStatusDict.Remove(chargePointStatus.ExtId, out dummy);
        }

        /// <summary>
        /// Waits for new OCPP V1.6 messages on the open websocket connection and delegates processing to a controller
        /// </summary>
        private async Task Reset16(ChargePointStatus chargePointStatus, HttpContext apiCallerContext)
        {
            var resetRequest = new ResetRequest
            {
                Type = ResetRequestType.Soft
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
            await SendOcpp16Message(msgOut, _logger, chargePointStatus.WebSocket);

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
        private async void UnlockConnector16(ChargePointStatus chargePointStatus, HttpContext apiCallerContext)
        {
            var unlockConnectorRequest = new UnlockConnectorRequest
            {
                ConnectorId = 0
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
            await SendOcpp16Message(msgOut, _logger, chargePointStatus.WebSocket);

            // Wait for asynchronous chargepoint response and processing
            var apiResult = await msgOut.TaskCompletionSource.Task;

            // 
            apiCallerContext.Response.StatusCode = 200;
            apiCallerContext.Response.ContentType = "application/json";
            await apiCallerContext.Response.WriteAsync(apiResult);
        }

        private async Task SendOcpp16Message(OcppMessage msg, ILogger logger, WebSocket webSocket)
        {
            string? ocppTextMessage;

            if (string.IsNullOrEmpty(msg.ErrorCode))
            {
                ocppTextMessage = msg.MessageType == "2" ? $"[{msg.MessageType},\"{msg.UniqueId}\",\"{msg.Action}\",{msg.JsonPayload}]" : $"[{msg.MessageType},\"{msg.UniqueId}\",{msg.JsonPayload}]";
            }
            else
            {
                ocppTextMessage =
                    $"[{msg.MessageType},\"{msg.UniqueId}\",\"{msg.ErrorCode}\",\"{msg.ErrorDescription}\",{{}}]";
            }
            logger.Verbose("OCPPMiddleware.OCPP16 => SendOcppMessage: {OcppTextMessage}", ocppTextMessage);

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
                string path = Path.Combine(dumpDir,
                    $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-ffff}_ocpp16-out.txt");
                try
                {
                    await File.WriteAllTextAsync(path, ocppTextMessage);
                }
                catch (Exception exp)
                {
                    logger.Error(exp, "OCPPMiddleware.SendOcpp16Message=> Error dumping message to path: '{Path}'", path);
                }
            }

            var binaryMessage = Encoding.UTF8.GetBytes(ocppTextMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(binaryMessage, 0, binaryMessage.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }


        private async Task<bool> ProcessMessage16(ChargePointStatus chargePointStatus,ControllerOcpp16 controller16 ,Match match,ILogger logger)
        {
            var messageTypeId = match.Groups[1].Value;
            var uniqueId = match.Groups[2].Value;
            var action = match.Groups[3].Value;
            var jsonPaylod = match.Groups[4].Value;
            
            logger.Information("OCPPMiddleware.Receive16 => OCPP-Message: Type={MessageTypeId} / ID={UniqueId} / Action={Action})", messageTypeId, uniqueId, action);
            
            var isMessageProcessed = false;
            
            var msgIn = new OcppMessage(messageTypeId, uniqueId, action, jsonPaylod);
            switch (msgIn.MessageType)
            {
                case "2":
                {
                    // Request from chargepoint to OCPP server
                            
                    var msgOut = await controller16.ProcessRequest(msgIn);

                    // Send OCPP message with optional logging/dump
                    await SendOcpp16Message(msgOut, logger, chargePointStatus.WebSocket);
                    isMessageProcessed =  true;
                    break;
                }
                case "3":
                case "4":
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
                    break;
                }
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
