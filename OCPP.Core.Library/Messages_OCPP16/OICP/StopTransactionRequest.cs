﻿/*
 * OCPP.Core - https://github.com/dallmann-consulting/OCPP.Core
 * Copyright (C) 2020-2021 dallmann consulting GmbH.
 * All Rights Reserved.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

namespace OCPP.Core.Library.Messages_OCPP16.OICP
{
#pragma warning disable // Disable all warnings

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class StopTransactionRequest
    {
        [Newtonsoft.Json.JsonProperty("idTag", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.StringLength(20)]
        public string? IdTag { get; set; }

        [Newtonsoft.Json.JsonProperty("meterStop", Required = Newtonsoft.Json.Required.Always)]
        public int MeterStop { get; set; }

        [Newtonsoft.Json.JsonProperty("timestamp", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public System.DateTimeOffset Timestamp { get; set; }

        [Newtonsoft.Json.JsonProperty("transactionId", Required = Newtonsoft.Json.Required.Always)]
        public int TransactionId { get; set; }

        [Newtonsoft.Json.JsonProperty("reason", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public StopTransactionRequestReason Reason { get; set; }

        [Newtonsoft.Json.JsonProperty("transactionData", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.ICollection<TransactionData> TransactionData { get; set; }


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public enum StopTransactionRequestReason
    {
        [System.Runtime.Serialization.EnumMember(Value = @"EmergencyStop")]
        EmergencyStop = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"EVDisconnected")]
        EvDisconnected = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"HardReset")]
        HardReset = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"Local")]
        Local = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"Other")]
        Other = 4,

        [System.Runtime.Serialization.EnumMember(Value = @"PowerLoss")]
        PowerLoss = 5,

        [System.Runtime.Serialization.EnumMember(Value = @"Reboot")]
        Reboot = 6,

        [System.Runtime.Serialization.EnumMember(Value = @"Remote")]
        Remote = 7,

        [System.Runtime.Serialization.EnumMember(Value = @"SoftReset")]
        SoftReset = 8,

        [System.Runtime.Serialization.EnumMember(Value = @"UnlockCommand")]
        UnlockCommand = 9,

        [System.Runtime.Serialization.EnumMember(Value = @"DeAuthorized")]
        DeAuthorized = 10,

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class TransactionData
    {
        [Newtonsoft.Json.JsonProperty("timestamp", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public System.DateTimeOffset Timestamp { get; set; }

        [Newtonsoft.Json.JsonProperty("sampledValue", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public System.Collections.Generic.ICollection<SampledValue> SampledValue { get; set; } = new System.Collections.ObjectModel.Collection<SampledValue>();


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class SampledValue
    {
        [Newtonsoft.Json.JsonProperty("value", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Value { get; set; }

        [Newtonsoft.Json.JsonProperty("context", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public SampledValueContext? Context { get; set; }

        [Newtonsoft.Json.JsonProperty("format", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public SampledValueFormat? Format { get; set; }

        [Newtonsoft.Json.JsonProperty("measurand", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public SampledValueMeasurand? Measurand { get; set; }

        [Newtonsoft.Json.JsonProperty("phase", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public SampledValuePhase? Phase { get; set; }

        [Newtonsoft.Json.JsonProperty("location", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public SampledValueLocation? Location { get; set; }

        [Newtonsoft.Json.JsonProperty("unit", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public SampledValueUnit? Unit { get; set; }


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public enum SampledValueContext
    {
        [System.Runtime.Serialization.EnumMember(Value = @"Interruption.Begin")]
        InterruptionBegin = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"Interruption.End")]
        InterruptionEnd = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"Sample.Clock")]
        SampleClock = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"Sample.Periodic")]
        SamplePeriodic = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"Transaction.Begin")]
        TransactionBegin = 4,

        [System.Runtime.Serialization.EnumMember(Value = @"Transaction.End")]
        TransactionEnd = 5,

        [System.Runtime.Serialization.EnumMember(Value = @"Trigger")]
        Trigger = 6,

        [System.Runtime.Serialization.EnumMember(Value = @"Other")]
        Other = 7,

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public enum SampledValueFormat
    {
        [System.Runtime.Serialization.EnumMember(Value = @"Raw")]
        Raw = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"SignedData")]
        SignedData = 1,

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public enum SampledValueMeasurand
    {
        [System.Runtime.Serialization.EnumMember(Value = @"Energy.Active.Export.Register")]
        EnergyActiveExportRegister = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"Energy.Active.Import.Register")]
        EnergyActiveImportRegister = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"Energy.Reactive.Export.Register")]
        EnergyReactiveExportRegister = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"Energy.Reactive.Import.Register")]
        EnergyReactiveImportRegister = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"Energy.Active.Export.Interval")]
        EnergyActiveExportInterval = 4,

        [System.Runtime.Serialization.EnumMember(Value = @"Energy.Active.Import.Interval")]
        EnergyActiveImportInterval = 5,

        [System.Runtime.Serialization.EnumMember(Value = @"Energy.Reactive.Export.Interval")]
        EnergyReactiveExportInterval = 6,

        [System.Runtime.Serialization.EnumMember(Value = @"Energy.Reactive.Import.Interval")]
        EnergyReactiveImportInterval = 7,

        [System.Runtime.Serialization.EnumMember(Value = @"Power.Active.Export")]
        PowerActiveExport = 8,

        [System.Runtime.Serialization.EnumMember(Value = @"Power.Active.Import")]
        PowerActiveImport = 9,

        [System.Runtime.Serialization.EnumMember(Value = @"Power.Offered")]
        PowerOffered = 10,

        [System.Runtime.Serialization.EnumMember(Value = @"Power.Reactive.Export")]
        PowerReactiveExport = 11,

        [System.Runtime.Serialization.EnumMember(Value = @"Power.Reactive.Import")]
        PowerReactiveImport = 12,

        [System.Runtime.Serialization.EnumMember(Value = @"Power.Factor")]
        PowerFactor = 13,

        [System.Runtime.Serialization.EnumMember(Value = @"Current.Import")]
        CurrentImport = 14,

        [System.Runtime.Serialization.EnumMember(Value = @"Current.Export")]
        CurrentExport = 15,

        [System.Runtime.Serialization.EnumMember(Value = @"Current.Offered")]
        CurrentOffered = 16,

        [System.Runtime.Serialization.EnumMember(Value = @"Voltage")]
        Voltage = 17,

        [System.Runtime.Serialization.EnumMember(Value = @"Frequency")]
        Frequency = 18,

        [System.Runtime.Serialization.EnumMember(Value = @"Temperature")]
        Temperature = 19,

        [System.Runtime.Serialization.EnumMember(Value = @"SoC")]
        SoC = 20,

        [System.Runtime.Serialization.EnumMember(Value = @"RPM")]
        Rpm = 21,

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public enum SampledValuePhase
    {
        [System.Runtime.Serialization.EnumMember(Value = @"L1")]
        L1 = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"L2")]
        L2 = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"L3")]
        L3 = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"N")]
        N = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"L1-N")]
        L1N = 4,

        [System.Runtime.Serialization.EnumMember(Value = @"L2-N")]
        L2N = 5,

        [System.Runtime.Serialization.EnumMember(Value = @"L3-N")]
        L3N = 6,

        [System.Runtime.Serialization.EnumMember(Value = @"L1-L2")]
        L1L2 = 7,

        [System.Runtime.Serialization.EnumMember(Value = @"L2-L3")]
        L2L3 = 8,

        [System.Runtime.Serialization.EnumMember(Value = @"L3-L1")]
        L3L1 = 9,

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public enum SampledValueLocation
    {
        [System.Runtime.Serialization.EnumMember(Value = @"Cable")]
        Cable = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"EV")]
        Ev = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"Inlet")]
        Inlet = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"Outlet")]
        Outlet = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"Body")]
        Body = 4,

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.3.1.0 (Newtonsoft.Json v9.0.0.0)")]
    public enum SampledValueUnit
    {
        [System.Runtime.Serialization.EnumMember(Value = @"Wh")]
        Wh = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"kWh")]
        KWh = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"varh")]
        Varh = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"kvarh")]
        Kvarh = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"W")]
        W = 4,

        [System.Runtime.Serialization.EnumMember(Value = @"kW")]
        Kw = 5,

        [System.Runtime.Serialization.EnumMember(Value = @"VA")]
        Va = 6,

        [System.Runtime.Serialization.EnumMember(Value = @"kVA")]
        Kva = 7,

        [System.Runtime.Serialization.EnumMember(Value = @"var")]
        Var = 8,

        [System.Runtime.Serialization.EnumMember(Value = @"kvar")]
        Kvar = 9,

        [System.Runtime.Serialization.EnumMember(Value = @"A")]
        A = 10,

        [System.Runtime.Serialization.EnumMember(Value = @"V")]
        V = 11,

        [System.Runtime.Serialization.EnumMember(Value = @"K")]
        K = 12,

        [System.Runtime.Serialization.EnumMember(Value = @"Celcius")]
        Celcius = 13,

        [System.Runtime.Serialization.EnumMember(Value = @"Fahrenheit")]
        Fahrenheit = 14,

        [System.Runtime.Serialization.EnumMember(Value = @"Percent")]
        Percent = 15,

    }
}