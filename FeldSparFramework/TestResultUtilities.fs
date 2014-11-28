namespace FeldSpar.Framework

module TestResultUtilities =
    /// <summary>
    /// Returns true if the result is a Failue
    /// </summary>
    /// <param name="result">the result to check</param>
    let filterFailures result = result <> Success

    /// <summary>
    /// Returns true if the result is a Success
    /// </summary>
    /// <param name="result">the result to check</param>
    let filterSuccesses result = result = Success

    /// <summary>
    /// Filters a sequence of tests by a filter criteria. Returns indeterminate if the sequence of results is empty.
    /// </summary>
    /// <param name="filter">the filter criteria</param>
    /// <param name="results">the results to filter</param>
    let filterBy (filter: TestResult -> bool) results =
        if results |> Seq.isEmpty
        then [indeterminateTest] |> Seq.ofList
        else results |> Seq.filter filter

    /// <summary>
    /// Reduce a sequence of results down to only the failures. Returns indeterminate if the sequence of results is empty.
    /// </summary>
    /// <param name="results">The results to filtes</param>
    let filterByFailures (results : TestResult list) =
        results |> filterBy filterFailures

    /// <summary>
    /// Reduce a sequence of results down to only the Successes. Returns indeterminate if the sequence of results is empty.
    /// </summary>
    /// <param name="results">The results to filtes</param>
    let filterBySuccess (results : TestResult list) = 
        results |> filterBy filterSuccesses
