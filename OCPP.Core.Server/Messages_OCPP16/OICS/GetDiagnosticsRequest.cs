namespace OCPP.Core.Server.Messages_OCPP16;
using System;
using Newtonsoft.Json;
public partial class GetDiagnosticsRequest
{
    [JsonProperty("location", Required = Required.Always)]
    public Uri Location { get; set; }

    [JsonProperty("retries", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? Retries { get; set; }

    [JsonProperty("retryInterval", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? RetryInterval { get; set; }

    [JsonProperty("startTime", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? StartTime { get; set; }

    [JsonProperty("stopTime", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? StopTime { get; set; }
}