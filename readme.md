**Feld Spar F#**
=========
> An opinionated test framework designed to be functional from the ground up.

Goals
-----------

-----------------

### 1. Be as purely functional as possible
### 2. Enforce Test Isolation

* _(done)_ Tests run in isolated memory space
* _(maybe)_ Test execution order is indeterminate

### 3. Enable Gold Standard Testing as a Framework Feature
* _(in progress)_ Enable use of Approval Libraries in a functional manner
* Allow use of Approval Use Reporter Attribute or some equivalent

### 4. Implement Theory Based Testing

### 5. Integrate with visual studio

### 6. Provide out of editor and console runner

### 3. Generate Documentation
* Add XML Comments
* _(done)_ Add Read Me

##Example
### How to write a unit test

```fsharp
module BasicTests =
    let ``Adding 6 and 4 equals 10`` = 
        Test((fun env ->
                let x = 6
                let y = 4

                (x + y) expectsToBe 10 "Addition failed 6 + 4 <> %d but did equal %d"
            ))
              
    let ``A test with multiple checks to deterime a good result`` =
        Test((fun env ->
                let x = 6
                let y = 4
                let z = x + y
                
                verify
                    {
                        let! goodX = x expectsToBe 6 "x failed expected %d but got %d"
                        let! goodY = y expectsToBe 4 "y failed expected %d but got %d"
                        let! goodZ = z expectsToBe 10 "(x + y) failed expected %d but got %d"
                        return Success
                    }
            ))
```

### How to _(currently)_ run all tests

```fsharp
module Program =
    type internal Marker = interface end
    let private assembly = typeof<Marker>.Assembly

    [<EntryPoint>]
    let public main argv = 
        let results = assembly |> runTests
        
        let failedTests = results
                            |> reduceToFailures 

        printfn "Displaying Results (%d Failed of %d)" (failedTests |> Seq.length) (results |> Seq.length)
```
## Design Considerations
### **NO** Exception Driven Workflows
> OO based test frameworks use `Assert` to designate a failure. This works because it generates an exception which forces an early exit without `if` `then` `else` or `case` statements.

> Functional programming has a better way. In F# that way is called a workflow. Every test **must** return a valid type indicating its success or failure. In FeldSpar that type is a `TestResult`.

> Exceptions happen. The Framework will handle them, however they should be an exception to the normal rule.

### Inored Tests are failing tests
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