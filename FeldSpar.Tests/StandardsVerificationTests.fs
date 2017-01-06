namespace FeldSpar.Console.Tests
open FeldSpar.Framework
open FeldSpar.Framework.Verification
open ApprovalTests.Reporters

module ``When checking against standards FeldSpar should`` =
    type Color = | White | Brown | Black | TooCool
    type TestingType =
        {
            Name : string;
            Age:int;
            Dojo:string*Color
        }
    
    let ``be usable from within a verify block`` = 
        Test((fun env ->
                let itemUnderTest = 
                    sprintf "%A%s"
                        ({
                            Name = "Steven";
                            Age = 38;
                            Dojo = ("Too Cool For School", TooCool)
                        }) "\n"

                verify
                    {
                        let! standardsAreGood = itemUnderTest |> checkAgainstStringStandard env
                        return Success
                    }
            ))

    let ``check against a query rather then results`` = 
        Test(fun env ->
            let getQuery (x: int) = x.ToString ()
            let executeQuery (s: string) = s

            let query = { QueryResult = 1234; GetQuery = getQuery; ExecuteQuery = executeQuery }

            checkQueryResultAgainstStandard env query
        )

    let ``test a query and show results of old and new on failure`` = 
        Test(fun env ->
            let queryResult = "Hello World"
            let getQuery (x: string) = x
            let executeQuery (s: string) = s.ToUpperInvariant ()

            queryResult
                |> getQueryWith getQuery
                |> executeQueryWith executeQuery
                |> checkQueryResultAgainstStandard env
        )
