namespace FeldSparGuiFSharp.ValueConverters
open System
open System.Globalization
open System.Windows.Data
open FeldSpar.Api.Engine.ClrInterop.ViewModels
open FeldSparGuiFSharp.StyleConstants
open TestStatusColors

type public TestStatusColorValueConverter () =
    interface IValueConverter with
        member this.Convert ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) =
            let status = value :?> TestStatus
            (getStatusBrush status) :> obj

        member this.ConvertBack ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) =
            failwith "Not Implemented"

    member this.IValueConverter = this :> IValueConverter

    member this.Convert ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) = this.IValueConverter.Convert (value, targetType, parameter, culture)
    member this.ConvertBack ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) = this.IValueConverter.ConvertBack (value, targetType, parameter, culture)