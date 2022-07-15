using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

/// <summary>
/// A geographical coordinate
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
public partial class CancelReservationRequest
{
    [JsonProperty("reservationId")]
    public long ReservationId { get; set; }
}

