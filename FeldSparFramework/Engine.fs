namespace FeldSpar.Framework.Engine
open System
open FeldSpar.Framework
open FeldSpar.Framework.Formatters
open System.Reflection

/// <summary>
/// A type that carries information about a test durring execution reporting
/// </summary>
type ExecutionInformation =
    {
        TestName: string;
    }

/// <summary>
/// A type used to report the status of a test durring execution
/// </summary>
type ExecutionStatus =
    | Found of ExecutionInformation
    | Running of ExecutionInformation
    | Finished of ExecutionInformation * TestResult
    
/// <summary>
/// Information about the configuration of an assembly
/// </summary>
type RunConfiguration = 
    {
        Token: IToken;
        AssemblyConfiguration: Configuration option;
    }

type ConfigurationUsage =
    | IgnoreAssemblyConfiguration
    | UseAssemblyConfiguration

[<AutoOpen>]
module Runner =
    open System.Diagnostics

    let private emptyGlobal : AssemblyConfiguration = { Reporters = [] }

    let private createEnvironment (config : AssemblyConfiguration) (token:IToken) containerName testName = 
        let rec getSourcePath path = 
            let p = System.IO.DirectoryInfo(path)

            let filters = ["bin"; "Debug"; "Release"]

            match filters |> List.tryFind (fun bad -> p.Name = bad) with
            | Some(_) -> p.Parent.FullName |> getSourcePath
            | None -> p.FullName + "\\"

        let path = token.AssemblyPath |> IO.Path.GetDirectoryName |> getSourcePath
            
        { 
            ContainerName              = containerName;
            CanonicalizedContainerName = containerName |> Formatters.Basic.CanonicalizeString;
            TestName                   = testName;
            CanonicalizedName          = testName |> Formatters.Basic.CanonicalizeString;
            GoldStandardPath           = path;
            Assembly                   = token.Assembly;
            AssemblyPath               = token.AssemblyPath;
            FeldSparNetFramework       = currentFramework;
            Reporters                  = config.Reporters;
        }

    let private fileFoundReport (env:TestEnvironment) report =
        Found({TestName = env.TestName; }) |> report 

    let private fileRunningReport (env:TestEnvironment) report =
        Running({TestName = env.TestName; }) |> report 

    let internal fileFinishedReport testName report (result:TestResult) =
        Finished({TestName = testName; }, result) |> report 

    /// <summary>
    /// Creates an executable unit test from a template type
    /// </summary>
    /// <param name="config">Information about the current executing environment for the test assembly</param>
    /// <param name="report">a way to report progress as the test executes</param>
    /// <param name="token">the token representing the test assembly</param>
    /// <param name="testName">the name of the test</param>
    /// <param name="template">The code that is executed as the test</param>
    let createTestFromTemplate (config : AssemblyConfiguration) (report : ExecutionStatus -> unit ) (token:IToken) { TestContainerName = containerName; TestName = testName; Test = Test(template) } =
        let env = testName |> createEnvironment config token containerName

        report |> fileFoundReport env

        if token.IsDebugging && not System.Diagnostics.Debugger.IsAttached
        then System.Diagnostics.Debugger.Launch () |> ignore
        else
            ()

        let testCase = (fun() -> 
                            let testingCode = (fun () ->
                                                    let result = 
                                                        try
                                                            template env
                                                        with
                                                        | e -> 
                                                            Failure(ExceptionFailure(e))
                                                    
                                                    {
                                                        TestContainerName = containerName;
                                                        TestName = env.TestName;
                                                        TestCanonicalizedName = env.CanonicalizedName;
                                                        TestResults = result;
                                                    }
                                                )

                            fileRunningReport env report
                            testingCode ()
                        )
                        
        { Container = env.ContainerName; TestName = testName; TestCase = testCase }
        
    /// <summary>
    /// Converts Execution summaries to strings for reporting on
    /// </summary>
    /// <param name="results">the execution summaries to report</param>
    let reportResults results = Basic.printResults results

    let private findStaticProperties (t:Type) = 
        t.GetProperties(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Static)
            |> Array.map (fun p -> (t.Name, p))

    let private findTestProperties (filter : string * PropertyInfo -> bool) (token:IToken) = 
        token.GetExportedTypes()
            |> List.ofSeq
            |> List.map findStaticProperties
            |> List.toSeq
            |> Array.concat
            |> Array.filter filter

    /// <summary>
    /// Finds the configuration object for a test assembly. This object is used to set up reporters for gold standard testing.
    /// </summary>
    /// <param name="ignoreAssemblyConfig">is used to bypass the use of the configuration object</param>
    /// <param name="token">the token representing the test Assembly</param>
    let findConfiguration ignoreAssemblyConfig (token:IToken) = 
        let empty = { Token = token; AssemblyConfiguration = Some(Config(fun () -> emptyGlobal)) }

        match ignoreAssemblyConfig with
        | IgnoreAssemblyConfiguration -> empty
        | UseAssemblyConfiguration ->
            let configs = token |> findTestProperties (fun (_, p) -> p.PropertyType = typeof<Configuration>)

            if configs.Length = 0
            then empty
            elif configs.Length = 1
            then
                let config = configs.[0] |> (fun (_, p) -> p.GetValue(null, null) :?> Configuration)
                { Token = token; AssemblyConfiguration = Some(config) }
            else
                { Token = token; AssemblyConfiguration =  None}

    /// <summary>
    /// Takes all test templates and converts them to executable unit tests 
    /// </summary>
    /// <param name="config">The configuration information for the assembly</param>
    /// <param name="report">a way to report progress of any test</param>
    /// <param name="token">the token representing the test assembly</param>
    /// <param name="tests">the test templates to convert</param>
    let buildTestPlan (config : AssemblyConfiguration) report (token:IToken) (tests:TestInformation[]) =
        tests
            |> Array.map(fun info -> info |> createTestFromTemplate config report token)
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

    let internal shuffleTests (tests:TestInformation []) =
        let rnd = System.Random()
        let getNext = (fun (min, max) -> rnd.Next(min, max))

        shuffle tests getNext
    
    let private isTheory (t:Type) =
        t.IsGenericType && (t.GetGenericTypeDefinition()) = (typeof<Theory<_>>.GetGenericTypeDefinition())

    let private getTestsWith (map:string * PropertyInfo -> (TestInformation)[]) (config : AssemblyConfiguration) report (token:IToken) = 
        let filter (_, prop:PropertyInfo) = 
            match prop.PropertyType with
            | t when t = typeof<Test> -> true
            | t when t = typeof<IgnoredTest> -> true
            | t when isTheory t -> true
            | _ -> false

        let tests = 
            token
            |> findTestProperties filter
            |> Array.map map
            |> Array.concat

        tests
            |> shuffleTests
            |> buildTestPlan config report token

    /// <summary>
    /// Converts theory a theory template into an array of test templates
    /// </summary>
    /// <param name="baseName">The name of the theory template being converted</param>
    let convertTheoryToTests (Theory({Data = data; Base = {UnitDescription = getUnitDescription; UnitTest = testTemplate}})) containerName baseName =
        data
            |> Seq.map(fun datum -> (datum, datum |> testTemplate))
            |> Seq.map(fun (datum, testTemplate) -> { TestContainerName = containerName; TestName = sprintf "%s %s" baseName (getUnitDescription datum); Test = Test(testTemplate); })
            |> Seq.toArray

    let private getTestsFromPropery (containerName, prop:PropertyInfo) =
        let converterForTheoryTestsMethodInfo = 
            typeof<Theory<_>>.Assembly.GetExportedTypes() 
                    |> Seq.map(fun t -> t.GetMethods ()) 
                    |> Seq.concat 
                    |> Seq.filter (fun m -> m.Name = "convertTheoryToTests") 
                    |> Seq.head

        let createTestInfo testName test = { TestContainerName = containerName; TestName = testName; Test = test; }

        match prop.PropertyType with
            | t when t = typeof<Test> -> [|(createTestInfo prop.Name (prop.GetValue(null, null) :?> Test))|]
            | t when t = typeof<IgnoredTest> -> [|(createTestInfo prop.Name (Test(fun _ -> ignoreWith "Compile Ignored")))|]
            | t when t |> isTheory -> 
                let g = t.GetGenericArguments() 
                let converterForTheoryTests = converterForTheoryTestsMethodInfo.MakeGenericMethod(g)

                converterForTheoryTests.Invoke(null, [|prop.GetValue(null, null); containerName; prop.Name|]) :?> (TestInformation)[]
            | _ -> raise (ArgumentException("Incorrect property found by engine"))
            
    let private getConfigurationError (containerName:string, prop:PropertyInfo) =
        [|
            { TestContainerName = containerName; TestName = prop.Name; Test = Test(fun _ -> ignoreWith "Assembly Can only have one Configuration"); }
         |]

    let private getMapper (config) =
        match config with
        | { Token = _; AssemblyConfiguration = Some(_) } -> (config, (fun (containerName:string, prop:PropertyInfo) -> getTestsFromPropery (containerName, prop) ))
        | { Token = _; AssemblyConfiguration = None } -> (config, (fun (containerName:string, prop:PropertyInfo) -> getConfigurationError (containerName, prop) ))

    let private determinEnvironmentAndMapping (config, mapper) =
        match config with
        | { Token = token; AssemblyConfiguration = Some(Config(getConfig)) } -> 
            (token, getConfig(), mapper)
        | { Token = token; AssemblyConfiguration = None } -> (token, emptyGlobal, mapper)

    /// <summary>
    /// Searches test assembly for tests and reports as it finds them.
    /// </summary>
    /// <param name="ignoreAssemblyConfig">Ured if you do not want gold standard testing reporters</param>
    /// <param name="report">Used to report when a test is found</param>
    /// <param name="token">the token representing the test assembly</param>
    let findTestsAndReport ignoreAssemblyConfig report (token:IToken) = 
        let (token, config, mapper) = token |> findConfiguration ignoreAssemblyConfig |> getMapper |> determinEnvironmentAndMapping 

        token |> getTestsWith mapper config report

    /// <summary>
    /// Searches test assembly for tests and runs them. It then reports as it finds them, runs, them, and they complete.
    /// </summary>
    /// <param name="ignoreAssemblyConfig">Ured if you do not want gold standard testing reporters</param>
    /// <param name="report">Used to report when a test is found</param>
    /// <param name="token">the token for the test assembly</param>
    let runTestsAndReport ignoreAssemblyConfig report (token:IToken) = 
        token 
        |> findTestsAndReport ignoreAssemblyConfig report 
        |> List.map(
            fun { TestName = _; TestCase = test } -> 
                let result = test()
                result.TestResults |> fileFinishedReport result.TestName report
                result
            )

    /// <summary>
    /// Searches test assembly for tests
    /// </summary>
    /// <param name="ignoreAssemblyConfig">Ured if you do not want gold standard testing reporters</param>
    /// <param name="token">the token representing the test Assembly</param>
    let findTests ignoreAssemblyConfig (token:IToken) =  token |> findTestsAndReport ignoreAssemblyConfig ignore

    /// <summary>
    /// Searches test assembly for tests and runs them.
    /// </summary>
    /// <param name="ignoreAssemblyConfig">Ured if you do not want gold standard testing reporters</param>
    /// <param name="token">the token representing the test Assembly</param>
    let runTests ignoreAssemblyConfig (token:IToken) = token |> runTestsAndReport ignoreAssemblyConfig ignore
