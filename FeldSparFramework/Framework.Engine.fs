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

    let private emptyGlobal : GlobalTestEnvironment = { Reporters = [] }

    let private executeInNewDomain (input : 'a) (action : 'a -> 'b) =
        let appDomain = AppDomain.CreateDomain("AppDomainHelper.ExecuteInNewAppDomain") 

        let exicutionType = typeof<Executioner<'a, 'b>>
        let sandBoxName = exicutionType.Name
        let sandBoxAssemblyName = exicutionType.Assembly.GetName () |> fun assembly -> assembly.FullName
        let sandBoxTypeName = exicutionType.FullName

        let sandbox = appDomain.CreateInstanceAndUnwrap(sandBoxAssemblyName, sandBoxTypeName) :?> Executioner<'a, 'b>

        sandbox.Execute input action

    let private createEnvironment (env : GlobalTestEnvironment) name = 
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

    let getGlobalTestEnvironment reporters : GlobalTestEnvironment = 
        { Reporters = reporters }

    let createTestFromTemplate (globalEnv : GlobalTestEnvironment) (report : ExecutionStatus -> unit ) name (Test(template)) =
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

    let findTestsAndReport (environment : GlobalTestEnvironment) report (assembly:Assembly) = 
        (assembly.GetExportedTypes())
            |> List.ofSeq
            |> List.map (fun t -> t.GetProperties(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Static))
            |> List.toSeq
            |> Array.concat
            |> Array.filter (fun p -> p.PropertyType = typeof<Test>)
            |> Array.map(fun p -> (p.Name,p.GetValue(null) :?> Test))
            |> Array.map(fun (name, test) -> test |> createTestFromTemplate environment report name)
            |> Array.toList

    let runTestsAndReport report (assembly:Assembly) = 
        assembly |> findTestsAndReport emptyGlobal report |> List.map(fun (_, test) -> test())

    let runTestsAndReportWith environment report (assembly:Assembly) = 
        assembly |> findTestsAndReport environment report |> List.map(fun (_, test) -> test())

    let findTests (assembly:Assembly) =  assembly |> findTestsAndReport emptyGlobal ignore

    let runTests (assembly:Assembly) = assembly |> runTestsAndReport ignore

    let runTestsWith (env : GlobalTestEnvironment) (assembly : Assembly) =
        assembly |> findTestsAndReport env ignore |> List.map(fun (_, test) -> test())
