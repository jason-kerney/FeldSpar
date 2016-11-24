namespace FeldSpar.Framework

module TestSummaryUtilities =
    open TestResultUtilities
    open TryParser
    open FeldSpar.Framework.Sorting
    open System
    
    let joinWith separator (strings : string seq) =
        System.String.Join(separator, strings)

    let reduceTo filter summaries =
        summaries |> Seq.filter (fun summary -> filter summary.TestResults)

    let reduceToFailures summaries =
        summaries |> reduceTo filterFailures

    let reduceToSuccess summaries = 
        summaries |> reduceTo filterSuccesses

    let JSONFormat (value:OutputReport) =
        let spacer = "\n\t"

        let groups = value.Reports |> Seq.groupBy (fun r -> r.TestContainerName)
        let buildGroups group report = sprintf "\t{\n\t\t\t\t\"Collection\": %A,\n\t\t\t\t\"Tests\": [\n%s\n\t\t\t\t]\n\t\t\t}" group report

        let buildReport items = 
            items
                |> Seq.map 
                    (fun (g, passing) -> 
                        buildGroups g passing
                    )
                |> joinWith ",\n"

        let successes = 
            groups
            |> Seq.map
                (fun (group, results) -> 
                    let successes = results |> Seq.map (fun r -> r.Successes)
                    (group, successes |> Seq.concat |> Seq.map (fun s -> sprintf "\t\t\t\t\t\t\"%s\"" (s.Replace("\"", "'"))) |> joinWith ",\n")
                )
        
        let successString = buildReport successes

        let failures = 
            groups
            |> Seq.map 
                (fun (group,results) ->
                    let failures = results |> Seq.map (fun r -> r.Failures ) |> Seq.concat
                                
                    let result =
                        failures
                        |> Seq.map 
                            (
                                fun { TestName = name; FailureType = failureType } ->
                                    let failMsg = 
                                        let rec getFailMessage failType message =
                                            match failType with
                                            | GeneralFailure(msg)     -> sprintf "%sGeneral Failure ('%s')" message msg
                                            | ExceptionFailure(ex)    -> sprintf "%sException Thrown:\n%A" message ex
                                            | ExpectationFailure(msg) -> sprintf "%sExpectation Not Met ('%s')" message msg
                                            | Ignored(msg)            -> sprintf "%sIgnored ('%s')" message msg
                                            | StandardNotMet(path)    -> sprintf "%sStandard was not Met at %A" message path
                                            | SetupFailure(failure)   -> getFailMessage failure "Before test failed with "

                                        getFailMessage failureType ""

                                    let failMsg = failMsg.Replace("\"", "'")

                                    sprintf "\t\t\t\t\t\t{ \"%s\" : \"%s\" }" name failMsg
                            )
                        |> joinWith ",\n"
                    (group, result)
                )

        let failString = buildReport failures

        let jsonString = sprintf "{%s\"Assembly Name\" : \"%s\" %s\"Failures\" : [\n\t\t%s\n\t],%s\"Successes\" : [\n\t\t%s\n\t]\n}\n" spacer (value.AssemblyName) spacer failString spacer successString
        jsonString

