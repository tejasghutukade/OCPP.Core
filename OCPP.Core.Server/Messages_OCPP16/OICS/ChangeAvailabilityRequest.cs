using Newtonsoft.Json;

namespace OCPP.Core.Server.Messages_OCPP16;

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
public class ChangeAvailabilityRequest
{
    [JsonProperty("connectorId")]
    public long ConnectorId { get; set; }

    [JsonProperty("type")]
    public TypeEnum Type { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
public enum TypeEnum
{
    [System.Runtime.Serialization.EnumMember(Value = @"Inoperative")]
    Inoperative=0,
    
    [System.Runtime.Serialization.EnumMember(Value = @"Operative")]
    Operative = 1
};