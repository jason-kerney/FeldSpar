namespace FeldSpar.Console.Helpers
open FeldSpar.Framework
open FeldSpar.Framework.Engine

module Data =
    let assembly = System.Reflection.Assembly.GetExecutingAssembly()

    let runTest description template = 
            let _, test = template |> createTestFromTemplate { Reporters = [] } ignore description assembly
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


