namespace FeldSpar.Framework

[<AutoOpen>]
module FrameworkTools =
    type SupportedFrameworks =
        | Net40
        | Net45
        | Net451
        | Net452
        | Net46

    let currentFramework = 
        let getVersion t v =
            if t () = None then
                fun () -> Some(v)
            else
                raise (new System.ApplicationException("Too many .Net Frameworks"))

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

        match test () with
        | None -> raise (new System.ApplicationException("Invalid .Net Framework"))
        | Some(r) -> r