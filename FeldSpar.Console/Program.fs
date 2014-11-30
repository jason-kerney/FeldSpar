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
    let vebosityLevels = ["Max"; "Results"; "Errors"; "Detail"]


type CommandArguments =
    | [<Mandatory>][<AltCommandLine("--a")>]Test_Assembly of string
    | [<AltCommandLine("--r")>]Report_Location of string
    | [<AltCommandLine("--v")>]Verbosity of string
    | [<AltCommandLine("--al")>]Auto_Loop
    | [<AltCommandLine("--ur")>]UseReporters
    | [<AltCommandLine("--p")>]Pause
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Report_Location _ -> "This flag indicates that a JSON report is to be generated at the given location"
            | Test_Assembly _ -> "This is the location of the test library. It can be a *.dll or a *.exe file"
            | Verbosity _ -> sprintf "This sets the verbosity level for the run. Possible levels are: %A" vebosityLevels
            | Auto_Loop -> "This makes the command contiuously run executing on every compile."
            | UseReporters _ -> "This enables the use of reporters configured in the test"
            | Pause -> "This makes the console wait for key press inorder to exit. This is automaticly in effect if \"auto-loop\" is used"

[<AutoOpen>]
module Processors = 
    /// <summary>
    /// compares a sting to determin if it is a valid verbosity level. This comparison is case insensitive
    /// </summary>
    /// <param name="verbosity">the string to compare</param>
    let compareVerbosity (verbosity:string) =
        let verbosity = verbosity.ToUpper()
        vebosityLevels 
            |> List.map (fun s -> s.ToUpper()) 
            |> List.exists (fun v -> v = verbosity)

    let fileWriter path text = IO.File.WriteAllText(path, text)

    /// <summary>
    /// This uses a save funtion to save the testsSummaries as JSON
    /// </summary>
    /// <param name="saver">The function that saves the summaries path -> data -> unit</param>
    /// <param name="path">The path to which the summaries should be saved</param>
    /// <param name="testSummaries">the list test results to save by assembly name</param>
    let saveResults (saver:string -> string -> unit) (path:string) testSummaries =
        let jsonFeldSpar = testSummaries |> List.map buildOutputReport |> List.map JSONFormat
    
        saver path (jsonFeldSpar |> List.reduce (fun a b -> a + "\n" + b))

    /// <summary>
    /// Saves test summary if path is provided
    /// </summary>
    /// <param name="savePath">the path to save to, this function does nothing if is is none</param>
    /// <param name="saver">the method to call to save the summaries path -> testSummaries -> unit</param>
    /// <param name="testSummaries">the list test results to save by assembly name</param>
    let maybeSaveResults (savePath:string option) saver (testSummaries:(string * #seq<ExecutionSummary>) list) =
        match savePath with
        | Some(path) ->
            saver path testSummaries
        | _ -> ()

    /// <summary>
    /// Runs tests, saves the test result and returns the test result
    /// </summary>
    /// <param name="saver">A call that enables saving of results</param>
    /// <param name="runner">The Call that takes an assembly path and runs all tests contained in the assembly</param>
    /// <param name="testAssemblyPath">A list of test assembly tokens/param>
    let runTestsAndSaveResults (saver:(string * #seq<ExecutionSummary>) list -> unit) (runner:string -> string * #seq<ExecutionSummary>) (testAssemblyPath:IToken list) = 
        let testSummaries = testAssemblyPath |> List.map (fun token -> runner token.AssemblyPath)

        saver testSummaries

        testSummaries

    let processWithReport report processor =
        report "Running Tests"
    
        let testsFeldSpar = processor ()
    
        report "Done!"
    
        testsFeldSpar       

    let runTests savePath runner assemblyPaths =
        let saver = maybeSaveResults savePath (saveResults fileWriter)
        let processor = (fun () -> runTestsAndSaveResults saver runner assemblyPaths)
        let reporter = (printfn "%s")

        processWithReport reporter processor

type Launcher () =
    inherit MarshalByRefObject ()

    let run args =
        try
            let parser = UnionArgParser.Create<CommandArguments>()

            let args = parser.Parse(args)

            let ignoreConfig = args.Contains(<@ UseReporters @>) |> not

            let assebmlyValidation a =
                let ext = IO.Path.GetExtension a
                if not ([".exe"; ".dll"] |> List.exists (fun e -> e = ext)) then failwith "invalid file extension must be *.dll or *.exe"

                if not (IO.FileInfo(a).Exists) then failwith (sprintf "%A must exist" a)

                a

            let verbosityCheck a = 
                if compareVerbosity a
                then
                    let a = a.ToUpper()
                    if a = "MAX" then (runAndReportAll ignoreConfig true)
                    elif a = "RESULTS" then (runAndReportResults ignoreConfig false)
                    elif a = "ERRORS" then (runAndReportFailure ignoreConfig true)
                    elif a = "DETAIL" then (runAndReportNone ignoreConfig true)
                    else (runAndReportNone ignoreConfig false)
                else
                    failwith (sprintf "verbosity must be one of: %A" vebosityLevels)


            let pause = args.Contains (<@ Pause @>) || args.Contains (<@ Auto_Loop @>)

            let testAssemblyPaths = args.PostProcessResults(<@ Test_Assembly @>, assebmlyValidation ) |> List.map getToken

            let runner = 
                if args.Contains(<@ Verbosity @>)
                then
                    args.PostProcessResult(<@ Verbosity @>, verbosityCheck)
                else (runAndReportNone ignoreConfig false)

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
                let paths = testAssemblyPaths
                
                for token in paths do
                    let fileName = IO.Path.GetFileName token.AssemblyPath
                    let path = IO.Path.GetDirectoryName token.AssemblyPath
                    let watcherA = new IO.FileSystemWatcher(path, fileName)

                    let changed (ar:IO.FileSystemEventArgs) = 
                        Threading.Thread.Sleep 100
                        [ar.FullPath |> getToken] |> runTests savePath runner |> ignore

                    let created (ar:IO.FileSystemEventArgs) = 
                        Threading.Thread.Sleep 100
                        [ar.FullPath |> getToken] |> runTests savePath runner |> ignore

                    watcherA.Changed.Add changed
                    watcherA.Created.Add created

                    watcherA.EnableRaisingEvents <- true

            let results = runTests savePath runner testAssemblyPaths |> List.collect(fun (_, r) -> r)

            if pause then Console.ReadKey true |> ignore
            
            if autoLoop then 0
            else
                results 
                    |> List.filter
                        (
                            fun { TestName = _;  TestCanonicalizedName = _; TestResults = r } -> 
                                
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

