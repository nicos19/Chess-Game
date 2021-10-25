using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PieceSavegame
{
    public string pieceName;
    public string player;
    public string pieceTag;
    public float positionX;
    public float positionY;
    public float positionZ;
    public bool atStart;

    public override string ToString()
    {
        return $"{pieceName}-{player}-{pieceTag}-{positionX}-{positionY}-{positionZ}-{atStart}";
    }
}
