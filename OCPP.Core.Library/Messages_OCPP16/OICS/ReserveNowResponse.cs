using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public partial class ReserveNowResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public ReserveNowStatus Status { get; set; }
}

public enum ReserveNowStatus { Accepted, Faulted, Occupied, Rejected, Unavailable }