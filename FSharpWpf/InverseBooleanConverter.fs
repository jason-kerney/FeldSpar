namespace FeldSparGuiFSharp.ValueConverters
open System
open System.Windows.Data
open  System.Globalization

type InverseBooleanConverter () =
    interface IValueConverter with
        member this.Convert (value:obj, _targetType:Type, _parameter:obj, _culture:CultureInfo) = 
            (value :?> bool |> not) :> obj
        member this.ConvertBack (_value:obj, _targetType:Type, _parameter:obj, _culture:CultureInfo) = 
            failwith "Not Implemented"