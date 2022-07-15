using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public partial class GetDiagnosticsResponse
{
    [JsonProperty("fileName", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
    [System.ComponentModel.DataAnnotations.StringLength(255)]
    public string FileName { get; set; }
}