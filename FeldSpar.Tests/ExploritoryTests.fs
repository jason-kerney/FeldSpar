namespace FeldSpar.Console.Tests
open FeldSpar.Console.Helpers.Data
open FeldSpar.Framework
open FeldSpar.Framework.Engine
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ChecksClean
open FeldSpar.Framework.Verification.ApprovalsSupport
open System

module ``F# division theory test should`` =
    let ``Division Theory`` = 
        {
            UnitDescription = (fun n -> sprintf "(%f * %f) / %f = %f" n n n n)
            UnitTest = (fun n _ ->
                            let v1 = n ** 2.0
                            let result = v1 / n

                            result |> expectsToBe n
            )
        }
          
    let ``Whole Doubles from 1.0 to 20.0`` = seq { 1.0..20.0 }  

    let ``divide and multiply and get the original number:`` =
        Theory({
                        Data = ``Whole Doubles from 1.0 to 20.0``
                        Base = ``Division Theory``
            })

module ``FeldSpar can`` =
    let ``ignore a test at compile time`` =
        ITest(fun env -> Success)

    let ``use a shuffle function to determine test order`` =
        Test(fun env ->
                let numbers = seq { for i in 1..100 do yield i } |> Seq.toArray

                let indices = [| 80; 10; 94; 44; 64; 13; 27; 57; 50; 59; 30; 90; 72; 54; 30; 95; 27; 54; 98; 20;
                                89; 25; 70; 89; 31; 63; 57; 79; 80; 98; 77; 54; 35; 47; 81; 68; 51; 69; 82; 44;
                                83; 51; 82; 93; 76; 45; 90; 69; 54; 85; 78; 51; 67; 68; 72; 88; 75; 60; 71; 63;
                                85; 80; 91; 80; 94; 95; 93; 82; 91; 89; 90; 88; 91; 96; 81; 91; 83; 92; 83; 80;
                                92; 81; 92; 83; 90; 94; 86; 95; 96; 98; 90; 92; 93; 96; 96; 95; 97; 97; 98; 99 |]

                let result = shuffle<int> numbers (fun (min, _) -> indices.[min]) |> Array.toList

                let envNew = env |> addReporter<ApprovalTests.Reporters.ClipboardReporter>

                result |> checkAgainstStandardObjectAsCleanedString envNew
            )

    let ``perform combinitoriy gold standard testing`` =
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

