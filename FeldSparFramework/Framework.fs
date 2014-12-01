namespace FeldSpar.Framework
open System
open ApprovalTests.Core

type FailureType =
    | GeneralFailure of string
    | ExpectationFailure of string
    | ExceptionFailure of Exception
    | Ignored of String
    | StandardNotMet of String

type TestResult =
    | Success
    | Failure of FailureType

type ExecutionSummary =
    {
        TestName : string;
        TestCanonicalizedName : string;
        TestResults : TestResult;
    }

/// <summary>
/// Data about a failing test
/// </summary>
type FailureReport = 
    {
        TestName : string;
        FailureType : FailureType;
    }

/// <summary>
/// Data about a test assembly results
/// </summary>
type OutputReport =
    {
        AssemblyName : string;
        Failures : FailureReport[];
        Successes : string[];
    }
    
/// <summary>
/// A type used to configure reporters for gold standard (approval tests)
/// </summary>
type AssemblyConfiguration =
    {
        Reporters : (unit -> IApprovalFailureReporter) List;
    }

/// <summary>
/// Information about the current executing test evironment
/// </summary>
type TestEnvironment =
    {
        TestName:string;
        CanonicalizedName : string;
        GoldStandardPath : string;
        Assembly : Reflection.Assembly;
        AssemblyPath : string;
        Reporters : (unit -> IApprovalFailureReporter) List;
    }

/// <summary>
/// A type represeting a unit test
/// </summary>
type Test = | Test of (TestEnvironment -> TestResult)

/// <summary>
/// A type representing an ignored unit test
/// </summary>
type IgnoredTest = | ITest of (TestEnvironment -> TestResult)

/// <summary>
/// A type to allow the dynamic loading of configuration if it is used
/// </summary>
type Configuration = | Config of (unit -> AssemblyConfiguration)


/// <summary>
/// Data representing a therory or parameterized test.
/// </summary>
type TheoryCaseTemplate<'a> =
    {
        UnitDescription : 'a -> string;
        UnitTest : 'a -> TestEnvironment -> TestResult;
    }
    
/// <summary>
/// Data that shows how to combine a theory test with data
/// </summary>
type TestTheoryTemplate<'a> =
    {
        Data : seq<'a>;
        Base : TheoryCaseTemplate<'a>;
    }

/// <summary>
/// A type used to creat a theory test case
/// </summary>
type Theory<'a> =
    | Theory of TestTheoryTemplate<'a>

type TestInformation =
    {
        TestName: string;
        Test: Test;
    }

type UnitTest = 
    {
        TestName: string;
        TestCase: unit -> ExecutionSummary;
    }

type IToken =
    abstract AssemblyName: string;
    abstract AssemblyPath: string;
    abstract Assembly: Reflection.Assembly;
    abstract GetExportedTypes: unit -> Type[];
    

type RunningToken (assemblyPath) =
    interface IToken with
        member this.AssemblyPath = assemblyPath
        member this.AssemblyName = IO.Path.GetFileName assemblyPath
        member this.Assembly = assemblyPath |> IO.File.ReadAllBytes |> Reflection.Assembly.Load
        member this.GetExportedTypes () = this.IToken.Assembly.GetExportedTypes()

    member this.IToken = this :> IToken

[<AutoOpen>]
module Utilities =
    /// <summary>
    /// Constructs a token for running tests
    /// </summary>
    /// <param name="assemblyPath">The path to a test assembly</param>
    let getToken assemblyPath = RunningToken(assemblyPath) :> IToken

    /// <summary>
    /// A way to programaticly know if in release or debug.
    /// </summary>
    let buildType =
#if DEBUG
        "Debug"
#else
        "Release"
