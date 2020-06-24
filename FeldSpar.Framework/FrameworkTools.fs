namespace FeldSpar.Framework

[<AutoOpen>]
module FrameworkTools =
    type SupportedFrameworks =
        | Net40
        | Net45
        | Net451
        | Net452
        | Net46
        | Net461

    let currentFramework = 
        let getVersion t v =
            if t () = None then
                fun () -> Some(v)
            else
                raise (new System.ApplicationException("Too many .Net Frameworks"))

        let test : unit -> SupportedFrameworks option = fun () -> None

        let test = getVersion test Net461

        match test () with
        | None -> raise (new System.ApplicationException("Invalid .Net Framework"))
        | Some(r) -> r