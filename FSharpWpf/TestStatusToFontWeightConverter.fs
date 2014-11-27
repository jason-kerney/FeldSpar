namespace FeldSparGuiFSharp.StyleConstants
open System.Windows
open FeldSpar.Api.Engine.ClrInterop.ViewModels

module TestStatusFontWeight =
    let getFontWeight status = 
        match status with
        | TestStatus.Running | TestStatus.Failure | TestStatus.Ignored -> FontWeights.Bold
        | _ -> FontWeights.Normal

namespace FeldSparGuiFSharp.ValueConverters
open System
open System.Globalization
open System.Windows.Data
open FeldSpar.Api.Engine.ClrInterop.ViewModels
open FeldSparGuiFSharp.StyleConstants
open TestStatusFontWeight

type TestStatusToFontWeightConverter () = 
    interface IValueConverter with
        member this.Convert ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) =
            let status = value :?> TestStatus

            (getFontWeight status) :> obj

        member this.ConvertBack ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) =
            failwith "Not Implemented"

    member this.IValueConverter = this :> IValueConverter

    member this.Convert ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) = this.IValueConverter.Convert (value, targetType, parameter, culture)
    member this.ConvertBack ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) = this.IValueConverter.ConvertBack (value, targetType, parameter, culture)