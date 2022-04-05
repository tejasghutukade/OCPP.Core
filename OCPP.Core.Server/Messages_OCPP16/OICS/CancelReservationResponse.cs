
using Newtonsoft.Json;
namespace OCPP.Core.Server.Messages_OCPP16;
[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
public class CancelReservationResponse
{
    [JsonProperty("status")]
    public CancelReservationResponseStatus Status { get; set; }
}


[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
public enum CancelReservationResponseStatus
{
    [System.Runtime.Serialization.EnumMember(Value = @"Accepted")]
    Accepted = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Rejected")]
    Rejected = 1,
};