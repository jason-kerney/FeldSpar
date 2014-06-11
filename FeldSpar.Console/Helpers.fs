namespace FeldSpar.Console.Helpers
open FeldSpar.Framework
open FeldSpar.Framework.Engine

module Data =
    type internal Marker = interface end
    let assembly = typeof<Marker>.Assembly

    let runTest template = 
            let desc = 
                match template with
                | Test({Description = d; UnitTest = _}) ->d

            let _, test = template |> createTestFromTemplate ignore desc
            test()

    let runAsTests templates = 
        templates |> Seq.map (fun template -> template |> runTest)

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


