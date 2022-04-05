using System;

namespace OCPP.Core.Server.Messages_OCPP16;

using Newtonsoft.Json;


public partial class UpdateFirmwareRequest
{
    [JsonProperty("location", Required = Required.Always)]
    public Uri Location { get; set; }

    [JsonProperty("retries", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? Retries { get; set; }

    [JsonProperty("retrieveDate", Required = Required.Always)]
    public DateTimeOffset RetrieveDate { get; set; }

    [JsonProperty("retryInterval", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? RetryInterval { get; set; }
}