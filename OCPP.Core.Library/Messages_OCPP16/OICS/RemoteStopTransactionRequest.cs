using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public partial class RemoteStopTransactionRequest
{
    [JsonProperty("transactionId", Required = Required.Always)]
    public long TransactionId { get; set; }
}