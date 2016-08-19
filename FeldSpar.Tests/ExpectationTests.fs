namespace FeldSpar.Console.Tests
open FeldSpar.Framework
open FeldSpar.Framework.Verification

module ExpectationTests =
    let ``expectsNotToBe will succeed for "5" expectsNotToBe "6"`` =
        Test(
            fun _ ->
                "5" |> expectsNotToBe "6"
        )

    let ``expectsNotToBe fails if equal`` =
        Test(
            fun _ ->
                let result = "some" |> expectsNotToBe "some"

                result |> expectsToBe (Failure(ExpectationFailure("\"some\" expected not to be \"some\"")))
        )

    let ``isNull succeeds if null`` =
        Test(
            fun _ ->
                null |> isNull
        )

    let ``isNull fails if nof null`` =
        Test(
            fun _ ->
                let result = "" |> isNull

                result |> expectsNotToBe Success
        )

    let ``expectsToBeTrue passes when true`` =
        Test(
            fun _ ->
                true |> expectsToBeTrue
        )


    let ``expectsToBeTrue fails when false`` =
        Test(
            fun _ ->
                let result = false |> expectsToBeTrue
                result |> expectsNotToBe Success
        )

    let ``expectsToBeFalse passes when false`` =
        Test(
            fun _ ->
                false |> expectsToBeFalse
        )


    let ``expectsToBeFalse fails when true`` =
        Test(
            fun _ ->
                let result = true |> expectsToBeFalse
                result |> expectsNotToBe Success
        )

    let ``expectToContain will pass when given a sub list`` =
        Test(fun _ ->
            let list = ["a"; "b"; "c"; "d"; "e"]
            let sublist = ["b"; "c"; "d"]

            list 
                |> expectsToContain sublist
                |> expectsToBe Success
        )

    let ``expectToContain will fail when given a sub list that has a new item`` =
        Test(fun _ ->
            let list = [1; 2; 3; 4; 5; 6]
            let sublist = [2; 3; 100; 4]

            list 
                |> expectsToContain sublist
                |> expectsToBe (Failure(ExpectationFailure("[2; 3; 100; 4] expected to be contained in [1; 2; 3; 4; 5; 6]")))
        )

    let ``expectToContain will pass win a sub sequence is out of order`` =
        Test(fun _ ->
            let root = "Hello World"
            let sub = "WHo"

            root 
                |> expectsToContain sub
                |> expectsToBe Success
        )

    let ``expectsNotToContain will pass if any element of a sequence differs`` =
        Test(fun _ ->
            let root = seq { for y in 1..10 do yield y}
            let partial = [8; 10; 12]

            root
                |> expectsToNotContain partial
                |> expectsToBe Success
        )

    let ``expectsNotToContain will fail if all elements are contained`` =
        Test(fun _ ->
            let root = seq { for y in 1..10 do yield y.ToString ()}
            let partial = ["1"; "3"; "10"]

            root
                |> expectsToNotContain partial
                |> expectsToBe Success
        )
