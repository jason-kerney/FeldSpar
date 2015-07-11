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
## Writing Tests
[The Documentation](https://github.com/jason-kerney/FeldSpar/wiki)

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



