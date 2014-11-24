namespace FeldSpar.Console.Tests
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ChecksClean
open FeldSpar.Framework.Verification.ApprovalsSupport
open System

module ExploritoryTests =
    let ``Division Theory`` = 
        {
            UnitDescription = (fun n -> sprintf " (%f * %f) / %f = %f" n n n n)
            UnitTest = (fun n _ ->
                            let v1 = n ** 2.0
                            let result = v1 / n

                            result |> expectsToBe n "(%f <> %f)"
            )
        }
          
    let ``Whole Doubles from 1.0 to 20.0`` = seq { 1.0..20.0 }  

    let ``Here is a second theory test`` =
        Theory({
                        Data = ``Whole Doubles from 1.0 to 20.0``
                        Base = ``Division Theory``
            })

    let ``This is a theory Test`` =
        Theory({
                    Data = [
                                (1, "1");
                                (2, "2");
                                (3, "Fizz");
                                (5,"Buzz");
                                (6, "Fizz");
                                (10,"Buzz");
                                (15,"FizzBuzz")
                    ] |> List.toSeq
                    Base = 
                    {
                        UnitDescription = (fun (n,s) -> sprintf "test converts %d into \"%s\"" n s)
                        UnitTest = 
                            (fun (n, expected) _ ->
                                let result = 
                                    match n with
                                    | v when v % 15 = 0 -> "FizzBuzz"
                                    | v when v % 5 = 0 -> "Buzz"
                                    | v when v % 3 = 0 -> "Fizz"
                                    | v -> v.ToString()

                                result |> expectsToBe expected "did not convert n correctly. Expected \"%s\" but got \"%s\""
                            )
                    }
            })

        
    let ``This is an ignored test`` =
        ITest(fun env -> Success)

    let ``Test that shuffle works correctly`` =
        Test(fun env ->
                let numbers = seq { for i in 1..100 do yield i } |> Seq.toArray

                let indices = [| 80; 10; 94; 44; 64; 13; 27; 57; 50; 59; 30; 90; 72; 54; 30; 95; 27; 54; 98; 20;
                                89; 25; 70; 89; 31; 63; 57; 79; 80; 98; 77; 54; 35; 47; 81; 68; 51; 69; 82; 44;
                                83; 51; 82; 93; 76; 45; 90; 69; 54; 85; 78; 51; 67; 68; 72; 88; 75; 60; 71; 63;
                                85; 80; 91; 80; 94; 95; 93; 82; 91; 89; 90; 88; 91; 96; 81; 91; 83; 92; 83; 80;
                                92; 81; 92; 83; 90; 94; 86; 95; 96; 98; 90; 92; 93; 96; 96; 95; 97; 97; 98; 99 |]

                let result = shuffle<int> numbers (fun (min, _) -> indices.[min]) |> Array.toList

                let env = env |> addReporter<ApprovalTests.Reporters.ClipboardReporter>

                result |> checkAgainstStandardObjectAsCleanedString env
            )

    let ``Combinatory Gold Standard Testing`` =
        Test
            (
                fun env ->
                    let names = ["Tom"; "Jane"; "Tarzan"; "Stephanie"]
                    let amounts = [11; 2; 5;]
                    let items = ["pears";"earrings";"cups"]

                    let createSentence item amount name = sprintf "%s has %d %s" name amount item

                    createSentence
                        |> calledWithEachOfThese items
                        |> andAlsoEachOfThese amounts
                        |> andAlsoEachOfThese names
                        |> checkAllAgainstStandardCleaned env
            )

