using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public partial class TriggerMessageRequest
{
    [JsonProperty("connectorId", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? ConnectorId { get; set; }

    [JsonProperty("requestedMessage", Required = Required.Always)]
    public RequestedMessage RequestedMessage { get; set; }
}

public enum RequestedMessage { BootNotification, DiagnosticsStatusNotification, FirmwareStatusNotification, Heartbeat, MeterValues, StatusNotification };
