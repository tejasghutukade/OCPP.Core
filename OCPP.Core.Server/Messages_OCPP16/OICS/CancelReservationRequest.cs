using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OCPP.Core.Server.Messages_OCPP16;

/// <summary>
/// A geographical coordinate
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
public partial class CancelReservationRequest
{
    [JsonProperty("reservationId")]
    public long ReservationId { get; set; }
}

