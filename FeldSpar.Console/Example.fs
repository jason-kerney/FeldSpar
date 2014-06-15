namespace FeldSpar.Console.Tests

module Example =
    open FeldSpar.Framework
    open FeldSpar.Framework.Verification
    open FeldSpar.Framework.Verification.Checks
    open FeldSpar.Console.Helpers.Data

    let FizzBuzzer v = 
        match v with
        | fizz when fizz % 3 = 0 -> "Fizz"
        |_ -> v.ToString()

    let ``Fizz Buzzer returns 1 when given a one`` =
        Test(fun env ->
                1 |> FizzBuzzer |> expectsToBe "1" "FizzBuzzer did not return '%s' instead it returned '%s'"
            )

    let ``Fizz Buzzer returns Fizz when given 3`` =
        Test(fun env -> 
                3 |> FizzBuzzer |> expectsToBe "Fizz" "FizzBuzzer did not return '%s' instead it returned '%s'"
            )

    let ``Fizz Buzzer returns Fizz for multiples of 3`` =
        Test(fun env ->
                let numbers = seq {  for i in 1 .. 100 do 
                                     if i % 3 = 0
                                     then yield i}

                let fizzCount = numbers |> Seq.map FizzBuzzer |> Seq.filter (fun fb -> fb = "Fizz")

                verify
                    {
                        let! correctNumbers = numbers |> Seq.length |> expectsToBe 33 "incorrect number of numbers. Expected %d got %d"
                        let! correctFizzes = fizzCount |> Seq.length |> expectsToBe 33 "incorrectly converted numbers to Fizz. Got %d Fizzes but expected %d"
                        return Success
                    }
            )

    let ``Fizz Bu