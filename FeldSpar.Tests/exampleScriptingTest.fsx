#r @"./packages/ApprovalUtilities/lib/net45/ApprovalUtilities.dll"
#r @"./packages/ApprovalTests/lib/net40/ApprovalTests.dll"
#r @"./packages/FeldSparFramework/lib/Net461/FeldSparFramework.dll"

open FeldSpar.Framework
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ApprovalsSupport
open FeldSpar.Framework.ScriptSupport
open FeldSpar.Framework.ConsoleRunner
open ApprovalTests

let fizzBuzz x = 
    match x with
    | value when value % 15 = 0 -> "FizzBuzz"
    | value when value % 3 = 0 -> "Fizz"
    | value when value % 5 = 0 -> "Buzz"
    | value -> value.ToString ()

let testsStructure = 
    [        
        "FizzBuzz returns \"1\" when given 1"
            |> testedWith
                (fun _ -> 
                    1 
                    |> fizzBuzz 
                    |> expectsToBe "1"
                )
        "FizzBuzz returns \"2\" when given 2"
            |> testedWith 
                (fun _ -> 
                    2 
                    |> fizzBuzz 
                    |> expectsToBe "2"
                )
        "fizzBuzz returns \"Fizz\" when given 3" 
            |> testedWith 
                (fun _ -> 
                    3 
                    |> fizzBuzz 
                    |> expectsToBe "Fizz" 
                    |> withFailComment "3 did not convert correctly"
                )
        "fizzBuzz returns \"Fizz\" when given 6" 
            |> testedWith 
                (fun _ -> 
                    6 
                    |> fizzBuzz 
                    |> expectsToBe "Fizz" 
                    |> withFailComment "6 did not convert correctly"
                )
        "fizzBuzz returns \"Buzz\" when given 5"
            |> testedWith 
                (fun _ -> 
                    5 
                    |> fizzBuzz 
                    |> expectsToBe "Buzz" 
                    |> withFailComment "5 did not convert correctly"
                )
        "fizzBuzz returns \"Buzz\" when given 10"
            |> testedWith
                (fun _ ->
                    10
                    |> fizzBuzz
                    |> expectsToBe "Buzz"
                    |> withFailComment "10 did not convert correctly"
                )
        "fizzBuzz returns \"FizzBuzz\" when given 15"
            |> testedWith
                (fun _ ->
                    15
                    |> fizzBuzz
                    |> expectsToBe "FizzBuzz"
                    |> withFailComment "15 did not convert correctly"
                )
        "fizzBuzz returns \"FizzBuzz\" when given 30"
            |> testedWith
                (fun _ ->
                    30
                    |> fizzBuzz
                    |> expectsToBe "FizzBuzz"
                    |> withFailComment "15 did not convert correctly"
                )
    ]

testsStructure 
    |> runTestsAndReport defaultConfig