**Feld Spar F#**
=========
> An opinionated test framework designed to be functional from the ground up.

###[![Build status](https://ci.appveyor.com/api/projects/status/b5xo1bn8nxr7h06q?svg=true)](https://ci.appveyor.com/project/jason-kerney/feldspar)

## Available on Nuget

### [![NuGet Status](http://img.shields.io/nuget/v/FeldSparFramework.svg?style=flat)](https://www.nuget.org/packages/FeldSparFramework/) -- Framework
### [![NuGet Status](http://img.shields.io/nuget/v/FeldSpar.Console.svg?style=flat)](https://www.nuget.org/packages/FeldSpar.Console/) -- Console Runner
### [![NuGet Status](http://img.shields.io/nuget/v/FeldSparGui.svg?style=flat)](https://www.nuget.org/packages/FeldSparGui/) -- GUI Runner
### [![NuGet Status](http://img.shields.io/nuget/v/FeldSpar.GuiApi.Engine.svg?style=flat)](https://www.nuget.org/packages/FeldSpar.GuiApi.Engine/) -- API for creating a GUI Runner for FeldSpar

-----------------
## What's Different

1. Function Paradigm from the start
2. Test Memory Isolation
3. Random Test Execution
4. Not a xUnit Clone

----------------- 
##Example
### How to write a unit test

```fsharp
module BasicTests =
open FeldSpar.Framework
open FeldSpar.Framework.Verification
open FeldSpar.Framework.Verification.ApprovalSupport

    let ``Adding 6 and 4 equals 10`` = 
        Test((fun _ ->
                let x = 6
                let y = 4

               // Before version 0.5
               // (x + y) expectsToBe 10 "Addition failed 6 + 4 <> %d but did equal %d"

               // After version 0.5
               (x + y) expectsToBe 10
            ))
              
    let ``A test with multiple checks to deterime a good result`` =
        Test((fun _ ->
                let x = 6
                let y = 4
                let z = x + y
                
                verify
                    {
                        // Before version 0.5
                        // let! goodX = x expectsToBe 6 "x failed expected %d but got %d"
                        // let! goodY = y expectsToBe 4 "y failed expected %d but got %d"
                        // let! goodZ = z expectsToBe 10 "(x + y) failed expected %d but got %d"

                        // After version 0.5
                        let! goodX = x expectsToBe 6 |> withFailComment "x was wrong"
                        let! goodY = y expectsToBe 4 |> withFailComment "y was wrong"
                        let! goodZ = z expectsToBe 10 |> withFailComment "z was wrong"
                        return Success
                    }
            ))
            
    (*This is how you quickly ignore a test*)
    let ``This test is not ready yet and therefore is ignored`` =
        ITest(fun env -> Success)
        
    let ``Gold Standard Tests look like this for strings`` =
        Test(fun env ->
                let env = env |> addReporter<ApprovalTests.Reporters.DiffReporter>
                
                "My string under test" |> checkAgainstStringStandard env
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

                            result |> expectsToBe n "(%f <> %f)"
            )
        }
          
    let ``Whole Doubles from 1.0 to 20.0`` = seq { 1.0..20.0 }  

    let ``Here is a second theory test`` =
        Theory({
                Data = ``Whole Doubles from 1.0 to 20.0``
                Base = ``Division Theory``
        })
```

### How to _(currently)_ run all tests

* Run "FeldSparGui.exe" 
* Click "Add Test Suite" 
* Navigate to compiled tests, and open file 
* Click "Run"

### OR:
* Run FeldSpar.Console.exe with these args

#### Console args
```console

--test-assembly [--a] <string>: This is the location of the test library. It can be a *.dll or a *.exe file

--report-location [--r] <string>: This flag indicates that a JSON report is to be generated at the given location

--verbosity [--v] <string>: This sets the verbosity level for the run. Possible levels are: ["Max"; "Results"; "Errors"; "Detail"]

--auto-loop [--al]: This makes the command contiuously run executing on every compile.

--usereporters [--ur]: This enables the use of reporters configured in the test

--pause [--p]: This makes the console wait for key press inorder to exit. This is automaticly in effect if "auto-loop" is used

--debug: This launches the debugger to allow you to debug the tests

--help [-h|/h|/help|/?]: display this list of options.

```

##Goals
### 1. Be as purely functional as possible
### 2. _(done)_ Enforce Test Isolation

* _(done)_ Tests run in isolated memory space
* _(done)_ Test execution order is indeterminate

### 3. _(done)_ Enable Gold Standard Testing as a Framework Feature
* _(done)_ Enable use of Approval Libraries in a functional manner
* _(done)_ Enable Configuration to setup global reporters for test Assembly.

### 4. _(done)_ Implement Theory Based Testing
* _(done)_ Theory test type

### 5. Integrate with visual studio

### 6. _(done)_ Create Console Runner
* _(done)_ Create parameterized console
* _(done)_ Allow Console to auto detect changes and rerun all tests

### 7. _(done)_ Provide out of editor gui runner
* _(done)_ Build CLR integration layer for the Engine
* _(done)_ Create a WPF viewer in C#
* _(done)_ Move View Models to the FeldSpar main project and convert them to F#
* _(done)_ Create WPF GUI runner in F# 

### 3. Generate Documentation
* Add XML Comments
* _(done)_ Add Read Me

## Design Considerations
### **NO** Exception Driven Workflows
> OO based test frameworks use `Assert` to designate a failure. This works because it generates an exception which forces an early exit without `if` `then` `else` or `case` statements.

> Functional programming has a better way. In F# that way is called a workflow. Every test **must** return a valid type indicating its success or failure. In FeldSpar that type is a `TestResult`.

> Exceptions happen. The Framework will handle them, however they should be an exception to the normal rule.

### Ignored Tests are failing tests
> In other frameworks an ingnored test simply does not run and reports itself as being in a third state if _ignored_. Ignoring a test is a failure. It is a failure of either the test or test methodologies.

> By having an ignored state you increase complexity of the system because there are three states of a test.

> Feld Spar tackles this by having only 2 states of a test. Success or Failure. Failures allow you to have a reason for failure, which will be `Ignored` for an ignored test.

### Favor Intention
> .Net attributes are not immediately obvious when your program execution depends on them. If you find a method that conforms to the test signature but lacks the attribute was the attribute removed?

> I wanted a test framework that made a test method as obviously indented to be a test.

> I also choose a convention based approach whenever I was able to without forfeiting clarity

## Feld Spar?
> Vikings navigated using solar navigation. This presented a problem when it was foggy, overcast or rainy. However they were very successful at navigation despite these limitations. Myth states that the Vikings had a magic [Sun Stone](http://news.discovery.com/earth/rocks-fossils/viking-sunstone-shipwreck-130311.htm) that enabled them to navigate during the worst of weather.
  
> Recent discoveries have shown that the Viking Sun Stone was not a myth. It was a type of stone known as Icelantic Spar which is in tern a type of Feld Spar.
  
> Unit tests guide us out of the worst situations. And so I named my framework after the tool that guided the Vikings out of the worst weather.



