namespace FeldSpar.Framework
open System
open ApprovalTests.Core
open FeldSpar.Framework.Sorting.Sorters

type FailureType =
    | GeneralFailure of string
    | ExpectationFailure of string
    | ExceptionFailure of Exception
    | Ignored of String
    | StandardNotMet of String
    | SetupFailure of FailureType

type TestResult =
    | Success
    | Failure of FailureType

type ExecutionSummary =
    {
        TestContainerName : string;
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

type GroupReport = 
    {
        TestContainerName : string;
        Failures : FailureReport[];
        Successes : string[];
    }

/// <summary>
/// Data about a test assembly results
/// </summary>
type OutputReport =
    {
        AssemblyName : string;
        Reports : GroupReport seq;
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
        ContainerName              : string;
        CanonicalizedContainerName : string;
        TestName                   : string;
        CanonicalizedName          : string;
        GoldStandardPath           : string;
        Assembly                   : Reflection.Assembly;
        AssemblyPath               : string;
        Reporters                  : (unit -> IApprovalFailureReporter) List;
    }


type TestTemplate = TestEnvironment -> TestResult


/// <summary>
/// A type represeting a unit test
/// </summary>
type Test = | Test of (TestTemplate)

/// <summary>
/// A type representing an ignored unit test
/// </summary>
type IgnoredTest = | ITest of (TestTemplate)

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
        UnitTest : 'a -> TestTemplate;
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
        TestContainerName: string;
        TestName: string;
        Test: Test;
    }

type UnitTest = 
    {
        Container: string;
        TestName: string;
        TestCase: unit -> ExecutionSummary;
    }

type IToken =
    abstract AssemblyName: string;
    abstract AssemblyPath: string;
    abstract Assembly: Reflection.Assembly;
    abstract IsDebugging: bool;
    abstract GetExportedTypes: unit -> Type[];
    

type RunningToken (assemblyPath) =
    interface IToken with
        member this.AssemblyPath = assemblyPath
        member this.AssemblyName = IO.Path.GetFileName assemblyPath
        member this.Assembly = assemblyPath |> IO.File.ReadAllBytes |> Reflection.Assembly.Load
        member this.IsDebugging = false
        member this.GetExportedTypes () = this.IToken.Assembly.GetExportedTypes()

    member this.IToken = this :> IToken


