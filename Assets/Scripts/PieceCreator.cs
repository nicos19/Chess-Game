using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceCreator : MonoBehaviour
{
    public GameObject board;
    public GameObject whitePieces;
    public GameObject blackPieces;

    public GameObject prefabKingWhite;
    public GameObject prefabQueenWhite;
    public GameObject prefabRookWhite;
    public GameObject prefabKnightWhite;
    public GameObject prefabBishopWhite;
    public GameObject prefabPawnWhite;

    public GameObject prefabKingBlack;
    public GameObject prefabQueenBlack;
    public GameObject prefabRookBlack;
    public GameObject prefabKnightBlack;
    public GameObject prefabBishopBlack;
    public GameObject prefabPawnBlack;

    public void CreatePieceFromSavegame(PieceSavegame pieceSavegame)
        // create a chess piece gameobject based on "pieceSavegame"
    {
        Vector3 position = pieceSavegame.position;
        string player = pieceSavegame.player;
        Transform parent;

        if (player == "white")
        {
            parent = whitePieces.transform;
        }
        else
        {
            parent = blackPieces.transform;
        }

        // create new piece
        GameObject prefabPiece = GetProperPrefab(pieceSavegame.player, pieceSavegame.pieceTag);
        GameObject newPiece = Instantiate(prefabPiece, position, Quaternion.identity, parent);
        InitializePiece(newPiece, player, pieceSavegame.pieceName, pieceSavegame.atStart);

        // update "whitePiecesList" / "blackPiecesList" in BoardManager component of "board"
        if (player == "white")
        {
            board.GetComponent<BoardManager>().whitePiecesList.Add(newPiece);
        }
        else
        {
            board.GetComponent<BoardManager>().blackPiecesList.Add(newPiece);
        }
    }

    public void InitializePiece(GameObject newPiece, string player, string name, bool atStart)
    // initialize a new piece on the board
    {
        newPiece.GetComponent<PieceController>().board = board;
        newPiece.GetComponent<PieceController>().player = player;
        newPiece.GetComponent<PieceController>().atStart = atStart;
        newPiece.GetComponent<PieceMovement>().board = board;
        newPiece.name = name;

        newPiece.GetComponent<PieceController>().Start();
        newPiece.GetComponent<PieceMovement>().Start();

        // tell BoardManager the king pieces
        if (newPiece.tag == "King")
        {
            if (player == "white")
            {
                board.GetComponent<BoardManager>().whiteKing = newPiece;
            } else
            {
                board.GetComponent<BoardManager>().blackKing = newPiece;
            }
        }
    }

    public GameObject GetProperPrefab(string player, string pieceTag)
        // return the proper prefab based on "player" and "pieceTag"
    {
        string prefabCode = player + " " + pieceTag;

        GameObject prefab = prefabCode switch
        {
            "white King" => prefabKingWhite,
            "white Queen" => prefabQueenWhite,
            "white Rook" => prefabRookWhite,
            "white Knight" => prefabKnightWhite,
            "white Bishop" => prefabBishopWhite,
            "white Pawn" => prefabPawnWhite,

            "black King" => prefabKingBlack,
            "black Queen" => prefabQueenBlack,
            "black Rook" => prefabRookBlack,
            "black Knight" => prefabKnightBlack,
            "black Bishop" => prefabBishopBlack,
            "black Pawn" => prefabPawnBlack,
            _ => throw new System.NotImplementedException("Unknown prefabCode"),
        };

        return prefab;
    }

}
