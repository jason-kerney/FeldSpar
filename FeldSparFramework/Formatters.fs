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
            else sprintf "%s%s" v "\n"

        let printResult result = 
            let prefix = "\t\t"
            match result with
            | Success -> sprintf "\t\t%A" result
            | Failure(ExpectationFailure(m)) ->
                sprintf "%sExpectation Failure: %s" prefix m
            | Failure(t) -> sprintf "\t\t%A" t

        let resultsMessages = result.TestResults |> printResult 

        sprintf "\t%s\t%s" result.TestDescription resultsMessages

    let reportResults results =
        results |> Seq.map reportResult
