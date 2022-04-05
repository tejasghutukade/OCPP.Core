namespace OCPP.Core.Server.Messages_OCPP16;

using Newtonsoft.Json;

public partial class RemoteStopTransactionRequest
{
    [JsonProperty("transactionId", Required = Required.Always)]
    public long TransactionId { get; set; }
}