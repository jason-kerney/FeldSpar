namespace FeldSparGuiFSharp.ValueConverters
open System
open System.Globalization
open System.Windows
open System.Windows.Data

type BinaryVisibilityConverter () =
    interface IValueConverter with
        member this.Convert ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) =
            let v = value :?> bool
            (match v with | true -> Visibility.Visible | false -> Visibility.Collapsed) :> obj

        member this.ConvertBack ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) =
            failwith "Not Implemented"

    member this.IValueConverter = this :> IValueConverter

    member this.Convert ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) = this.IValueConverter.Convert (value, targetType, parameter, culture)
    member this.ConvertBack ((value:obj), (targetType:Type), (parameter:obj), (culture:CultureInfo)) = this.IValueConverter.ConvertBack (value, targetType, parameter, culture)
