namespace FeldSpar.Console.Tests
open FeldSpar.Framework
open FeldSpar.Framework.Verification

module DotNetFrameworkTests =
    let testFrameworkVersion = 
        let getVersion t v =
            if t () = None then
                fun () -> Some(v)
            else
                t

        let test : unit -> SupportedFrameworks option = fun () -> None
    #if NET40
        let test = getVersion test Net40
    #endif
    #if NET45
        let test = getVersion test Net45
    #endif
    #if NET451
        let test = getVersion test Net451
    #endif
    #if NET452
        let test = getVersion test Net452
    #endif
    #if NET46
        let test = getVersion test Net46
    #endif
    #if NET461
        let test = getVersion test Net461
    #endif

        test ()

    let ``Can Detect the correct version`` =
        Test(fun _ ->
                Some(currentFramework) |> expectsToBe testFrameworkVersion
        )

    let ``Environment has correct version`` =
        Test(fun env ->
            Some(env.FeldSparNetFramework) |> expectsToBe testFrameworkVersion
        )

