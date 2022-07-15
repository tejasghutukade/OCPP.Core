using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public partial class ClearCacheResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public ClearCacheResponseStatus Status { get; set; }
}

public enum ClearCacheResponseStatus { Accepted, Rejected }