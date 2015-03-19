namespace FeldSpar.Framework

module TestSummaryUtilities =
    open TestResultUtilities
    open TryParser
    open FeldSpar.Framework.Sorting
    open System
    
    let joinWith separator (strings : string seq) =
        System.String.Join(separator, strings)

    let reduceTo filter summaries =
        summaries |> Seq.filter (fun summary -> filter summary.TestResults)

    let reduceToFailures summaries =
        summaries |> reduceTo filterFailures

    let reduceToSuccess summaries = 
        summaries |> reduceTo filterSuccesses

    let JSONFormat (value:OutputReport) =
        let spacer = "\n\t"
        let joinSpacer = sprintf ",%s\t" spacer

        let successes = 
            value.Successes
            |> Seq.map(fun s -> sprintf "\"%s\"" (s.Replace("\"", "'")))
            |> joinWith joinSpacer

        let failures = 
            value.Failures
            |> Array.map (fun { TestName = name; FailureType = failureType } ->
                let failMsg = 
                    match failureType with
                    | GeneralFailure(msg)     -> sprintf "General Failure ('%s')" msg
                    | ExceptionFailure(ex)    -> sprintf "Exception Thrown:\n%A" ex
                    | ExpectationFailure(msg) -> sprintf "Expectation Not Met ('%s')" msg
                    | Ignored(msg)            -> sprintf "Ignored ('%s')" msg
                    | StandardNotMet(path)    -> sprintf "Standard was not Met at %A" path

                let failMsg = failMsg.Replace("\"", "'")

                sprintf "{ \"%s\" : \"%s\" }" name failMsg
            )
            |> joinWith joinSpacer

        let jsonString = sprintf "{%s\"Assembly Name\" : \"%s\" %s\"Failures\" : [\n\t\t%s\n\t],%s\"Successes\" : [\n\t\t%s\n\t]\n}\n" spacer (value.AssemblyName) spacer failures spacer successes
        jsonString
