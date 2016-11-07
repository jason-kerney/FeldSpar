﻿namespace FeldSpar.Framework
open FeldSpar.Framework.Engine
open FeldSpar.Framework.TestSummaryUtilities
open System

module ConsoleRunner =
    type Verbosity =
        | HideDetails
        | ShowDetails

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

    let reportConsoleColorForResult status = 
        let oColor = Console.ForegroundColor
        let nColor = getConsoleColor status
        do Console.ForegroundColor <- nColor

        match status with
        | Found(token) -> 
            printfn "\t\tFound: '%s'" token.TestName
        | Running(token) ->
            printfn "\t\tRunning: '%s'" token.TestName
        | Finished(token, result) -> 
            let display status =
                printfn "\t\t%s: '%s'" status token.TestName

            match result with
            | Success -> display "Success"
            | Failure(Ignored(_)) -> display "Ignored"
            | Failure(ExpectationFailure(_)) -> display "Expectation Failure"
            | Failure(ExceptionFailure(_)) -> display "Exception Failure"
            | Failure(GeneralFailure(_)) -> display "General Failure"
            | Failure(StandardNotMet(_)) -> display "Standard not met Failure"

        Console.ForegroundColor <- oColor

    let reportFilteredBy isReported status =
        if status |> isReported then status |> reportConsoleColorForResult

    let reportAll status =
        status |> reportFilteredBy (fun _ -> true)

    let reportOnlyResults status =
        let isFinished (s:ExecutionStatus) =
            match s with
            | Finished(_) -> true
            | _ -> false

        status |> reportFilteredBy isFinished

    let reportFailure status =
        let isFalure (s:ExecutionStatus) =
            match s with
            | Finished(_, Success) -> false
            | Finished(_, Failure(_)) -> true
            | _ -> false

        status |> reportFilteredBy isFalure

    let reportNone status =
        let isNone _ = false

        status |> reportFilteredBy isNone

    let printReports results =
        let oColor = Console.ForegroundColor
        Console.ForegroundColor <- System.ConsoleColor.Red
            
        results |> Seq.iter (fun report -> printfn "%s" report)
            
        Console.ForegroundColor <- oColor

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
