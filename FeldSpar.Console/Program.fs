namespace FeldSpar.Console
open System
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification.ApprovalsSupport

open FeldSpar.Console.Helpers.Data
open FeldSpar.Console.Tests.BuildingOfTestsTests
open FeldSpar.Console.Tests.IsolationTests
open FeldSpar.Console.Tests.FilteringTests
open FeldSpar.Console.Tests.StandardsVerificationTests
open ApprovalTests

open Nessos.UnionArgParser

module Program =
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

    type CommandArguments =
        | ReportLocation of string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | ReportLocation _ -> "This flag indicates that a JSON report is to be generated at the given location"

    [<EntryPoint>]
    let public main argv = 
        
        let savePath =
            let parser = UnionArgParser<CommandArguments>()
            let usage = parser.Usage()

            let args = parser.Parse(argv)

            let saveJSONReport = args.Contains <@ ReportLocation @>

            if saveJSONReport
            then
                let pathValue = args.GetResult <@ ReportLocation @>
                let fName = System.IO.Path.GetFileName pathValue

                if fName = null
                then raise (ArgumentException("reportlocation must contain a valid file name"))

                let fileInfo = IO.FileInfo(pathValue)

                if fileInfo.Exists
                then fileInfo.Delete ()

                Some(fileInfo.FullName)
            else
                None

        printfn "Running Tests"

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

        let tests = testAssembly |> runTestsAndReport reportConsoleColorForResult

        let failedTests = tests
                            |> reduceToFailures 
                            |> List.ofSeq

        printfn "Displaying Results (%d Failed of %d)" (failedTests |> List.length) (tests |> List.length)

        failedTests 
            |> reportResults 
            |> seperateResults
            |> printReports

        match savePath with
        | Some(path) ->
            let json = tests |> buildOutputReport |> JSONFormat

            IO.File.WriteAllText(path, json)
        | _ -> ()

        printfn "Done!"

        //*)

        Console.ReadKey true |> ignore
        0
