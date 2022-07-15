using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public partial class ReserveNowRequest
{
    [JsonProperty("connectorId", Required = Required.Always)]
    public long ConnectorId { get; set; }

    [JsonProperty("expiryDate", Required = Required.Always)]
    public DateTimeOffset ExpiryDate { get; set; }

    [JsonProperty("idTag", Required = Required.Always)]
   
    [System.ComponentModel.DataAnnotations.StringLength(20)]
    public string IdTag { get; set; }

    [JsonProperty("parentIdTag", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    [System.ComponentModel.DataAnnotations.StringLength(20)]
    public string ParentIdTag { get; set; }

    [JsonProperty("reservationId", Required = Required.Always)]
    public long ReservationId { get; set; }
}