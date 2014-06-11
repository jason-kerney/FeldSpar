namespace FeldSpar.Framework

module TestResultUtilities =
    let filterFailures result = match result with | Success -> false | _ -> true
    let filterSuccesses result = match result with | Success -> true | _ -> true

    let filterBy (filter: TestResult -> bool) results =
        if results |> Seq.isEmpty
        then [indeterminateTest] |> Seq.ofList
        else results |> Seq.filter filter

    let filterByFailures (results : TestResult list) =
        results |> filterBy filterFailures

    let filterBySuccess (results : TestResult list) = 
        results |> filterBy filterSuccesses