using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Savegame : MonoBehaviour
{
    public string activePlayer;
    public string ending;
    public bool inCheck;
    public List<Vector2Int> lastMove;
    public List<PieceSavegame> allPieces;
}
