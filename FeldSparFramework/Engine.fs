﻿namespace FeldSpar.Framework.Engine
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

    let private executeInNewDomain (input : 'a) (action : 'a -> 'b) =
        let appDomain = AppDomain.CreateDomain("AppDomainHelper.ExecuteInNewAppDomain") 

        let exicutionType = typeof<Executioner<'a, 'b>>
        let sandBoxName = exicutionType.Name
        let sandBoxAssemblyName = exicutionType.Assembly.GetName () |> fun assembly -> assembly.FullName
        let sandBoxTypeName = exicutionType.FullName

        let sandbox = appDomain.CreateInstanceAndUnwrap(sandBoxAssemblyName, sandBoxTypeName) :?> Executioner<'a, 'b>

        sandbox.Execute input action

    let private createEnvironment (env : AssemblyConfiguration) name = 
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

    let createTestFromTemplate (globalEnv : AssemblyConfiguration) (report : ExecutionStatus -> unit ) name (Test(template)) =
        let env = name |> createEnvironment globalEnv

        fileFoundReport env report

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
                            testingCode |> executeInNewDomain ()
                        )
                        
        (name, testCase)
        

    let reportResults results = Basic.reportResults results

    let private runTestCode (_, test: unit -> ExecutionSummary) = test()

    let findConfiguration (assembly:Assembly) = 
        let configs = assembly.GetExportedTypes()
                     |> List.ofSeq
                     |> List.map(fun t -> t.GetProperties(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Static))
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

    let filterProperties<'a> (propInfo: PropertyInfo) = propInfo.PropertyType = typeof<'a>

    let private findTestProperties (filter : PropertyInfo -> 'a) (assembly:Assembly) = 
        (assembly.GetExportedTypes())
            |> List.ofSeq
            |> List.map (fun t -> t.GetProperties(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Static))
            |> List.toSeq
            |> Array.concat
            |> Array.filter filter

    let buildTestPlan (environment : AssemblyConfiguration) report (tests:(string * Test)[]) =
        tests
            |> Array.map(fun (name, test) -> test |> createTestFromTemplate environment report name)
            |> Array.toList

    let private getTestsWith (map:PropertyInfo -> (string * Test)) (environment : AssemblyConfiguration) report (assembly:Assembly) = 
        assembly 
            |> findTestProperties filterProperties<Test>
            |> Array.map(fun p -> (p.Name,p.GetValue(null) :?> Test))
            |> buildTestPlan environment report

    let private getBadConfigTestsWith report (assembly:Assembly) = 
        let props = assembly |> findTestProperties filterProperties<Test>

        let tests =
            props
                |> Array.map(fun p -> (p.Name,Test(fun env -> ignoreWith "Assembly Can only have one Configuration")))

        tests |> buildTestPlan emptyGlobal report

    let private determinEnvironmentAndMapping (assembly: Assembly) =
        let config = assembly |> findConfiguration

        let goodMapper = (fun (p:PropertyInfo) -> (p.Name,p.GetValue(null) :?> Test))
        let badMapper = (fun (p:PropertyInfo) -> (p.Name,Test(fun env -> ignoreWith "Assembly Can only have one Configuration")))

        match config with
        | Some(Config(getConfig)) -> 
            (getConfig(), goodMapper)
        | None -> (emptyGlobal, badMapper)

    let findTestsAndReport (environment : AssemblyConfiguration) report (assembly:Assembly) = 
        let (env, mapper) = assembly|> determinEnvironmentAndMapping 

        assembly |> getTestsWith mapper env report

    let runTestsAndReport report (assembly:Assembly) = 
        assembly |> findTestsAndReport emptyGlobal report |> List.map(fun (_, test) -> test())

    let runTestsAndReportWith environment report (assembly:Assembly) = 
        assembly |> findTestsAndReport environment report |> List.map(fun (_, test) -> test())

    let findTests (assembly:Assembly) =  assembly |> findTestsAndReport emptyGlobal ignore

    let runTests (assembly:Assembly) = assembly |> runTestsAndReport ignore

    let runTestsWith (env : AssemblyConfiguration) (assembly : Assembly) =
        assembly |> findTestsAndReport env ignore |> List.map(fun (_, test) -> test())