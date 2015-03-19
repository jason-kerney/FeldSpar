namespace FeldSpar.Framework

//Thanks http://fssnip.net/2y
module TryParser =
     // convenient, functional TryParse wrappers returning option<'a>
     let tryParseWith tryParseFunc = tryParseFunc >> function
         | true, v    -> Some v
         | false, _   -> None
 
     let parseDate   = tryParseWith System.DateTime.TryParse
     let parseInt    = tryParseWith System.Int32.TryParse
     let parseSingle = tryParseWith System.Single.TryParse
     let parseDouble = tryParseWith System.Double.TryParse
     // etc.
 
     // active patterns for try-parsing strings
     let (|Date|_|)   = parseDate
     let (|Int|_|)    = parseInt
     let (|Single|_|) = parseSingle
     let (|Double|_|) = parseDouble