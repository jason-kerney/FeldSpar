namespace FeldSpar.Framework
open System

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

type TestEnvironment =
    {
        Name:string;
        CanonicalizedName : string;
        RootPath : string;
        Token : Guid
    }

type TestTemplate =
    {
        Description : string;
        UnitTest : TestEnvironment -> TestResult;
    }

type Test =
    | Test of TestTemplate

(*
type TheoryCaseTemplate<'a> =
    {
        UnitDescription : 'a -> string;
        UnitTest : TestEnvironment -> 'a -> TestResult list;
    }
    
type TestTheoryTemplate<'a> =
    {
        Description : string;
        Data : seq<'a>;
        Template : TheoryCaseTemplate<'a>;
    }

type Theory<'a> =
    | Theory of TestTheoryTemplate<'a>

//*)


[<AutoOpen>]
module Utilities =
    let ignoreWith message = Failure(Ignored(message))

    let failException ex = Failure(ExceptionFailure(ex))

    let failResult message = Failure(GeneralFailure(message))

    let notYetImplemented = ignoreWith "Test not yet implemented"

    let indeterminateTest = ignoreWith "Indeterminate Test Result"
