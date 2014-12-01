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

    let private executeInNewDomain (input : 'a) assemblyPath testName (action : 'a -> 'b) =
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
                raise (ArgumentException(sprintf "``%s`` Failed to load. Test member name may not contain a comma" testName))
            | e -> raise e
        finally
            AppDomain.Unload(appDomain)

    let private createEnvironment (config : AssemblyConfiguration) (token:IToken) testName = 
        let rec getSourcePath path = 
            let p = System.IO.DirectoryInfo(path)

            let filters = ["bin"; "Debug"; "Release"]

            match filters |> List.tryFind (fun bad -> p.Name = bad) with
            | Some(_) -> p.Parent.FullName |> getSourcePath
            | None -> p.FullName + "\\"

        let path = token.AssemblyPath |> IO.Path.GetDirectoryName |> getSourcePath
            
        { 
            TestName = testName;
            CanonicalizedName = testName |> Formatters.Basic.CanonicalizeString;
            GoldStandardPath = path;
            Assembly = token.Assembly;
            AssemblyPath = token.AssemblyPath;
            Reporters = config.Reporters;
        }

    let private fileFoundReport (env:TestEnvironment) report =
        Found({TestName = env.TestName; }) |> report 

    let private fileRunningReport (env:TestEnvironment) report =
        Running({TestName = env.TestName; }) |> report 

    let private fileFinishedReport testName report (result:TestResult) =
        Finished({TestName = testName; }, result) |> report 

    /// <summary>
    /// Creates an executable unit test from a template type
    /// </summary>
    /// <param name="config">Information about the current executing environment for the test assembly</param>
    /// <param name="report">a way to report progress as the test executes</param>
    /// <param name="testName">the name of the test</param>
    /// <param name="token">the token representing the test assembly</param>
    /// <param name="template">the template to use  to create an executable unit test</param>
    let createTestFromTemplate (config : AssemblyConfiguration) (report : ExecutionStatus -> unit ) testName (token:IToken) (Test(template)) =
        let env = testName |> createEnvironment config token

        report |> fileFoundReport env

        let testCase = (fun() -> 
                            let testingCode = (fun () ->
                                                    try
                                                        let result = 
                                                            {
                                                                TestName = env.TestName;
                                                                TestCanonicalizedName = env.CanonicalizedName;
                                                                TestResults = template env;
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
                        
        (testName, testCase)
        
    /// <summary>
    /// Converts Execution summaries to strings for reporting on
    /// </summary>
    /// <param name="results">the execution summaries to report</param>
    let reportResults results = Basic.printResults results

    let private findStaticProperties (t:Type) = t.GetProperties(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Static)

    let private findTestProperties (filter : PropertyInfo -> bool) (token:IToken) = 
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

        if ignoreAssemblyConfig then empty
        else
            let configs = token |> findTestProperties (fun p -> p.PropertyType = typeof<Configuration>)

            if configs.Length = 0
            then empty
            elif configs.Length = 1
            then
                let config = configs.[0] |> (fun p -> p.GetValue (null) :?> Configuration)
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
    let buildTestPlan (config : AssemblyConfiguration) report (token:IToken) (tests:(string * Test)[]) =
        tests
            |> Array.map(fun (testName, test) -> test |> createTestFromTemplate config report testName token)
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

    let private shuffleTests (tests:(string * Test) []) =
        let rnd = System.Random()
        let getNext = (fun (min, max) -> rnd.Next(min, max))

        shuffle tests getNext
    
    let private isTheory (t:Type) =
        t.IsGenericType && (t.GetGenericTypeDefinition()) = (typeof<Theory<_>>.GetGenericTypeDefinition())

    let private getTestsWith (map:PropertyInfo -> (string * Test)[]) (config : AssemblyConfiguration) report (token:IToken) = 
        let filter (prop:PropertyInfo) = 
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

        let createTestInfo testName test = { TestName = testName; Test = test }

        match prop.PropertyType with
            | t when t = typeof<Test> -> [|(createTestInfo prop.Name (prop.GetValue(null) :?> Test))|]
            | t when t = typeof<IgnoredTest> -> [|(createTestInfo prop.Name (Test(fun _ -> ignoreWith "Compile Ignored")))|]
            | t when t |> isTheory -> 
                let g = t.GetGenericArguments() 
                let converterForTheoryTests = converterForTheoryTestsMethodInfo.MakeGenericMethod(g)

                converterForTheoryTests.Invoke(null, [|prop.GetValue(null); prop.Name|]) :?> (string * Test)[] |> Array.map (fun (testName, test) -> { TestName = testName; Test = test})
            | _ -> raise (ArgumentException("Incorrect property found by engine"))
            
    let private getConfigurationError (prop:PropertyInfo) =
        [|
            { TestName = prop.Name; Test = Test(fun _ -> ignoreWith "Assembly Can only have one Configuration"); }
         |]

    let private getMapper (config) =
        match config with
        | { Token = _; AssemblyConfiguration = Some(_) } -> (config, (fun (prop:PropertyInfo) -> getTestsFromPropery prop ))
        | { Token = _; AssemblyConfiguration = None } -> (config, (fun (prop:PropertyInfo) -> getConfigurationError prop ))

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

        let mapper1 = (fun pi -> pi |> mapper |> Array.map (fun { TestName = testName; Test = test} -> (testName, test)))
        token |> getTestsWith mapper1 config report

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
            fun (_, test) -> 
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
