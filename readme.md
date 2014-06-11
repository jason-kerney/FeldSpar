**Feld Spar**
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

### 4. Integrate with visual studio

### 5. Provide out of editor and console runner

### 6. Generate Documentation
* Add XML Comments
* _(done)_ Add Read Me

##Example
### How to write a unit test

```fsharp
module BasicTests =
    let testAdditionOfTwoNumbers = 
        Test({
                Description = "A test of the addition operator"
                UnitTest = (fun env ->
                                let x = 6
                                let y = 4

                                (x + y) expectsToBe 10 "Addition failed 6 + 4 <> %d but did equal %d"
                            )
              })
```

### How to _(currently)_ run all tests

```fharp
module Program =
    type internal Marker = interface end
    let private assembly = typeof<Marker>.Assembly

    [<EntryPoint>]
    let public main argv = 
        let results = assembly |> runTests
        
        let failedTests = results
                            |> reduceToFailures 
                            |> List.ofSeq

        printfn "Displaying Results (%d Failed of %d)" (failedTests |> List.length) (results |> List.length)
```

### Feld Spar?
  Vikings navigated using solar navigation. This presented a problem when it was foggy, overcast or rainy. However they were very successful at navigation despite these limitations. Myth states that the Vikings had a magic [Sun Stone](http://news.discovery.com/earth/rocks-fossils/viking-sunstone-shipwreck-130311.htm) that enabled them to navigate during the worst of weather.
  
  Recent discoveries have shown that the Viking Sun Stone was not a myth. It was a type of stone known as Icelantic Spar which is in tern a type of Feld Spar.
  
  Unit tests guide us out of the worst situations. And so I named my framework after the tool that guided the Vikings out of the worst weather.