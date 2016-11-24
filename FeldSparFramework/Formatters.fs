namespace FeldSpar.Framework.Formatters
open System
open FeldSpar.Framework

module Basic =
    /// <summary>
    /// Converts a string into a string that is ok for a file name
    /// </summary>
    /// <param name="value">the string to convert</param>
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

    /// <summary>
    /// Converts a TestResult into a freindly string
    /// </summary>
    /// <param name="prefix">something to appent to the front of the string.</param>
    /// <param name="result">The result to convert</param>
    let rec printResult prefix result =
        match result with
        | Success -> sprintf "\%s%A" prefix result
        | Failure(ExpectationFailure(m)) -> sprintf "%sExpectation Failure: %s" prefix m
        | Failure(GeneralFailure(m)) -> sprintf "%sGeneral Failure: %s" prefix m
        | Failure(ExceptionFailure(ex)) -> sprintf "%sException Failure: %A" prefix ex
        | Failure(Ignored(m)) -> sprintf "%sIgnored: %s" prefix m
        | Failure(StandardNotMet(path)) -> sprintf "%sResult did not meet standards at %A" prefix path
        | Failure(SetupFailure(failure)) -> printResult (sprintf "%sBefore test failed with " prefix) (Failure failure)

    /// <summary>
    /// Converts an ExecutionSummary into a friendlystring
    /// </summary>
    /// <param name="result">the execution summary to convert</param>
    let printExecutionSummary (result : ExecutionSummary) =
        let resultsMessages = result.TestResults |> printResult "\t\t"

        sprintf "\t%s\n\t\t%s\t\t%s" result.TestContainerName result.TestName resultsMessages

    /// <summary>
    /// Maps all Execution summaries to a friendly string
    /// </summary>
    /// <param name="results">the results to convert</param>
    let printResults results =
        results |> Seq.map printExecutionSummary
