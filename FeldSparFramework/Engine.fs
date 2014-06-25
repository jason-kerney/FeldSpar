namespace FeldSpar.Framework.Engine
open System
open FeldSpar.Framework
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Formatters
open System.Reflection

type ExecutionToken =
    {
        Name: string;
    }

type ExecutionStatus =
    | Found of ExecutionToken
    | Running of ExecutionToken
    | Finished of ExecutionToken * TestResult
    

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

    let private executeInNewDomain (input : 'a) (env : TestEnvironment) (action : 'a -> 'b) =
        let appDomain = AppDomain.CreateDomain("AppDomainHelper.ExecuteInNewAppDomain") 

        try
            try

                let exicutionType = typeof<Executioner<'a, 'b>>
                let sandBoxName = exicutionType.Name
                let sandBoxAssemblyName = exicutionType.Assembly.GetName () |> fun assembly -> assembly.FullName
                let sandBoxTypeName = exicutionType.FullName

                let sandbox = appDomain.CreateInstanceAndUnwrap(sandBoxAssemblyName, sandBoxTypeName) :?> Executioner<'a, 'b>

                sandbox.Execute input action
            with 
            | e when (e.Message.Contains("Unexpected assembly-qualifier in a typename.")) ->
                raise (ArgumentException(sprintf "``%s`` Failed to load. Test member name may not contain a comma" env.Name))
            | e -> raise e
        finally
            AppDomain.Unload(appDomain)

    let private createEnvironment (env : AssemblyConfiguration) assembly name = 
        let rec getSourcePath path = 
            let p = System.IO.DirectoryInfo(path)

            let filters = ["bin"; "Debug"; "Release"]

            match filters |> List.tryFind (fun bad -> p.Name = bad) with
            | Some(_) -> p.Parent.FullName |> getSourcePath
            | None -> p.FullName + "\\"

        let path = System.Environment.CurrentDirectory |> getSourcePath
            
        { 
            Name = name;
            CanonicalizedName = name |> Formatters.Basic.CanonicalizeString;
            RootPath = path;
            Assembly = assembly;
            Reporters = env.Reporters;
        }

    let private fileFoundReport (env:TestEnvironment) report =
        Found({Name = env.Name; }) |> report 

    let private fileRunningReport (env:TestEnvironment) report =
        Running({Name = env.Name; }) |> report 

    let private fileFinishedReport (env:TestEnvironment) report (result:TestResult) =
        Finished({Name = env.Name; }, result) |> report 

    let getGlobalTestEnvironment reporters : AssemblyConfiguration = 
        { Reporters = reporters }

    let createTestFromTemplate (globalEnv : AssemblyConfiguration) (report : ExecutionStatus -> unit ) name assembly (Test(template)) =
        let env = name |> createEnvironment globalEnv assembly

        report |> fileFoundReport env

        let testCase = (fun() -> 
                            let testingCode = (fun () ->
                                                    try
                                                        let result = 
                                                            {
                                                                TestDescription = env.Name;
                                                                TestCanonicalizedName = env.CanonicalizedName;
                                                                TestResults = template env;
                                                            }

                                                        result.TestResults |> fileFinishedReport env report

                                                        result
                                                    with
                                                    | e -> 
                                                        let result = 
                                                            {
                                                                TestDescription = env.Name;
                                                                TestCanonicalizedName = env.CanonicalizedName;
                                                                TestResults = Failure(ExceptionFailure(e));
                                                            }

                                                        result.TestResults |> fileFinishedReport env report
                                                        
                                                        result
                                                )

                            fileRunningReport env report
                            testingCode |> executeInNewDomain () env
                        )
                        
        (name, testCase)
        

    let reportResults results = Basic.reportResults results

    let private runTestCode (_, test: unit -> ExecutionSummary) = test()

    let private findStaticProperties (t:Type) = t.GetProperties(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Static)

    let findConfiguration (assembly:Assembly) = 
        let configs = assembly.GetExportedTypes()
                     |> List.ofSeq
                     |> List.map findStaticProperties
                     |> List.toSeq
                     |> Array.concat
                     |> Array.filter(fun p -> p.PropertyType = typeof<Configuration>)

        if configs.Length = 0
        then Some(Config(fun () -> emptyGlobal))
        elif configs.Length = 1
        then
            let config = configs.[0] |> (fun p -> p.GetValue (null) :?> Configuration)
            Some(config)
        else
            None

    let private findTestProperties (filter : PropertyInfo -> bool) (assembly:Assembly) = 
        (assembly.GetExportedTypes())
            |> List.ofSeq
            |> List.map findStaticProperties
            |> List.toSeq
            |> Array.concat
            |> Array.filter filter

    let buildTestPlan (environment : AssemblyConfiguration) report assembly (tests:(string * Test)[]) =
        tests
            |> Array.map(fun (name, test) -> test |> createTestFromTemplate environment report name assembly)
            |> Array.toList

    let shuffle<'a> (list: 'a []) (getRandom: (int * int) -> int) =
        let arr = list

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

    let private shuffleTests (list:(string * Test) []) =
        let rnd = System.Random()
        let getNext = (fun (min, max) -> rnd.Next(min, max))

        shuffle list getNext
    
    let private isTheory (t:Type) =
        t.IsGenericType && (t.GetGenericTypeDefinition()) = (typeof<Theory<_>>.GetGenericTypeDefinition())

    let private getTestsWith (map:PropertyInfo -> (string * Test)[]) (environment : AssemblyConfiguration) report (assembly:Assembly) = 
        let filter (prop:PropertyInfo) = 
            match prop.PropertyType with
            | t when t = typeof<Test> -> true
            | t when t = typeof<IgnoredTest> -> true
            | t when isTheory t -> true
            | _ -> false

        let tests = 
            assembly 
            |> findTestProperties filter
            |> Array.map map
            |> Array.concat

        tests
            |> shuffleTests
            |> buildTestPlan environment report assembly

    let convertTheoryToTests (Theory({Data = data; Base = {UnitDescription = getUnitDescription; UnitTest = testTemplate}})) baseName =
        data
            |> Seq.map(fun datum -> (datum, datum |> testTemplate))
            |> Seq.map(fun (datum, testTemplate) -> (sprintf "%s.%s" baseName (getUnitDescription datum), Test(testTemplate)))
            |> Seq.toArray

    let private getTestsFromPropery (prop:PropertyInfo) =
        let mi = typeof<Theory<_>>.Assembly.GetExportedTypes() 
                    |> Seq.map(fun t -> t.GetMethods ()) 
                    |> Seq.concat 
                    |> Seq.filter (fun m -> m.Name = "convertTheoryToTests") 
                    |> Seq.head

        match prop.PropertyType with
            | t when t = typeof<Test> -> [|(prop.Name, prop.GetValue(null) :?> Test)|]
            | t when t = typeof<IgnoredTest> -> [|(prop.Name, Test(fun _ -> ignoreWith "Compile Ignored"))|]
            | t when t |> isTheory -> 
                let g = t.GetGenericArguments() 
                let genericC = mi.MakeGenericMethod(g)

                genericC.Invoke(null, [|prop.GetValue(null); prop.Name|]) :?> (string * Test)[]
            | _ -> raise (ArgumentException("Incorrect property found by engine"))
            
    let private getConfigurationError (prop:PropertyInfo) =
        [|
            ( prop.Name, Test(fun env -> ignoreWith "Assembly Can only have one Configuration") )
         |]

    let private getMapper config =
        match config with
        | Some(_) -> (config, (fun (prop:PropertyInfo) -> getTestsFromPropery prop ))
        | None -> (config, (fun (prop:PropertyInfo) -> getConfigurationError prop ))

    let private determinEnvironmentAndMapping (config, mapper) =
        match config with
        | Some(Config(getConfig)) -> 
            (getConfig(), mapper)
        | None -> (emptyGlobal, mapper)

    let findTestsAndReport (environment : AssemblyConfiguration) report (assembly:Assembly) = 
        let (env, mapper) = assembly |> findConfiguration |> getMapper |> determinEnvironmentAndMapping 

        assembly |> getTestsWith mapper env report

    let runTestsAndReport report (assembly:Assembly) = 
        assembly |> findTestsAndReport emptyGlobal report |> List.map(fun (_, test) -> test())

    let runTestsAndReportWith environment report (assembly:Assembly) = 
        assembly |> findTestsAndReport environment report |> List.map(fun (_, test) -> test())

    let findTests (assembly:Assembly) =  assembly |> findTestsAndReport emptyGlobal ignore

    let runTests (assembly:Assembly) = assembly |> runTestsAndReport ignore

    let runTestsWith (env : AssemblyConfiguration) (assembly : Assembly) =
        assembly |> findTestsAndReport env ignore |> List.map(fun (_, test) -> test())