#endif

    /// <summary>
    /// Gets the failure message from a result or an empty string if success
    /// </summary>
    /// <param name="result">Dhe result from which to get the message.</param>
    let getMessage result = 
        match result with
        | Success -> String.Empty
        | Failure(StandardNotMet(m)) -> m
        | Failure(GeneralFailure(m)) -> m
        | Failure(ExceptionFailure(ex)) -> sprintf "%A" ex
        | Failure(ExpectationFailure(m)) -> m
        | Failure(Ignored(m)) -> m

    /// <summary>
    /// Adds an additional comment to the beginnig of a failure message
    /// </summary>
    /// <param name="comment">The comment that is added</param>
    /// <param name="result">The result to add the comment to. The comment is only added if the result is a failure</param>
    let withFailComment comment result = 
        match result with
        | Success | Failure(StandardNotMet(_)) | Failure(ExceptionFailure(_)) -> result
        | Failure(failType) ->
            let msg = comment + "\n" + (result |> getMessage )
            let fail = 
                match failType with
                | GeneralFailure(_) -> GeneralFailure(msg)
                | ExpectationFailure(_) -> ExpectationFailure(msg)
                | ExceptionFailure(ex) -> ExceptionFailure(ex)
                | Ignored(_) -> Ignored(msg)
                | StandardNotMet(path) -> StandardNotMet(path)

            Failure(fail)
            

    /// <summary>
    /// Returns a Failure result with failure type of 'Ignored' and with the desired message
    /// </summary>
    /// <param name="message"></param>
    let ignoreWith message = Failure(Ignored(message))

    /// <summary>
    /// Wraps an exception into a failure result with a failure type of 'FailureException'
    /// </summary>
    /// <param name="ex">The exception to convert to a failure result</param>
    let failException ex = Failure(ExceptionFailure(ex))

    /// <summary>
    /// Creates a failure result of type 'GeneralFailure' with a desired message
    /// </summary>
    /// <param name="message">The message to wrap into a failure result</param>
    let failResult message = Failure(GeneralFailure(message))

    /// <summary>
    /// Returns a failure result of failure type 'Ignored' with a message indicating test is not yet implemented
    /// </summary>
    let ``Not Yet Implemented`` = ignoreWith "Test not yet implemented"

    /// <summary>
    /// Returns a failure result of failure type 'Ignored' with a message indicating test is not yet implemented
    /// </summary>
    let ``Not yet implemented`` = ``Not Yet Implemented``

    /// <summary>
    /// Returns a failure result of failure type 'Ignored' with a message indicating test is not yet implemented
    /// </summary>
    let NotYetImplemented = ``Not Yet Implemented``


    /// <summary>
    /// Returns a failure result of failure type 'Ignored' with a message indicating test has an indeterminate test result.
    /// </summary>
    let indeterminateTest = ignoreWith "Indeterminate Test Result"

    /// <summary>
    /// An alias for Seq.map
    /// </summary>
    /// <param name="items">the items to map</param>
    /// <param name="call">the function that is called on each item</param>
    let calledWithEachOfThese (items: 'a seq) call =
        items |> Seq.map call
      
    /// <summary>
    /// Maps all items using each call function and returns sequence of all permutations
    /// </summary>
    /// <param name="items">the items to map</param>
    /// <param name="calls">the mapping functions that will get called on each item</param>
    let andAlsoEachOfThese items (calls: ('a -> 'b) seq) =
        seq { for callWith in calls do
                 for item in items do 
                    yield callWith item }

    /// <summary>
    /// Takes results and divides them up by success and failures
    /// </summary>
    /// <param name="name">The name of the test assembly</param>
    /// <param name="results">the test results</param>
    let buildOutputReport (assemblyName, results:ExecutionSummary seq) =
        let successes = 
            results
            |> Seq.filter (fun result -> result.TestResults = Success)
            |> Seq.sortBy (fun result -> result.TestName)
            |> Seq.map (fun result -> result.TestName)
            |> Seq.toArray

        let failures =
            results
            |> Seq.filter (fun result -> result.TestResults <> Success)
            |> Seq.sortBy (fun result -> result.TestName)
            |> Seq.map(fun { TestName = testName; TestCanonicalizedName = _ ; TestResults = Failure(failType) } -> 
                {
                    TestName = testName;
                    FailureType = failType;
                }
            )
            |> Seq.toArray

        {
            AssemblyName = assemblyName;
            Failures = failures;
            Successes = successes;
        }