namespace FeldSpar.Console.Tests
open FeldSpar.Framework
open FeldSpar.Framework.Verification

module ExpectationTests =
    let ``expectsNotToBe will succeed for "5" expectsNotToBe "6"`` =
        Test(
            fun _ ->
                "5" |> expectsNotToBe "6" "'%s' was not suposed to be '%s'"
        )

    let ``expectsNotToBe fails if equal`` =
        Test(
            fun _ ->
                let result = "some" |> expectsNotToBe "some" "%s = %s"

                result |> expectsToBe (Failure(ExpectationFailure("some = some"))) "%A <> %A"
        )

    let ``isNull succeeds if null`` =
        Test(
            fun _ ->
                null |> isNull "%A not %A"
        )

    let ``isNull fails if nof null`` =
        Test(
            fun _ ->
                let result = "" |> isNull "'%A' is not '%A'"

                result |> expectsNotToBe Success "%A should not be %A"
        )

    let ``expectsToBeTrue passes when true`` =
        Test(
            fun _ ->
                true |> expectsToBeTrue "%b is not %b"
        )


    let ``expectsToBeTrue fails when false`` =
        Test(
            fun _ ->
                let result = false |> expectsToBeTrue "%b is not %b"
                result |> expectsNotToBe Success "false should not be true '%A' = '%A'"
        )

    let ``expectsToBeFalse passes when false`` =
        Test(
            fun _ ->
                false |> expectsToBeFalse "%b is not %b"
        )


    let ``expectsToBeFalse fails when true`` =
        Test(
            fun _ ->
                let result = true |> expectsToBeFalse "%b is not %b"
                result |> expectsNotToBe Success "true should not be false '%A' = '%A'"
        )

