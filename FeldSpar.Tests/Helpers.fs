namespace FeldSpar.Console.Helpers
open FeldSpar.Framework
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification.ApprovalsSupport
open ApprovalTests

module Data =
    type internal Marker = interface end
    let testFeldSparAssembly = typeof<Marker>.Assembly

    let testToken = { new IToken with
                        member this.AssemblyPath = testFeldSparAssembly.Location;
                        member this.AssemblyName = System.IO.Path.GetFileName this.AssemblyPath;
                        member this.Assembly = this.AssemblyPath |> System.IO.File.ReadAllBytes |> System.Reflection.Assembly.Load
                        member this.GetExportedTypes () = this.Assembly.GetExportedTypes()
                    }

    let ``Setup Global Reports`` = 
        Config(fun () -> 
        { 
            Reporters = [
                            fun _ -> 
                                    Searching
                                        |> findFirstReporter<Reporters.DiffReporter>
                                        |> findFirstReporter<Reporters.WinMergeReporter>
                                        |> findFirstReporter<Reporters.NotepadLauncher>
                                        |> unWrapReporter
                                            
                            fun _ -> Reporters.ClipboardReporter() :> Core.IApprovalFailureReporter;

                            fun _ -> Reporters.QuietReporter() :> Core.IApprovalFailureReporter;
                        ]
        })

    let runTest description assemblyPath template = 
            let _, test = template |> createTestFromTemplate { Reporters = [] } ignore description assemblyPath testFeldSparAssembly
            test()

    let runAsTests assemblyPath templates = 
        templates |> Seq.map (fun (description, template) -> template |> runTest description assemblyPath)

    let filteringSetUp = 
        let hasOnlySuccesses =
            {
                TestName = "successes only test";
                TestCanonicalizedName = "";
                TestResults = Success
            }

        let hasOnlyFailures =
            {
                TestName = "failures test";
                TestCanonicalizedName = "";
                TestResults = Failure(ExceptionFailure(new System.Exception()));
            }

        let hasMixedResults = 
            {
                TestName = "mixed test";
                TestCanonicalizedName = "";
                TestResults = Failure(GeneralFailure("This is a failure")); 
            }

        (hasOnlySuccesses, hasOnlyFailures, hasMixedResults)


