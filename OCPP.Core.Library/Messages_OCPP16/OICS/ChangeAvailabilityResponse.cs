using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public class ChangeAvailabilityResponse
{
    [JsonProperty("status")]
    public ChangeAvailabilityResponseStatus Status { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
public enum ChangeAvailabilityResponseStatus
{
    [System.Runtime.Serialization.EnumMember(Value = @"Accepted")]
    Accepted = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Rejected")]
    Rejected = 1,
};