namespace FeldSpar.Console
open System
open FeldSpar.Framework
open FeldSpar.Framework.ConsoleRunner
open FeldSpar.Framework.Engine
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Verification.ApprovalsSupport

open FeldSpar.Console.Helpers.Data
open FeldSpar.Console.Tests.BuildingOfTestsTests
open FeldSpar.Console.Tests.IsolationTests
open FeldSpar.Console.Tests.FilteringTests
open FeldSpar.Console.Tests.StandardsVerificationTests
open ApprovalTests

open Nessos.UnionArgParser

module Program =
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

        let tests = testFeldSparAssembly |> runAndReport
        match savePath with
        | Some(path) ->
            let json = tests |> buildOutputReport |> JSONFormat

            IO.File.WriteAllText(path, json)
        | _ -> ()

        printfn "Done!"

        //*)

        Console.ReadKey true |> ignore
        0
