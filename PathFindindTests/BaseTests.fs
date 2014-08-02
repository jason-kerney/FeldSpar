namespace PathFinding.Tests
open FeldSpar.Framework

module BaseTests = 
    type internal Marker = interface end
    let testPathFindingAssembly = typeof<Marker>.Assembly

    let ``Simple Pathfinding`` = 
        Test(fun _ ->
                // Given a 3 x 4 rectangulare graph
                //  a stating point of 0, 1
                //  a ending point of 3, 1
                //  and no obstructions
                // then the path should be
                //  0,1
                //  1,1
                //  2,1
                //  3,1
                ignoreWith "not done"
            )