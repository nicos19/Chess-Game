using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Savegame
{
    public string activePlayer;
    public List<int> lastMoveX = new List<int>();
    public List<int> lastMoveY = new List<int>();
    public List<PieceSavegame> allPieces = new List<PieceSavegame>();

    public override string ToString()
        // create string representation of savegame object
    {
        string savegameString = $"-{activePlayer}-";
        savegameString += lastMoveToString();
        savegameString += allPiecesToString();
        return savegameString;
    }

    public string lastMoveToString()
    {
        string lastMoveString = "";

        foreach (int el in lastMoveX)
        {
            lastMoveString += $"{el}-";
        }

        foreach (int el in lastMoveY)
        {
            lastMoveString += $"{el}-";
        }

        return lastMoveString;
    }

    public string allPiecesToString()
    {
        string allPiecesString = "";

        foreach (PieceSavegame pieceSavegame in allPieces)
        {
            allPiecesString += pieceSavegame.ToString() + "-";
        }

        return allPiecesString;
    }

}
