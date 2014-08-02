namespace PathFinding

type GridSpaceType =
    | Normal of GridSpace
    | Start of GridSpace
    | End of GridSpace

and GridSpace = 
    {
        Up : GridSpaceType Option;
        Right : GridSpaceType Option;
        Down : GridSpaceType Option;
        Left : GridSpaceType Option;
    }

type Grid =
    {
        Starts : GridSpaceType List;
    }

module Framework =
    let BuildGrid diminsion startingPoints endingPoints = ()

