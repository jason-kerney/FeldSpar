(*
namespace FeldSpar.Examples
open FeldSpar.Framework
open FeldSpar.Framework.Verification


module ``Here are examples of how to write basic Tests`` =
    // The primary way to call verify a result is the expectsToBe function. 
    // Which has the signature of 'a->'a->TestResult
    let ``Adding 6 and 4 equals 10`` = 
        Test(fun _ ->
                let x = 6
                let y = 4

                (x + y) |> expectsToBe 10
        )

    // You can use withFailComment to add additional comment to the result. 
    // It has the signature String -> TestResult -> TestResult
    let ``Adding 6 and 4 equals 10 with fail comment`` = 
        Test(fun _ ->
                let x = 6
                let y = 4

                (x + y) |> expectsToBe 10 |> withFailComment "6 + 4 did not equal 10"
             )

    // You can do multiple checks by using a verify block. 
    // The verify stops execution at the first failure.
    let ``A test with multiple checks to determine a good result`` =
        Test(fun _ ->
                let x = 6
                let y = 4
                let z = x + y

                verify
                    {
                        let! goodX = x |> expectsToBe 6 |> withFailComment "x was wrong"
                        let! goodY = y |> expectsToBe 4 |> withFailComment "y was wrong"
                        let! goodZ = z |> expectsToBe 10 |> withFailComment "z was wrong"
                        return Success
                    }
            )
    // You can quickly ignore a test by using ITest instead of Test
    // ***************************************************************************************
    // *  An ignored test is a FAILED test. As such this causes a failure with type of Ignored
    // ***************************************************************************************
    let ``This test is not ready yet and therefore is ignored`` =
        ITest(fun env -> Success)

module ``Here are examlpes on how to write gold standard tests`` = 
    // What is Gold Master Testing
    //
    // Gold Master Testing is a process where the expected result is an artifact that a human understands and acknowledges as correct.
    // This artifact is the "Gold Standard" or "Gold Master". It usually lives in a separate file from the unit tests.
    //
    // Benefits
    //
    // Humans are great at pattern recognition. This type of testing takes advantage of that.
    // Increases understandably of a failure by showing a full picture of the differences.
    // Enforces Single Responsibility of test code, since expected result and test code are in different files.
    //
    // Reporters
    //
    // Reporters are types that are used to display why a test has failed to the human. Reporters allow for the system to capitalize 
    // on the ability of Humans to recognize patterns.
    //
    // BE AWARE If using the command line runner then you will have to use --ur for reporters to be used.
    // Configuring Reporters
    //
    // Before you can see your results you will need a bit of config code. 
    // This code can be located any-where in your project. The findFirstReporter function finds the first reporter checks to see 
    // if a reporter of the specified type will run on the current system. If so it returns that reporter. If not it checks the next. 
    // If all fail, it then returns a reporter which echoes the Gold Standard's file name to the Console. All reporters listed in the 
    // return value Reporters are used.

    type Color = | White | Brown | Black | TooCool
    type TestingType =
        {
            Name : string;
            Age:int;
            Dojo:string * Color
        }

    let ``A test to check verification`` = 
        Test(fun env ->
                let itemUnderTest = 
                    sprintf "%A%s"
                        ({
                            Name = "Steven";
                            Age = 38;
                            Dojo = ("Too Cool For School", TooCool)
                        }) "\n"

                itemUnderTest |> checkAgainstStringStandard env
            )

    let ``This is a Combinatory Gold Standard Testing`` =
        Test(fun env ->
            let names = ["Tom"; "Jane"; "Tarzan"; "Stephanie"]
            let amounts = [11; 2; 5;]
            let items = ["pears";"earrings";"cups"]

            let createSentance item amount name = sprintf "%s has %d %s" name amount item

            createSentance
                |> calledWithEachOfThese items
                |> andAlsoEachOfThese amounts
                |> andAlsoEachOfThese names
                |> checkAllAgainstStandard env
        )

module ``Examples on how to write a theory test`` =
    // What is a theory test?
    // 
    // A theory test is a test where you can verify that a large number of inputs will always produce a known or determinable output.
    // 
    // The way that theory tests are implemented in FeldSpar you can also use them to create parameterized tests.
    // 
    // Note
    // 
    // All theory tests run each input as a different concrete test.

    // A theory test has 2 parts. The first is Data which is 'a seq and represents the values being passed to the test. 
    // The second part is called a Base. The base represents the test factory used to create a single test for each datum in Data

    let ``This is a theory Test`` =
        Theory({
                    Data = [
                                (1, "1");
                                (2, "2");
                                (3, "Fizz");
                                (5, "Buzz");
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

                                result |> expectsToBe expected
                            )
                    }
        })

    let ``Division Theory`` = 
        {
            UnitDescription = (fun n -> sprintf " (%f * %f) / %f = %f" n n n n)
            UnitTest = (fun n _ ->
                            let v1 = n ** 2.0
                            let result = v1 / n

                            result |> expectsToBe n |> withFailComment "(%f <> %f)"
            )
        }

    let ``Whole Doubles from 1.0 to 20.0`` = seq { 1.0..20.0 }  

    let ``Here is a second theory test`` =
        Theory({
                Data = ``Whole Doubles from 1.0 to 20.0``
                Base = ``Division Theory``
        })

        // *)