namespace FeldSpar.Framework

module TestSummaryUtilities =
    open TestResultUtilities
    
    let joinWith separator (strings : string seq) =
        System.String.Join(separator, strings)

    let reduceTo filter summaries =
        summaries |> Seq.filter (fun summary -> filter summary.TestResults)

    let reduceToFailures summaries =
        summaries |> reduceTo filterFailures

    let reduceToSuccess summaries = 
        summaries |> reduceTo filterSuccesses

    let JSONFormat (value:OutputReport) =
        let successes = 
            let subSuccesses =
                value.Successes
                |> Seq.map(fun s -> sprintf "\"%s\"" (s.Replace("\"", "'")))
                |> joinWith ",\r\n\t\t"

            subSuccesses

        let failures = 
            let subFailures = 
                value.Failures
                |> Array.map (fun { Name = name; FailureType = failureType } ->
                    let failMsg = 
                        match failureType with
                        | GeneralFailure(msg)     -> sprintf "General Failure ('%s')" msg
                        | ExceptionFailure(ex)    -> sprintf "Exception Thrown:\r\n%A" ex
                        | ExpectationFailure(msg) -> sprintf "Expectation Not Met ('%s')" msg
                        | Ignored(msg)            -> sprintf "Ignored ('%s')" msg
                        | StandardNotMet          -> "Standard was not Met"

                    let failMsg = failMsg.Replace("\"", "'")

                    sprintf "{ \"%s\" : \"%s\" }" name failMsg
                )
                |> joinWith ",\r\n\t\t"

            subFailures

        let jsonString = sprintf "{\r\n\t\"Failures\" : [\r\n\t\t%s\r\n\t],\r\n\t\"Successes\" : [\r\n\t\t%s\r\n\t]\r\n}\r\n"  failures successes
        jsonString