[<AutoOpen>]
module Utilities =
    /// <summary>
    /// Constructs a token for running tests
    /// </summary>
    /// <param name="assemblyPath">The path to a test assembly</param>
    let getToken assemblyPath = RunningToken(assemblyPath) :> IToken

    let withDebug (token:IToken) = { new IToken with
                                        member this.AssemblyPath = token.AssemblyPath;
                                        member this.AssemblyName = token.AssemblyName;
                                        member this.Assembly = token.Assembly;
                                        member this.IsDebugging = true;
                                        member this.GetExportedTypes () = token.GetExportedTypes ();
                                    }

    let withOutDebug (token:IToken) = { new IToken with
                                        member this.AssemblyPath = token.AssemblyPath;
                                        member this.AssemblyName = token.AssemblyName;
                                        member this.Assembly = token.Assembly;
                                        member this.IsDebugging = false;
                                        member this.GetExportedTypes () = token.GetExportedTypes ();
                                      }

    /// <summary>
    /// Gets the failure message from a result or an empty string if success
    /// </summary>
    /// <param name="result">Dhe result from which to get the message.</param>
    let getMessage result = 
        let rec getMessage result msg =
            match result with
            | Success -> String.Empty
            | Failure(StandardNotMet(m)) -> msg + m
            | Failure(GeneralFailure(m)) -> msg + m
            | Failure(ExceptionFailure(ex)) -> sprintf "%s%A" msg ex
            | Failure(ExpectationFailure(m)) -> msg + m
            | Failure(Ignored(m)) -> msg + m
            | Failure(SetupFailure(failure)) ->
                getMessage (Failure failure) "Before test failed with " 

        getMessage result ""

    let rec rebuild failure msg = 
        match failure with
        | GeneralFailure(_) -> GeneralFailure(msg)
        | ExpectationFailure(_) -> ExpectationFailure(msg)
        | ExceptionFailure(ex) -> ExceptionFailure(ex)
        | Ignored(_) -> Ignored(msg)
        | StandardNotMet(path) -> StandardNotMet(path)
        | SetupFailure(failure) -> SetupFailure (rebuild failure msg)

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
            let fail = rebuild failType msg

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
        let getFailureType r =
            match r with
            | Success -> GeneralFailure("FeldSpar Framework Reporting Error. Filter Broken")
            | Failure(fail) -> fail

        let groups = 
            results
                |> Seq.groupBy (fun result -> result.TestContainerName)

        let reports =
            groups
            |> Seq.map 
                (fun (group, res) ->
                    let res = 
                        res
                        |> Seq.sortBy (fun r -> r.TestName)

                    let successes = 
                        res
                        |> Seq.filter (fun r -> r.TestResults = Success)
                        |> Seq.map (fun r -> r.TestName)
                        |> Seq.toArray

                    let failures =
                        res
                        |> Seq.filter (fun r -> r.TestResults <> Success)
                        |> Seq.map
                            (fun r ->
                                {
                                    TestName = r.TestName;
                                    FailureType = r.TestResults |> getFailureType
                                }
                            )
                        |> Seq.toArray

                    {
                        TestContainerName = group;
                        Failures = failures;
                        Successes = successes;
                    }
                )

        let reports =
            reports |> Seq.sortBy (fun r -> r.TestContainerName)

        {
            AssemblyName = assemblyName;
            Reports = reports;
        }

    type SetupFlow<'a> =
        | ContinueFlow of TestResult * 'a * TestEnvironment
        | FlowFailed of FailureType * 'a Option

    let flowIt (execution : TestEnvironment -> TestResult * 'a * TestEnvironment) (onFailure: FailureType -> FailureType) env =
        let getNone failure : FailureType * 'a Option = failure, None
        let getSome data failure = failure, Some (data)

        try
            let result, data, newEnv = (execution env)
            match result with
            | Success -> ContinueFlow (result, data, newEnv)
            | Failure(reason) -> reason |> onFailure |> getSome data |> FlowFailed
        with
        | e -> e |> ExceptionFailure |> onFailure |> getNone |> FlowFailed

    let beforeTest (setup : TestEnvironment -> TestResult * 'a * TestEnvironment) = 
        fun env -> 
            flowIt setup SetupFailure env

    let theTest (test : TestEnvironment -> 'a  -> TestResult) (setup : TestEnvironment -> SetupFlow<'a>) : TestEnvironment -> SetupFlow<'a> =
        fun env ->
            match setup env with
            | ContinueFlow (result, data, newEnv) ->
                let noop x = x
                let testWrapper env = 
                    try
                        test env data, data, newEnv
                    with 
                    | e -> ExceptionFailure e |> Failure, data, newEnv
                flowIt testWrapper noop env
            | result -> result

    let afterTheTest (teardown : TestEnvironment -> TestResult -> 'a Option -> TestResult) (test : TestEnvironment -> SetupFlow<'a>) : TestTemplate =
        fun env ->
            match test env with
            | ContinueFlow (result, data, newEnv) ->
                teardown newEnv result (Some data)
            | FlowFailed (failure, data) -> 
                let testFailure = Failure failure
                match teardown env testFailure data with
                | Success -> testFailure
                | tearDownFailure -> failwith "after test failure not implimented"

    let startWithTheTest (test: TestEnvironment -> TestResult) : TestEnvironment -> SetupFlow<unit> =
        fun env -> 
            try
                let result = test env
                match result with
                | Success -> ContinueFlow (Success, (), env)
                | Failure(failure) -> FlowFailed (failure, Some ())
            with
            | e -> FlowFailed (e |> ExceptionFailure, Some ())

    let endWithTest (test : TestEnvironment -> 'a  -> TestResult) (setup : TestEnvironment -> SetupFlow<'a>) =
        fun env ->
            let setupResult = setup env
            match setupResult with
            | ContinueFlow (result, data, newEnv) -> test newEnv data
            | FlowFailed (result, _) -> result |> Failure