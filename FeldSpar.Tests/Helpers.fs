namespace FeldSpar.Console.Helpers
open FeldSpar.Framework
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification.ApprovalsSupport
open ApprovalTests

module Data =
    type internal Marker = interface end
    let testFeldSparAssembly = typeof<Marker>.Assembly

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

    let runTest template = 
            let _, test = template |> createTestFromTemplate ignore
            test()

    let runAsTests templates = 
        templates |> Seq.map (fun (unitTest) -> unitTest |> runTest)

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


