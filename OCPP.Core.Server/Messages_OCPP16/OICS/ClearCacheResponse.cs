namespace OCPP.Core.Server.Messages_OCPP16;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public partial class ClearCacheResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public ClearCacheResponseStatus Status { get; set; }
}

public enum ClearCacheResponseStatus { Accepted, Rejected }