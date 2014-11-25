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

