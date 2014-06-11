namespace FeldSpar.Console.Tests
open FeldSpar.Framework
open FeldSpar.Framework.TestSummaryUtilities
open FeldSpar.Framework.Verification
open FeldSpar.Console.Helpers.Data

module FilteringTests = 
    let testFilterByFailures = 
        Test({
                Description = "filterByFailures should remove any non failing tests summaries from a collection of result summaries";
                UnitTest = (fun env ->
                                let hasOnlySuccesses, hasOnlyFailures, hasMixedResults = filteringSetUp

                                let testResults = [hasOnlySuccesses; hasOnlyFailures; hasMixedResults;]

                                let successFul = 
                                        testResults 
                                        |> Seq.filter (fun r ->  r.TestResults = Success) 
                                        |> Seq.map (fun r -> r.TestDescription) 
                                        |> Seq.sort
                                        |> Seq.toList

                                let filterResult = testResults |> reduceToFailures |> Seq.toList

                                let filteredAndPassing = 
                                        filterResult 
                                        |> Seq.filter (fun r -> r.TestResults = Success)
                                        |> Seq.map (fun r -> r.TestDescription)
                                        |> Seq.sort
                                        |> Seq.toList

                                verify
                                    {
                                        let! correctNumberOfFailures = (filterResult |> List.length) |> expectsToBe 2 "Incorrect number of test results after filtering expected %d but got %d"
                                        let! noSuccessFound = (filteredAndPassing |> List.length) |> expectsToBe 0 "Incorrect number of passing tests found. Expected %d after filtering but got %d"
                                        return Success
                                    }
                            )
            })
