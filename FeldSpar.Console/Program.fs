namespace FeldSpar.Console
open System
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Console.Helpers.Data
open FeldSpar.Console.Tests.BuildingOfTestsTests
open FeldSpar.Console.Tests.IsolationTests
open FeldSpar.Console.Tests.FilteringTests
open FeldSpar.Console.Tests.StandardsVerificationTests
open ApprovalTests;

module Program =
    let ``Setup Global Reports`` = Config(fun () -> { Reporters = [
                                                                    fun () -> Reporters.BeyondCompareReporter() :> Core.IApprovalFailureReporter;
                                                                    fun () -> Reporters.ClipboardReporter() :> Core.IApprovalFailureReporter;
                                                                  ] })

    [<EntryPoint>]
    let public main argv = 
        printfn "Running Tests"

        let formatResults result = 
            sprintf "\t\t%A" result

        let printReports results =
            results |> Seq.iter (fun report -> printfn "%s" report)

        let seperateResults (results: string seq) = 
            results |> Seq.map (
                                    fun result ->
                                        let s = ""
                                        let sep = s.PadLeft(24, '_')
                                        result + Environment.NewLine + sep + Environment.NewLine
                                )

        let tests = assembly |> runTestsAndReport (fun(status) -> 
                                                        match status with
                                                        | Found(token) -> 
                                                            let color = Console.ForegroundColor
                                                            do Console.ForegroundColor <- ConsoleColor.Gray
                                                            printfn "\t\tFound: '%s'" token.Name
                                                            do Console.ForegroundColor <- color
                                                        | Running(token) ->
                                                            let color = Console.ForegroundColor
                                                            do Console.ForegroundColor <- ConsoleColor.Blue
                                                            printfn "\t\tRunning: '%s'" token.Name
                                                            do Console.ForegroundColor <- color
                                                        | Finished(token, result) -> 
                                                            let color = Console.ForegroundColor
                                                            let newColor = 
                                                                match result with
                                                                | Success -> ConsoleColor.Green
                                                                | Failure(ExceptionFailure(_)) -> ConsoleColor.Magenta
                                                                | _ -> ConsoleColor.Red
                                                            do Console.ForegroundColor <- newColor
                                                            printfn "\t\tFinished: '%s'" token.Name
                                                            do Console.ForegroundColor <- color
                                                    )
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
