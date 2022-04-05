namespace OCPP.Core.Server.Messages_OCPP16;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public partial class TriggerMessageRequest
{
    [JsonProperty("connectorId", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? ConnectorId { get; set; }

    [JsonProperty("requestedMessage", Required = Required.Always)]
    public RequestedMessage RequestedMessage { get; set; }
}

public enum RequestedMessage { BootNotification, DiagnosticsStatusNotification, FirmwareStatusNotification, Heartbeat, MeterValues, StatusNotification };
