
using Newtonsoft.Json;


namespace OCPP.Core.Library.Messages_OCPP16;

public partial class ChangeConfigurationRequest
{
    [JsonProperty("key", Required = Required.Always)]
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string Key { get; set; }

    [JsonProperty("value",Required = Required.Always)]
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string Value { get; set; }
}