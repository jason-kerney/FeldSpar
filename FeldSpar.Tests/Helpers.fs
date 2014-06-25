namespace FeldSpar.Console.Helpers
open FeldSpar.Framework
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification.ApprovalsSupport
open ApprovalTests

module Data =
    type internal Marker = interface end
    let testAssembly = typeof<Marker>.Assembly

    let ``Setup Global Reports`` = 
        Config(fun () -> 
        { 
            Reporters = [
                            fun () -> 
                                    Searching
                                        |> findFirstReporter<Reporters.DiffReporter>
                                        |> findFirstReporter<Reporters.WinMergeReporter>
                                        |> findFirstReporter<Reporters.NotepadLauncher>
                                        |> unWrapReporter
                                            
                            fun () -> Reporters.ClipboardReporter() :> Core.IApprovalFailureReporter;
                        ] 
        })

    let runTest description template = 
            let _, test = template |> createTestFromTemplate { Reporters = [] } ignore description testAssembly
            test()

    let runAsTests templates = 
        templates |> Seq.map (fun (description, template) -> template |> runTest description)

    let filteringSetUp = 
        let hasOnlySuccesses =
            {
                TestDescription = "successes only test";
                TestCanonicalizedName = "";
                TestResults = Success
            }

        let hasOnlyFailures =
            {
                TestDescription = "failures test";
                TestCanonicalizedName = "";
                TestResults = Failure(ExceptionFailure(new System.Exception()));
            }

        let hasMixedResults = 
            {
                TestDescription = "mixed test";
                TestCanonicalizedName = "";
                TestResults = Failure(GeneralFailure("This is a failure")); 
            }

        (hasOnlySuccesses, hasOnlyFailures, hasMixedResults)


