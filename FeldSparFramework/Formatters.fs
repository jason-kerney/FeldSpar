namespace FeldSpar.Framework.Formatters
open System
open FeldSpar.Framework

module Basic =
    let CanonicalizeString (value:string) =
        let toString : char seq -> string = Seq.map string >> String.concat ""
        let cannicalized = value 
                            |> Seq.filter (fun c -> 
                                               (System.Char.IsLetterOrDigit c)
                                               || c = ' '
                                               || c = '_'
                                               || c = '.'
                                               || c = '-'
                                           )
                            |> Seq.map (fun c -> 
                                            if c = ' '
                                            then '_'
                                            else c
                                        )
        (cannicalized |> toString).Trim()

    let reportResult (result : ExecutionSummary) =
        let getValue v =
            if System.String.IsNullOrWhiteSpace v
            then ""
            else sprintf "%s%s" v Environment.NewLine

        let printResult result = 
            sprintf "\t\t%A" result

        let resultsMessages = result.TestResults |> printResult 

        sprintf "\t%s\t%s" result.TestDescription resultsMessages

    let reportResults results =
        results |> Seq.map reportResult
