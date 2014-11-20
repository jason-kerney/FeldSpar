namespace FeldSpar.Console
open System
open FeldSpar.Framework
open FeldSpar.Framework.ConsoleRunner
open FeldSpar.Framework.Engine
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Verification.ApprovalsSupport

open ApprovalTests
open Nessos.UnionArgParser

[<AutoOpen>]
module Extras =    
    let vebosityLevels = ["Max"; "Results"; "Errors"]


type CommandArguments =
    | [<Mandatory>][<AltCommandLine("--a")>]Test_Assembly of string
    | [<AltCommandLine("--r")>]Report_Location of string
    | [<AltCommandLine("--v")>]Verbosity of string
    | [<AltCommandLine("--al")>]Auto_Loop
    | [<AltCommandLine("--p")>]Pause
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Report_Location _ -> "This flag indicates that a JSON report is to be generated at the given location"
            | Test_Assembly _ -> "This is the location of the test library. It can be a *.dll or a *.exe file"
            | Verbosity _ -> sprintf "This sets the verbosity level for the run. Possible levels are: %A" vebosityLevels
            | Auto_Loop -> "This makes the command contiuously run executing on every compile."
            | Pause -> "This makes the console wait for key press inorder to exit. This is automaticly in effect if \"auto-loop\" is used"

type Launcher () =
    inherit MarshalByRefObject ()

    let compareVerbosity (verbosity:string) =
        let verbosity = verbosity.ToUpper()
        vebosityLevels 
            |> List.map (fun s -> s.ToUpper()) 
            |> List.exists (fun v -> v = verbosity)



    let runTests savePath runner tests =
        printfn "Running Tests"

        let testsFeldSpar = tests |> List.map runner
        match savePath with
        | Some(path) ->
            let jsonFeldSpar = testsFeldSpar |> List.map buildOutputReport |> List.map JSONFormat

            IO.File.WriteAllText(path, jsonFeldSpar |> List.reduce (fun a b -> a + "\n" + b))
        | _ -> ()

        printfn "Done!"

        testsFeldSpar

    let run args =
        try
            let parser = UnionArgParser.Create<CommandArguments>()

            let args = parser.Parse(args)

            let assebmlyValidation a =
                let ext = IO.Path.GetExtension a
                if not ([".exe"; ".dll"] |> List.exists (fun e -> e = ext)) then failwith "invalid file extension must be *.dll or *.exe"

                if not (IO.FileInfo(a).Exists) then failwith (sprintf "%A must exist" a)

                a

            let verbosityCheck a = 
                if compareVerbosity a
                then
                    let a = a.ToUpper()
                    if a = "MAX" then runAndReportAll
                    elif a = "RESULTS" then runAndReportResults
                    elif a = "ERRORS" then runAndReportFailure
                    else runAndReportNone
                else
                    failwith (sprintf "verbosity must be one of: %A" vebosityLevels)


            let pause = args.Contains (<@ Pause @>) || args.Contains (<@ Auto_Loop @>)

            let tests = args.PostProcessResults(<@ Test_Assembly @>, assebmlyValidation )

            let runner = 
                if args.Contains(<@ Verbosity @>)
                then
                    args.PostProcessResult(<@ Verbosity @>, verbosityCheck)
                else runAndReportNone

            let savePath =
                let saveJSONReport = args.Contains <@ Report_Location @>

                if saveJSONReport
                then
                    let pathValue = args.GetResult <@ Report_Location @>
                    let fName = System.IO.Path.GetFileName pathValue

                    if fName = null
                    then raise (ArgumentException("reportlocation must contain a valid file name"))

                    let fileInfo = IO.FileInfo(pathValue)

                    if fileInfo.Exists
                    then fileInfo.Delete ()

                    Some(fileInfo.FullName)
                else
                    None

            let autoLoop = args.Contains <@ Auto_Loop @> 
            if autoLoop 
            then
                let paths = tests
                
                for path in paths do
                    let fileName = IO.Path.GetFileName path
                    let path = IO.Path.GetDirectoryName path
                    let watcherA = new IO.FileSystemWatcher(path, fileName)

                    let changed (ar:IO.FileSystemEventArgs) = 
                        Threading.Thread.Sleep 100
                        [ar.FullPath] |> runTests savePath runner |> ignore

                    let created (ar:IO.FileSystemEventArgs) = 
                        Threading.Thread.Sleep 100
                        [ar.FullPath] |> runTests savePath runner |> ignore

                    watcherA.Changed.Add changed
                    watcherA.Created.Add created

                    watcherA.EnableRaisingEvents <- true

            let results = runTests savePath runner tests |> List.collect(fun (_, r) -> r)

            if pause then Console.ReadKey true |> ignore
            
            if autoLoop then 0
            else
                results 
                    |> List.filter
                        (
                            fun { TestDescription = _;  TestCanonicalizedName = _; TestResults = r } -> 
                                
                                match r with
                                | Failure(Ignored(_)) -> false
                                | Failure(_) -> true
                                | _ -> false
                        )

                    |> List.length

        with
        | ex -> 
            printfn "%A" (ex.Message)
            -1

    member this.Run args = 
        run args

module Program =
    [<EntryPoint>]
    let public main args = 
        try
            let applicationName = "FeldSparConsole"
            let path = IO.Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly().Location)
            let appDomain = AppDomain.CreateDomain(applicationName, null, path, path, true)

            let launcherType = typeof<Launcher>
            let sandBoxAssemblyName = launcherType.Assembly.FullName
            let sandBoxTypeName = launcherType.FullName

            let sandbox = appDomain.CreateInstanceAndUnwrap(sandBoxAssemblyName, sandBoxTypeName) :?> Launcher

            sandbox.Run args
        with
        | ex -> 
            printfn "%A" ex
            -1

