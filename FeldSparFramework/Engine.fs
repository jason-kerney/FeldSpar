namespace FeldSpar.Framework.Engine
open System
open FeldSpar.Framework
open FeldSpar.Framework.Formatters
open System.Reflection

/// <summary>
/// A type that carries information about a test durring execution reporting
/// </summary>
type ExecutionToken =
    {
        Name: string;
    }

/// <summary>
/// A type used to report the status of a test durring execution
/// </summary>
type ExecutionStatus =
    | Found of ExecutionToken
    | Running of ExecutionToken
    | Finished of ExecutionToken * TestResult
    
/// <summary>
/// Information about the configuration of an assembly
/// </summary>
type RunConfiguration = 
    {
        Assembly: Reflection.Assembly;
        Config: Configuration option;
    }

[<AutoOpen>]
module Runner =
    open System.Diagnostics
    type private Executioner<'a, 'b> () =
        inherit System.MarshalByRefObject ()
        member this.Execute (input : 'a) (action : 'a -> 'b) =
            input |> action

        override this.InitializeLifetimeService () =
            null

    let private emptyGlobal : AssemblyConfiguration = { Reporters = [] }

    let private executeInNewDomain (input : 'a) assemblyPath name (action : 'a -> 'b) =
        let appDomain = AppDomain.CreateDomain("AppDomainHelper.ExecuteInNewAppDomain", null, appBasePath = System.IO.Path.GetDirectoryName(assemblyPath), appRelativeSearchPath = System.IO.Path.GetDirectoryName(assemblyPath), shadowCopyFiles = true)

        try
            try

                let exicutionType = typeof<Executioner<'a, 'b>>
                let sandBoxAssemblyName = exicutionType.Assembly.GetName () |> fun assembly -> assembly.FullName
                let sandBoxTypeName = exicutionType.FullName

                let sandbox = appDomain.CreateInstanceAndUnwrap(sandBoxAssemblyName, sandBoxTypeName) :?> Executioner<'a, 'b>

                sandbox.Execute input action
            with 
            | e when (e.Message.Contains("Unexpected assembly-qualifier in a typename.")) ->
                raise (ArgumentException(sprintf "``%s`` Failed to load. Test member name may not contain a comma" name))
            | e -> raise e
        finally
            AppDomain.Unload(appDomain)

    /// <summary>
    /// Creates the environment to pass to the test
    /// </summary>
    /// <param name="config">A way to load gold standard reporters.</param>
    /// <param name="assemblyPath">the path to the assembly</param>
    /// <param name="assembly">the assembly that has the unit tests</param>
    /// <param name="testName">The name of the test case</param>
    let createEnvironment (config : AssemblyConfiguration) assemblyPath assembly testName = 
        let rec getSourcePath path = 
            let p = System.IO.DirectoryInfo(path)

            let filters = ["bin"; "Debug"; "Release"]

            match filters |> List.tryFind (fun bad -> p.Name = bad) with
            | Some(_) -> p.Parent.FullName |> getSourcePath
            | None -> p.FullName + "\\"

        let path = assemblyPath |> IO.Path.GetDirectoryName |> getSourcePath
            
        { 
            TestName = testName;
            CanonicalizedName = testName |> Formatters.Basic.CanonicalizeString;
            RootPath = path;
            Assembly = assembly;
            AssemblyPath = assemblyPath;
            Reporters = config.Reporters;
        }

    let private fileFoundReport (env:TestEnvironment) report =
        Found({Name = env.TestName; }) |> report 

    let private fileRunningReport (env:TestEnvironment) report =
        Running({Name = env.TestName; }) |> report 

    let private fileFinishedReport name report (result:TestResult) =
        Finished({Name = name; }, result) |> report 

    /// Creates an executable unit test from a template type
    #if DEBUG
    #else
    [<DebuggerStepThrough()>]
    #endif
    let createTestFromTemplate (report : ExecutionStatus -> unit) (template:UnitTestTemplate) =

        report |> fileFoundReport (template.Environment)
        let env = template.Environment

        let testCase = (fun() -> 
                            let testingCode = (fun () ->
                                                    //System.Diagnostics.Debugger.Launch() |> ignore
                                                    try
                                                        let result = 
                                                            {
                                                                TestName = env.TestName;
                                                                TestCanonicalizedName = env.CanonicalizedName;
                                                                TestResults = template.TestTemplate env;
                                                            }

                                                        result
                                                    with
                                                    | e -> 
                                                        let result = 
                                                            {
                                                                TestName = env.TestName;
                                                                TestCanonicalizedName = env.CanonicalizedName;
                                                                TestResults = Failure(ExceptionFailure(e));
                                                            }

                                                        result
                                                )

                            fileRunningReport env report
                            testingCode |> executeInNewDomain () env.AssemblyPath env.TestName
                        )
                        
        (env.TestName, testCase)
        
    /// <summary>
    /// Converts Execution summaries to strings for reporting on
    /// </summary>
    /// <param name="results">the execution summaries to report</param>
    let reportResults results = Basic.printResults results

    let private findStaticProperties (t:Type) = t.GetProperties(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Static)

    /// <summary>
    /// Finds the configuration object for a test assembly. This object is used to set up reporters for gold standard testing.
    /// </summary>
    /// <param name="ignoreAssemblyConfig">is used to bypass the use of the configuration object</param>
    /// <param name="assemblyPath">the path of the test assembly</param>
    let findConfiguration ignoreAssemblyConfig (assemblyPath:string) = 
        let assembly = assemblyPath |> IO.File.ReadAllBytes |> Assembly.Load
        let empty = { Assembly = assembly; Config = Some(Config(fun () -> emptyGlobal)) }

        if ignoreAssemblyConfig then empty
        else
            let configs = assembly.GetExportedTypes()
                            |> List.ofSeq
                            |> List.map findStaticProperties
                            |> List.toSeq
                            |> Array.concat
                            |> Array.filter(fun p -> p.PropertyType = typeof<Configuration>)

            if configs.Length = 0
            then empty
            elif configs.Length = 1
            then
                let config = configs.[0] |> (fun p -> p.GetValue (null) :?> Configuration)
                { Assembly = assembly; Config = Some(config) }
            else
                { Assembly = assembly; Config =  None}

    let private findTestProperties (filter : PropertyInfo -> bool) (assembly:Reflection.Assembly) = 
        assembly.GetExportedTypes()
            |> List.ofSeq
            |> List.map findStaticProperties
            |> List.toSeq
            |> Array.concat
            |> Array.filter filter

    /// Takes all test templates and converts them to executable unit tests 
    let buildTestPlan report (tests:(UnitTestTemplate)[]) =
        tests
            |> Array.map(fun (test) -> test |> createTestFromTemplate report)
            |> Array.toList

    /// <summary>
    /// A way to randomize an array
    /// </summary>
    /// <param name="items">the array to randomize</param>
    /// <param name="getRandom">a random number generator that takes (low range * high range) and returns a number between them, inclusively</param>
    let shuffle<'a> (items: 'a []) (getRandom: (int * int) -> int) =
        let arr = items

        let rec shuffle pt =
            if pt >= arr.Length
            then arr
            else
                let pt2 = getRandom (pt, arr.Length - 1)
                let hold = arr.[pt]
                arr.[pt] <- arr.[pt2]
                arr.[pt2] <- hold
                shuffle (pt + 1)

        shuffle 0

    let private shuffleTests (tests:(UnitTestTemplate) []) =
        let rnd = System.Random()
        let getNext = (fun (min, max) -> rnd.Next(min, max))

        shuffle tests getNext
    
    let private isTheory (t:Type) =
        t.IsGenericType && (t.GetGenericTypeDefinition()) = (typeof<Theory<_>>.GetGenericTypeDefinition())

    let private getTestsWith (map:PropertyInfo -> (string * Test)[]) (config : AssemblyConfiguration) report assembly (assemblyPath:string) = 
        let filter (prop:PropertyInfo) = 
            match prop.PropertyType with
            | t when t = typeof<Test> -> true
            | t when t = typeof<IgnoredTest> -> true
            | t when isTheory t -> true
            | _ -> false

        let creater = createEnvironment config assemblyPath assembly

        let tests = 
            assembly 
            |> findTestProperties filter
            |> Array.map map
            |> Array.concat
            |> Array.map (fun (testName, Test(template)) -> { Environment = creater testName; TestTemplate = template })

        tests
            |> shuffleTests
            |> buildTestPlan report

    /// <summary>
    /// Converts theory a theory template into an array of test templates
    /// </summary>
    /// <param name="baseName">The name of the theory template being converted</param>
    let convertTheoryToTests (Theory({Data = data; Base = {UnitDescription = getUnitDescription; UnitTest = testTemplate}})) baseName =
        data
            |> Seq.map(fun datum -> (datum, datum |> testTemplate))
            |> Seq.map(fun (datum, testTemplate) -> (sprintf "%s.%s" baseName (getUnitDescription datum), Test(testTemplate)))
            |> Seq.toArray

    let private getTestsFromPropery (prop:PropertyInfo) =
        let converterForTheoryTestsMethodInfo = 
            typeof<Theory<_>>.Assembly.GetExportedTypes() 
                    |> Seq.map(fun t -> t.GetMethods ()) 
                    |> Seq.concat 
                    |> Seq.filter (fun m -> m.Name = "convertTheoryToTests") 
                    |> Seq.head

        match prop.PropertyType with
            | t when t = typeof<Test> -> [|(prop.Name, prop.GetValue(null) :?> Test)|]
            | t when t = typeof<IgnoredTest> -> [|(prop.Name, Test(fun _ -> ignoreWith "Compile Ignored"))|]
            | t when t |> isTheory -> 
                let g = t.GetGenericArguments() 
                let converterForTheoryTests = converterForTheoryTestsMethodInfo.MakeGenericMethod(g)

                converterForTheoryTests.Invoke(null, [|prop.GetValue(null); prop.Name|]) :?> (string * Test)[]
            | _ -> raise (ArgumentException("Incorrect property found by engine"))
            
    let private getConfigurationError (prop:PropertyInfo) =
        [|
            ( prop.Name, Test(fun env -> ignoreWith "Assembly Can only have one Configuration") )
         |]

    let private getMapper (config) =
        match config with
        | { Assembly = assembly; Config = Some(_) } -> (config, (fun (prop:PropertyInfo) -> getTestsFromPropery prop ))
        | { Assembly = assembly; Config = None } -> (config, (fun (prop:PropertyInfo) -> getConfigurationError prop ))

    let private determinEnvironmentAndMapping (config, mapper) =
        match config with
        | { Assembly = assembly; Config = Some(Config(getConfig)) } -> 
            (assembly, getConfig(), mapper)
        | { Assembly = assembly; Config = None } -> (assembly, emptyGlobal, mapper)

    /// <summary>
    /// Searches test assembly for tests and reports as it finds them.
    /// </summary>
    /// <param name="ignoreAssemblyConfig">Ured if you do not want gold standard testing reporters</param>
    /// <param name="report">Used to report when a test is found</param>
    /// <param name="assemblyPath">the path of the assembly</param>
    let findTestsAndReport ignoreAssemblyConfig report (assemblyPath:string) = 
        let (assembly, config, mapper) = assemblyPath |> findConfiguration ignoreAssemblyConfig |> getMapper |> determinEnvironmentAndMapping 

        assemblyPath |> getTestsWith mapper config report assembly

    /// <summary>
    /// Searches test assembly for tests and runs them. It then reports as it finds them, runs, them, and they complete.
    /// </summary>
    /// <param name="ignoreAssemblyConfig">Ured if you do not want gold standard testing reporters</param>
    /// <param name="report">Used to report when a test is found</param>
    /// <param name="assemblyPath">the path of the assembly</param>
    let runTestsAndReport ignoreAssemblyConfig report (assemblyPath:string) = 
        assemblyPath 
        |> findTestsAndReport ignoreAssemblyConfig report 
        |> List.map(
            fun (_, test) -> 
                let result = test()
                result.TestResults |> fileFinishedReport result.TestName report
                result
            )

    /// <summary>
    /// Searches test assembly for tests
    /// </summary>
    /// <param name="ignoreAssemblyConfig">Ured if you do not want gold standard testing reporters</param>
    /// <param name="assemblyPath">the path of the assembly</param>
    let findTests ignoreAssemblyConfig (assemblyPath:string) =  assemblyPath |> findTestsAndReport ignoreAssemblyConfig ignore

    /// <summary>
    /// Searches test assembly for tests and runs them.
    /// </summary>
    /// <param name="ignoreAssemblyConfig">Ured if you do not want gold standard testing reporters</param>
    /// <param name="assemblyPath">the path of the assembly</param>
    let runTests ignoreAssemblyConfig (assemblyPath:string) = assemblyPath |> runTestsAndReport ignoreAssemblyConfig ignore
