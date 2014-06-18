namespace FeldSpar.Console
open System
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification.ApprovalsSupport;

open FeldSpar.Console.Helpers.Data
open FeldSpar.Console.Tests.BuildingOfTestsTests
open FeldSpar.Console.Tests.IsolationTests
open FeldSpar.Console.Tests.FilteringTests
open FeldSpar.Console.Tests.StandardsVerificationTests
open ApprovalTests;

module Program =
    let ``Setup Global Reports`` = 
        Config(fun () -> { Reporters = [
                                        fun () -> 
                                                try
                                                    getReporter_old<Reporters.DiffReporter> ()
                                                with
                                                | _ -> getReporter_old<Reporters.NotepadLauncher> ()

                                            
                                        fun () -> Reporters.ClipboardReporter() :> Core.IApprovalFailureReporter;
                                        ] })

    let getConsoleColor status =
        match status with
        | Found(_) -> ConsoleColor.Gray
        | Running(_) -> ConsoleColor.Blue
        | Finished(_, result) ->
            match result with
            | Success -> ConsoleColor.Green
            | Failure(ExceptionFailure(_)) -> ConsoleColor.Magenta
            | Failure(Ignored(_)) -> ConsoleColor.DarkRed
            | _ -> ConsoleColor.Red

    let reportConsoleColorForResult status = 
        let oColor = Console.ForegroundColor
        let nColor = getConsoleColor status
        do Console.ForegroundColor <- nColor

        match status with
        | Found(token) -> 
            printfn "\t\tFound: '%s'" token.Name
        | Running(token) ->
            printfn "\t\tRunning: '%s'" token.Name
        | Finished(token, result) -> 
            printfn "\t\tFinished: '%s'" token.Name

        do Console.ForegroundColor <- oColor

    [<EntryPoint>]
    let public main argv = 
        printfn "Running Tests"

        let formatResults result = 
            sprintf "\t\t%A" result

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
                                        result + Environment.NewLine + sep + Environment.NewLine
                                )

        let tests = assembly |> runTestsAndReport reportConsoleColorForResult

        let failedTests = tests
                            |> reduceToFailures 
                            |> List.ofSeq

        printfn "Displaying Results (%d Failed of %d)" (failedTests |> List.length) (tests |> List.length)

        failedTests 
            |> reportResults 
            |> seperateResults
            |> printReports

        printfn "Done!"

        Console.ReadKey true |> ignore
        0
