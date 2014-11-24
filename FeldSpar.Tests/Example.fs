namespace FeldSpar.Console.Tests

module Example =
    open FeldSpar.Framework
    open FeldSpar.Framework.Verification
    open FeldSpar.Framework.Verification.ChecksClean
    open FeldSpar.Console.Helpers.Data

    let FizzBuzzer v = 
        match v with
        | fizzBuzz when fizzBuzz % 15 = 0 -> "FizzBuzz"
        | fizz when fizz % 3 = 0 -> "Fizz"
        | buzz when buzz % 5 = 0 -> "Buzz"
        |_ -> v.ToString()

    let ``Fizz Buzzer returns 1 when given a one`` =
        Test(fun env ->
                1 |> FizzBuzzer |> expectsToBe "1"
            )

    let ``Fizz Buzzer returns Fizz when given 3`` =
        Test(fun env -> 
                3 |> FizzBuzzer |> expectsToBe "Fizz"
            )

    let ``Fizz Buzzer returns 'Fizz' for multiples of 3 up to 10`` =
        Test(fun env ->
                let numbers = seq {  for i in 1 .. 10 do 
                                     if i % 3 = 0
                                     then yield i}

                let fizzCount = numbers |> Seq.map FizzBuzzer |> Seq.filter (fun fb -> fb = "Fizz")

                verify
                    {
                        let! correctNumbers = numbers |> Seq.length |> expectsToBe 3 |> addMessage "incorrect number of numbers"
                        let! correctFizzes = fizzCount |> Seq.length |> expectsToBe 3 |> addMessage "incorrectly converted numbers to Fizz."
                        return Success
                    }
            )

    let ``Fizz Buzzer return Buzz for 5`` =
        Test(fun env ->
                let result = 5 |> FizzBuzzer

                result |> expectsToBe "Buzz" |> addMessage "5 should have been turned into 'Buzz'"
            )

    let ``Fizz Buzzer returns 'Buzz' for all multiples of 5 up to 10`` =
        Test(fun env ->
                let numbers = seq { for i in 1 .. 10 do
                                    if i % 5 = 0
                                    then yield i }

                let buzzCount = numbers |> Seq.map FizzBuzzer |> Seq.filter (fun fb -> fb = "Buzz")

                verify
                    {
                        let! correctNumbers = numbers |> Seq.length |> expectsToBe 2 |> addMessage "incorrect number of numbers."
                        let! correctFizzes = buzzCount |> Seq.length |> expectsToBe 2 |> addMessage "incorrectly converted numbers to Buzz."
                        return Success
                    }
            )

    let ``Fizz Buzzer returns 'FizzBuzz' for 15`` =
        Test(fun env -> 
                let result = 15 |> FizzBuzzer

                result |> expectsToBe "FizzBuzz"
            )

    let ``Fizz Buzzer returns 'FizzBuzz' for all numbers that are multiples of 3 and 5 up to 32`` =
        Test(fun env ->
                let numbers = seq { for i in 1 .. 32 do 
                                    if i % 15 = 0
                                    then yield i }

                let fizzBuzzCount = numbers |> Seq.map FizzBuzzer |> Seq.filter (fun fb -> fb = "FizzBuzz")

                verify
                    {
                        let! correctNumbers = numbers |> Seq.length |> expectsToBe 2 |> addMessage "incorrect number of numbers"
                        let! correctFizzBuzzes = fizzBuzzCount |> Seq.length |> expectsToBe 2 |> addMessage "incorrectly converted numbers to FizzBuzz."
                        return Success
                    }
            )

    let ``Fizz Buzzer returns the correct Fizz Buzz or FizzBuzz for every number up to 500`` =
        Test(fun env ->
                let numbers = seq { for i in 1..100 do
                                    yield (i, i |> FizzBuzzer) } 
                              |> List.ofSeq 
                              |> List.map (fun (number, fizzed) -> 
                                            let strNum = number.ToString()
                                            let strNum = strNum.PadLeft(3, '0')

                                            sprintf "%s: %s" strNum fizzed
                                          )
                              |> fun results -> "\n" + System.String.Join("\n", results) + "\n"
                              |> fun s -> s.Trim() + "\n"
                                    
                numbers |> checkAgainstStringStandardCleaned env
            )