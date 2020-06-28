namespace FeldSpar.Framework

open ApprovalTests
open FeldSpar.Framework.Engine
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Verification.ApprovalsSupport
open System

module ConsoleRunner =
    type Verbosity =
        | HideDetails
        | ShowDetails

    /// <summary>
    /// A configuration that will work for a large number of people
    /// </summary>
    let defaultConfig : AssemblyConfiguration = 
        { 
            Reporters = [
                            fun _ -> 
                                    Searching
                                        |> findFirstReporter<Reporters.DiffReporter>
                                        |> findFirstReporter<Reporters.MeldReporter>
                                        |> findFirstReporter<Reporters.InlineTextReporter>
                                        |> unWrapReporter
                                        
                            fun _ -> Reporters.ClipboardReporter() :> Core.IApprovalFailureReporter;

                            fun _ -> Reporters.QuietReporter() :> Core.IApprovalFailureReporter;
                        ]
        }

    /// <summary>
    /// An empty configuration that can be configured from scratch or used if not doing gold standard testing
    /// </summary>
    let emptyConfig : AssemblyConfiguration = 
        { 
            Reporters = []
        }

    /// <summary>
    /// returns the Console Color based on the status given
    /// </summary>
    /// <param name="status">The status of the current test stating if the test was found, run, or finished with a result.</param>
    let getConsoleColor status =
        match status with
        | Found(_) -> ConsoleColor.Gray
        | Running(_) -> ConsoleColor.Blue
        | Finished(_, result) ->
            match result with
            | Success -> ConsoleColor.Green
            | Failure(ExceptionFailure(_)) -> ConsoleColor.Magenta
            | Failure(Ignored(_)) -> ConsoleColor.DarkYellow
            | _ -> ConsoleColor.Red

    /// <summary>
    /// Shows a summary of execution as testing runs based on verbosity
    /// </summary>
    /// <param name="verbosity">If every status change should be shown or only the status changes should be shown</param>
    /// <param name="status">The status of the current test stating if the test was found, run, or finished with a result.</param>
    let reportConsoleColorForResultByVerbosity verbosity status = 
        let oColor = Console.ForegroundColor
        let nColor = getConsoleColor status
        do Console.ForegroundColor <- nColor

        let (foundReport, runningReport) =
            let quietReporter = ignore
            match verbosity with
            | ShowDetails -> ((printfn "\t\tFound: '%s'"), (printfn "\t\tRunning: '%s'"))
            | HideDetails -> quietReporter, quietReporter

        match status with
        | Found(token) -> 
            foundReport token.TestName
        | Running(token) ->
            runningReport token.TestName
        | Finished(token, result) -> 
            let display status =
                printfn "\t\t%s: '%s'" status token.TestName

            let getResultReport result =
                let rec getResultReport result msg =
                    match result with
                    | Success -> display (msg + "Success")
                    | Failure(Ignored(_)) -> display (msg + "Ignored")
                    | Failure(ExpectationFailure(_)) -> display (msg + "Expectation Failure")
                    | Failure(ExceptionFailure(_)) -> display (msg + "Exception Failure")
                    | Failure(GeneralFailure(_)) -> display (msg + "General Failure")
                    | Failure(StandardNotMet(_)) -> display (msg + "Standard not met Failure")
                    | Failure(SetupFailure(failure)) -> getResultReport (Failure failure) "Setup failured with "

                getResultReport result ""

            getResultReport result

        Console.ForegroundColor <- oColor

    /// <summary>
    /// Shows a summary of execution as testing runs
    /// </summary>
    /// <param name="status">The status of the current test stating if the test was found, run, or finished with a result.</param>
    let reportConsoleColorForResult status = 
        reportConsoleColorForResultByVerbosity ShowDetails status

    /// <summary>
    /// Shows a summary of execution as testing runs allowing filtering of execution statuses
    /// </summary>
    /// <param name="isReported">a predicate determining if the status should be shown</param>
    /// <param name="status">The status of the current test stating if the test was found, run, or finished with a result.</param>
    let reportFilteredBy isReported status =
        if status |> isReported then status |> reportConsoleColorForResult

    /// <summary>
    /// Shows a summary of execution as testing runs
    /// </summary>
    /// <param name="status">The status of the current test stating if the test was found, run, or finished with a result.</param>
    let reportAll status =
        status |> reportFilteredBy (fun _ -> true)

    /// <summary>
    /// Shows a summary of execution as testing runs as the tests finish running
    /// </summary>
    /// <param name="status">The status of the current test stating if the test was found, run, or finished with a result.</param>
    let reportOnlyResults status =
        let isFinished (s:ExecutionStatus) =
            match s with
            | Finished(_) -> true
            | _ -> false

        status |> reportFilteredBy isFinished

    /// <summary>
    /// Shows a summary of execution as testing runs showing only failing tests
    /// </summary>
    /// <param name="status">The status of the current test stating if the test was found, run, or finished with a result.</param>
    let reportFailure status =
        let isFalure (s:ExecutionStatus) =
            match s with
            | Finished(_, Success) -> false
            | Finished(_, Failure(_)) -> true
            | _ -> false

        status |> reportFilteredBy isFalure

    /// <summary>
    /// Hides execution results
    /// </summary>
    /// <param name="status">The status of the current test stating if the test was found, run, or finished with a result.</param>
    let reportNone status =
        let isNone _ = false

        status |> reportFilteredBy isNone

    /// <summary>
    /// Prints strings to the console in red
    /// </summary>
    /// <param name="results">a sequince of strings to print</param>
    let printReports results =
        let oColor = Console.ForegroundColor
        Console.ForegroundColor <- System.ConsoleColor.Red
            
        results |> Seq.iter (fun report -> printfn "%s" report)
            
        Console.ForegroundColor <- oColor

    /// <summary>
    /// Adds spacing before and after lines
    /// </summary>
    /// <param name="results">a sequince of strings to put spacing around</param>
    let seperateResults (results: string seq) = 
        results |> Seq.map (
                                fun result ->
                                    let s = ""
                                    let sep = s.PadLeft(24, '_')
                                    result + "\n" + sep + "\n"
                            )

    let runAndReport ignoreAssemblyConfiguration reporter showDetails (token:IToken) = 
        let tests = token |> runTestsAndReport ignoreAssemblyConfiguration reporter
        
        let failedTests = tests
                            |> reduceToFailures 
                            |> List.ofSeq

        printfn "Displaying Results (%d Failed of %d)" (failedTests |> List.length) (tests |> List.length)

        match showDetails with
        | ShowDetails ->
            failedTests 
                |> reportResults 
                |> seperateResults
                |> printReports
        | HideDetails -> ()

        (token.AssemblyName, tests)
        
    let runAndReportAll ignoreAssemblyConfiguration showDetails (token:IToken) =
        runAndReport ignoreAssemblyConfiguration reportAll showDetails token

    let runAndReportResults ignoreAssemblyConfiguration showDetails (token:IToken) =
        runAndReport ignoreAssemblyConfiguration reportOnlyResults showDetails token

    let runAndReportFailure ignoreAssemblyConfiguration showDetails (token:IToken) =
        runAndReport ignoreAssemblyConfiguration reportFailure showDetails token

    let runAndReportNone ignoreAssemblyConfiguration showDetails (token:IToken) =
        runAndReport ignoreAssemblyConfiguration reportNone showDetails token
