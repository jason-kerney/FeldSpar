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

