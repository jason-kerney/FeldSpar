namespace FeldSpar.Console.Tests

module Example =
    open FeldSpar.Framework
    open FeldSpar.Framework.Verification
    open FeldSpar.Framework.Verification.Checks
    open FeldSpar.Console.Helpers.Data

    let Fail = Test(fun _ -> failwith "not a real error")

    let FizzBuzzer v = 
        match v with
        | fizzBuzz when fizzBuzz % 15 = 0 -> "FizzBuzz"
        | fizz when fizz % 3 = 0 -> "Fizz"
        | buzz when buzz % 5 = 0 -> "Buzz"
        |_ -> v.ToString()

    let ``Fizz Buzzer returns 1 when given a one`` =
        Test(fun env ->
                1 |> FizzBuzzer |> expectsToBe "1" "FizzBuzzer did not return '%s' instead it returned '%s'"
            )

    let ``Fizz Buzzer returns Fizz when given 3`` =
        Test(fun env -> 
                3 |> FizzBuzzer |> expectsToBe "Fizz" "FizzBuzzer did not return '%s' instead it returned '%s'"
            )

    let ``Fizz Buzzer returns 'Fizz' for multiples of 3 up to 10`` =
        Test(fun env ->
                let numbers = seq {  for i in 1 .. 10 do 
                                     if i % 3 = 0
                                     then yield i}

                let fizzCount = numbers |> Seq.map FizzBuzzer |> Seq.filter (fun fb -> fb = "Fizz")

                verify
                    {
                        let! correctNumbers = numbers |> Seq.length |> expectsToBe 3 "incorrect number of numbers. Expected %d got %d"
                        let! correctFizzes = fizzCount |> Seq.length |> expectsToBe 3 "incorrectly converted numbers to Fizz. Got %d Fizzes but expected %d"
                        return Success
                    }
            )

    let ``Fizz Buzzer return Buzz for 5`` =
        Test(fun env ->
                let result = 5 |> FizzBuzzer

                result |> expectsToBe "Buzz" "5 should have been turned into '%s' but instead got '%s'"
            )

    let ``Fizz Buzzer returns 'Buzz' for all multiples of 5 up to 10`` =
        Test(fun env ->
                let numbers = seq { for i in 1 .. 10 do
                                    if i % 5 = 0
                                    then yield i }

                let buzzCount = numbers |> Seq.map FizzBuzzer |> Seq.filter (fun fb -> fb = "Buzz")

                verify
                    {
                        let! correctNumbers = numbers |> Seq.length |> expectsToBe 2 "incorrect number of numbers. Expected %d got %d"
                        let! correctFizzes = buzzCount |> Seq.length |> expectsToBe 2 "incorrectly converted numbers to Buzz. Got %d Fizzes but expected %d"
                        return Success
                    }
            )

    let ``Fizz Buzzer returns 'FizzBuzz' for 15`` =
        Test(fun env -> 
                let result = 15 |> FizzBuzzer

                result |> expectsToBe "FizzBuzz" "15 was expected to be turned into '%s' but instead was turned into '%s'"
            )

    let ``Fizz Buzzer returns 'FizzBuzz' for all numbers that are multiples of 3 and 5 up to 32`` =
        Test(fun env ->
                let numbers = seq { for i in 1 .. 32 do 
                                    if i % 15 = 0
                                    then yield i }

                let fizzBuzzCount = numbers |> Seq.map FizzBuzzer |> Seq.filter (fun fb -> fb = "FizzBuzz")

                verify
                    {
                        let! correctNumbers = numbers |> Seq.length |> expectsToBe 2 "incorrect number of numbers. Expected %d got %d"
                        let! correctFizzBuzzes = fizzBuzzCount |> Seq.length |> expectsToBe 2 "incorrectly converted numbers to FizzBuzz. Got %d FizzBuzzes but expected %d"
                        return Success
                    }
            )//*)

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
                              |> fun results -> System.Environment.NewLine + System.String.Join(System.Environment.NewLine, results) + System.Environment.NewLine
                                    
                numbers |> checkAgainstStandardObjectAsString env
            )