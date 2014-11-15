namespace FeldSpar.Console
open System
open FeldSpar.Framework
open FeldSpar.Framework.ConsoleRunner
open FeldSpar.Framework.Engine
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Verification.ApprovalsSupport

open PathFinding.Tests.BaseTests

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
        | [<Mandatory>][<AltCommandLine("--a")>]TestAssembly of string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | ReportLocation _ -> "This flag indicates that a JSON report is to be generated at the given location"
                | TestAssembly _ -> "This is the location of the test library. It can be a *.dll or a *.exe file"

    [<EntryPoint>]
    let public main argv = 
        let parser = UnionArgParser<CommandArguments>()

        let args = parser.Parse(argv)

        let assebmlyValidation a =
            let ext = IO.Path.GetExtension a
            if not ([".exe"; ".dll"] |> List.exists (fun e -> e = ext)) then failwith "invalid file extension must be *.dll or *.exe"

            if not (IO.FileInfo(a).Exists) then failwith (sprintf "%A must exist" a)

            a

        let assemblyLocations = args.PostProcessResults(<@ TestAssembly @>, assebmlyValidation )

        let savePath =
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

        let testsFeldSpar = assemblyLocations |> List.map runAndReportAll
        match savePath with
        | Some(path) ->
            let jsonFeldSpar = testsFeldSpar |> List.map buildOutputReport |> List.map JSONFormat

            IO.File.WriteAllText(path, jsonFeldSpar |> List.reduce (fun a b -> a + Environment.NewLine + b))
        | _ -> ()

        printfn "Done!"

        Console.ReadKey true |> ignore
        0
