namespace OCPP.Core.Library.Messages_OCPP16;


using Newtonsoft.Json;


public partial class ChangeConfigurationResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public ChangeConfigurationResponseStatus Status { get; set; }
}

public enum ChangeConfigurationResponseStatus { Accepted, NotSupported, RebootRequired, Rejected };