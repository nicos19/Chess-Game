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
}
