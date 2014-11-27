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
        TestDescription : string;
        TestCanonicalizedName : string;
        TestResults : TestResult;
    }

type FailureReport = 
    {
        Name : string;
        FailureType : FailureType;
    }

type OutputReport =
    {
        Name : string;
        Failures : FailureReport[];
        Successes : string[];
    }
    
type AssemblyConfiguration =
    {
        Reporters : (unit -> IApprovalFailureReporter) List;
    }

type TestEnvironment =
    {
        Name:string;
        CanonicalizedName : string;
        RootPath : string;
        Assembly : Reflection.Assembly;
        AssemblyPath : string;
        Reporters : (unit -> IApprovalFailureReporter) List;
    }

type Test = | Test of (TestEnvironment -> TestResult)
type IgnoredTest = | ITest of (TestEnvironment -> TestResult)
type Configuration = | Config of (unit -> AssemblyConfiguration)
type RunConfiguration = 
    {
        Assembly: Reflection.Assembly;
        Config: Configuration option;
    }

type TheoryCaseTemplate<'a> =
    {
        UnitDescription : 'a -> string;
        UnitTest : 'a -> TestEnvironment -> TestResult;
    }
    
type TestTheoryTemplate<'a> =
    {
        Data : seq<'a>;
        Base : TheoryCaseTemplate<'a>;
    }

type Theory<'a> =
    | Theory of TestTheoryTemplate<'a>

[<AutoOpen>]
module Utilities =
    let getMessage result = 
        match result with
        | Success -> String.Empty
        | Failure(StandardNotMet(m)) -> m
        | Failure(GeneralFailure(m)) -> m
        | Failure(ExceptionFailure(ex)) -> sprintf "%A" ex
        | Failure(ExpectationFailure(m)) -> m
        | Failure(Ignored(m)) -> m

    let addMessage message result = 
        match result with
        | Success | Failure(StandardNotMet(_)) | Failure(ExceptionFailure(_)) -> result
        | Failure(failType) ->
            let msg = message + "\n" + (result |> getMessage )
            let fail = 
                match failType with
                | GeneralFailure(_) -> GeneralFailure(msg)
                | ExpectationFailure(_) -> ExpectationFailure(msg)
                | ExceptionFailure(ex) -> ExceptionFailure(ex)
                | Ignored(_) -> Ignored(msg)
                | StandardNotMet(path) -> StandardNotMet(path)

            Failure(fail)
            

    let ignoreWith message = Failure(Ignored(message))

    let failException ex = Failure(ExceptionFailure(ex))

    let failResult message = Failure(GeneralFailure(message))

    let ``Not Yet Implemented`` = ignoreWith "Test not yet implemented"

    let indeterminateTest = ignoreWith "Indeterminate Test Result"

    let calledWithEachOfThese (items: 'a seq) call =
        seq { for i in items do
                yield call i }
            
    let andAlsoEachOfThese items (calls: ('a -> 'b) seq) =
        seq { for callWith in calls do
                 for item in items do 
                    yield callWith item }

    let buildOutputReport (name, results:ExecutionSummary seq) =
        let successes = 
            results
            |> Seq.filter (fun result -> result.TestResults = Success)
            |> Seq.sortBy (fun result -> result.TestDescription)
            |> Seq.map (fun result -> result.TestDescription)
            |> Seq.toArray

        let failures =
            results
            |> Seq.filter (fun result -> result.TestResults <> Success)
            |> Seq.sortBy (fun result -> result.TestDescription)
            |> Seq.map(fun { TestDescription = name; TestCanonicalizedName = _ ; TestResults = Failure(failType) } -> 
                {
                    Name = name;
                    FailureType = failType;
                }
            )
            |> Seq.toArray

        {
            Name = name;
            Failures = failures;
            Successes = successes;
        }