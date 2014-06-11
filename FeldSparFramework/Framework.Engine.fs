namespace FeldSpar.Framework.Engine
open System
open FeldSpar.Framework
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Formatters
open System.Reflection

type ExecutionToken =
    {
        Description: string;
        Token: Guid;
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

    let private executeInNewDomain (input : 'a) (action : 'a -> 'b) =
        let appDomain = AppDomain.CreateDomain("AppDomainHelper.ExecuteInNewAppDomain") 

        let exicutionType = typeof<Executioner<'a, 'b>>
        let sandBoxName = exicutionType.Name
        let sandBoxAssemblyName = exicutionType.Assembly.GetName () |> fun assembly -> assembly.FullName
        let sandBoxTypeName = exicutionType.FullName

        let sandbox = appDomain.CreateInstanceAndUnwrap(sandBoxAssemblyName, sandBoxTypeName) :?> Executioner<'a, 'b>

        sandbox.Execute input action

    let private createEnvironment (template: TestTemplate) = 
        let rec getSourcePath path = 
            let p = System.IO.DirectoryInfo(path)

            let filters = ["bin"; "Debug"; "Release"]

            match filters |> List.tryFind (fun bad -> p.Name = bad) with
            | Some(_) -> p.Parent.FullName |> getSourcePath
            | None -> p.FullName + "\\"

        let path = System.Environment.CurrentDirectory |> getSourcePath
            
        { 
            CanonicalizedName = template.Description |> Formatters.Basic.CanonicalizeString;
            RootPath = path
            Token = Guid.NewGuid ()
        }

    let private fileFoundReport (env:TestEnvironment) report (template:TestTemplate) =
        Found({Description = template.Description; Token = env.Token}) |> report 

    let private fileRunningReport (env:TestEnvironment) report (template:TestTemplate) =
        Running({Description = template.Description; Token = env.Token}) |> report 

    let private fileFinishedReport (env:TestEnvironment) report (template:TestTemplate) (result:TestResult) =
        Finished({Description = template.Description; Token = env.Token}, result) |> report 

    let createTestFromTemplate (report : ExecutionStatus -> unit ) (Test(template)) =
        let env = template |> createEnvironment

        template |> fileFoundReport env report

        let testCase = (fun() -> 
                            let testingCode = (fun () ->
                                                    try
                                                        let result = 
                                                            {
                                                                TestDescription = template.Description;
                                                                TestCanonicalizedName = env.CanonicalizedName;
                                                                TestResults = template.UnitTest env;
                                                            }

                                                        result.TestResults |> fileFinishedReport env report template

                                                        result
                                                    with
                                                    | e -> 
                                                        let result = 
                                                            {
                                                                TestDescription = template.Description;
                                                                TestCanonicalizedName = env.CanonicalizedName;
                                                                TestResults = Failure(ExceptionFailure(e));
                                                            }

                                                        result.TestResults |> fileFinishedReport env report template
                                                        
                                                        result
                                                )

                            template |> fileRunningReport env report
                            testingCode |> executeInNewDomain ()
                        )
                        
        (template.Description, testCase)
        

    let reportResults results = Basic.reportResults results

    let private runTestCode (_, test: unit -> ExecutionSummary) = test()

    let findTestsAndReport report (assembly:Assembly) = 
        (assembly.GetExportedTypes())
            |> List.ofSeq
            |> List.map (fun t -> t.GetMethods(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Static))
            |> List.toSeq
            |> Array.concat
            |> List.ofArray
            |> List.filter(fun m -> m.ReturnType = typeof<Test>)
            |> List.map(fun m -> m.Invoke(null, Array.empty) :?> Test)
            |> List.map(fun test -> test |> createTestFromTemplate report)

    let runTestsAndReport report (assembly:Assembly) = 
        assembly |> findTestsAndReport report |> List.map(fun (_, test) -> test())

    let findTests (assembly:Assembly) =  assembly |> findTestsAndReport ignore

    let runTests (assembly:Assembly) = assembly |> runTestsAndReport ignore
