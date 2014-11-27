namespace FeldSparGuiFSharp.StyleConstants
open System.Windows.Media
open FeldSpar.Api.Engine.ClrInterop.ViewModels

module TestStatusColors = 
    let FailureBrush = Brushes.Red
    let IgnoredBrush = Brushes.DarkGoldenrod
    let SuccessBrush = Brushes.ForestGreen
    let RunningBrush = Brushes.DodgerBlue
    let NoneBrush = Brushes.Gray

    let getStatusBrush status =
        match status with
        | TestStatus.Success -> SuccessBrush
        | TestStatus.Running -> RunningBrush
        | TestStatus.Ignored -> IgnoredBrush
        | TestStatus.Failure -> FailureBrush
        | _ -> NoneBrush

namespace FeldSparGuiFSharp.ValueConverters
open System
open System.Collections.Generic
open System.Globalization
open System.Windows
open System.Windows.Data
open FeldSpar.Api.Engine.ClrInterop
open FeldSpar.Api.Engine.ClrInterop.ViewModels
open FeldSparGuiFSharp.StyleConstants
open System.Windows.Media
open TestStatusColors


type TestAssemblyStatusColorValueConverter () =
    let getMaybeBrush (items:ITestDetailModel seq) (brush:Brush) status =
        if
            query
                {
                    for item in items do
                    exists (item.Status = status)
                }
        then Some(brush :> obj)
        else None

    let (|IsRunning|_|) (items:ITestDetailModel seq) =
        getMaybeBrush items RunningBrush (TestStatus.Running)

    let (|IsFailure|_|) (items:ITestDetailModel seq) =
        getMaybeBrush items FailureBrush (TestStatus.Failure)

    let (|IsIgnored|_|) (items:ITestDetailModel seq) =
        getMaybeBrush items IgnoredBrush (TestStatus.Ignored)

    let (|IsSuccess|_|) (items:ITestDetailModel seq) =
        getMaybeBrush items SuccessBrush (TestStatus.Success)

    interface IValueConverter with
        member this.Convert ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) =
            let tests = value :?> ITestDetailModel seq

            match tests with
            | IsRunning brush -> brush
            | IsFailure brush -> brush
            | IsIgnored brush -> brush
            | IsSuccess brush -> brush
            | _ -> NoneBrush :> obj

            

        member this.ConvertBack ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) =
            failwith "Not Implemented"

    member this.IValueConverter = this :> IValueConverter

    member this.Convert ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) = this.IValueConverter.Convert (value, targetType, parameter, culture)
    member this.ConvertBack ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) = this.IValueConverter.ConvertBack (value, targetType, parameter, culture)