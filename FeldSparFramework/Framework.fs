namespace FeldSpar.Framework
open System
open ApprovalTests.Core

type FailureType =
    | GeneralFailure of string
    | ExpectationFailure of string
    | ExceptionFailure of Exception
    | Ignored of String
    | StandardNotMet

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