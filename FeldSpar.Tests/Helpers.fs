namespace FeldSpar.Console.Helpers
open FeldSpar.Framework
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification.ApprovalsSupport
open ApprovalTests

module Data =
    let buildType =
#if DEBUG
        "Debug"
#else
        "Release"
#endif

    let loadToken (env:TestEnvironment) = { new IToken with
                                                member this.Assembly = env.Assembly;
                                                member this.AssemblyName = env.AssemblyPath |> System.IO.Path.GetFileName;
                                                member this.AssemblyPath = env.AssemblyPath;
                                                member this.GetExportedTypes () = env.Assembly.GetExportedTypes()
                                                member this.IsDebugging = false;
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

    let runTest assemblyPath template = 
            let { TestName = _; TestCase = test } = template |> createTestFromTemplate { Reporters = [] } ignore assemblyPath
            test()

    let runAsTests assemblyPath templates = 
        templates |> Seq.map (fun info -> info |> runTest assemblyPath)

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


