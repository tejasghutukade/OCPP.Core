namespace OCPP.Core.Server.Messages_OCPP16;

using Newtonsoft.Json;

public partial class ReserveNowResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public ReserveNowStatus Status { get; set; }
}

public enum ReserveNowStatus { Accepted, Faulted, Occupied, Rejected, Unavailable }