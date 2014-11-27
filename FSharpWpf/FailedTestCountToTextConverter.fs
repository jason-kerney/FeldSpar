namespace FeldSparGuiFSharp.ValueConverters
open System
open System.Globalization
open System.Windows
open System.Windows.Data
open FeldSpar.Api.Engine.ClrInterop
open FeldSpar.Api.Engine.ClrInterop.ViewModels

type FailedTestCountToTextConverter () = 
    interface IValueConverter with
        member this.Convert ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) =
            let assemblyModel = value :?> ITestAssemblyModel

            let failures = 
                query
                 {
                    for test in assemblyModel.Tests do
                    where (test.Status = TestStatus.Failure)
                    select test
                    count
                 }

            failures.ToString() :> obj

        member this.ConvertBack ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) =
            failwith "Not Implemented"

    member this.IValueConverter = this :> IValueConverter

    member this.Convert ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) = this.IValueConverter.Convert (value, targetType, parameter, culture)
    member this.ConvertBack ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) = this.IValueConverter.ConvertBack (value, targetType, parameter, culture)