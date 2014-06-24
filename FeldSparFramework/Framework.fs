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
        TestResults : TestResult
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
        Assembly : System.Reflection.Assembly;
        Reporters : (unit -> IApprovalFailureReporter) List;
    }

type Test = | Test of (TestEnvironment -> TestResult)
type IgnoredTest = | ITest of (TestEnvironment -> TestResult)
type Configuration = | Config of (unit -> AssemblyConfiguration)
type TheoryMap = | Theory of (string -> (string * Test)[])


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
    | Template of TestTheoryTemplate<'a>

[<AutoOpen>]
module Utilities =
    let ignoreWith message = Failure(Ignored(message))

    let failException ex = Failure(ExceptionFailure(ex))

    let failResult message = Failure(GeneralFailure(message))

    let notYetImplemented = ignoreWith "Test not yet implemented"

    let indeterminateTest = ignoreWith "Indeterminate Test Result"

    let calledWithEachOfThese (items: 'a seq) call =
        seq { for i in items do
                yield call i }
            
    let andAlsoEachOfThese items (calls: ('a -> 'b) seq) =
        seq { for callWith in calls do
                 for item in items do 
                    yield callWith item }
