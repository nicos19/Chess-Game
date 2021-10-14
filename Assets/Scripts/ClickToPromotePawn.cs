using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickToPromotePawn : MonoBehaviour
{
    public GameObject board;
    public GameObject pawnToPromote;
    public GameObject whitePieces;
    public GameObject blackPieces;

    public GameObject prefabQueenWhite;
    public GameObject prefabRookWhite;
    public GameObject prefabKnightWhite;
    public GameObject prefabBishopWhite;
    public GameObject prefabQueenBlack;
    public GameObject prefabRookBlack;
    public GameObject prefabKnightBlack;
    public GameObject prefabBishopBlack;

    public void PawnToQueen()
    {
        if (pawnToPromote.GetComponent<PieceController>().player == "white")
        {
            PawnToX(prefabQueenWhite, "white");
        } else  // player = "black"
        {
            PawnToX(prefabQueenBlack, "black");
        }
    }

    public void PawnToRook()
    {
        if (pawnToPromote.GetComponent<PieceController>().player == "white")
        {
            PawnToX(prefabRookWhite, "white");
        }
        else  // player = "black"
        {
            PawnToX(prefabRookBlack, "black");
        }
    }

    public void PawnToKnight()
    {
        if (pawnToPromote.GetComponent<PieceController>().player == "white")
        {
            PawnToX(prefabKnightWhite, "white");
        }
        else  // player = "black"
        {
            PawnToX(prefabKnightBlack, "black");
        }
    }

    public void PawnToBishop()
    {
        if (pawnToPromote.GetComponent<PieceController>().player == "white")
        {
            PawnToX(prefabBishopWhite, "white");
        }
        else  // player = "black"
        {
            PawnToX(prefabBishopBlack, "black");
        }
    }

    public void PawnToX(GameObject prefabPiece, string player)
        // promote pawn that reached board end to new game object using "prefabPiece"
    {
        Vector3 position = pawnToPromote.transform.position;
        Transform parent;
        if (player == "white")
        {
            parent = whitePieces.transform;
        } else
        {
            parent = blackPieces.transform;
        }

        // update "occupiedTiles" -> remove pawn
        board.GetComponent<BoardManager>().occupiedTiles.Remove(PieceController.GetTileForPosition(position));

        // create new piece using "prefabPiece"
        GameObject newPiece = Instantiate(prefabPiece, position, Quaternion.identity, parent);
        InitializePiece(newPiece, player);

        // update "whitePiecesList" / "blackPiecesList" in BoardManager component of "board"
        if (player == "white")
        {
            board.GetComponent<BoardManager>().whitePiecesList.Remove(pawnToPromote);
            board.GetComponent<BoardManager>().whitePiecesList.Add(newPiece);
        }
        else
        {
            board.GetComponent<BoardManager>().blackPiecesList.Remove(pawnToPromote);
            board.GetComponent<BoardManager>().blackPiecesList.Add(newPiece);
        }

        // destroy pawn
        Destroy(pawnToPromote);

        // close promotion menu
        board.GetComponent<BoardManager>().pawnPromotionMenu.SetActive(false);
        board.GetComponent<BoardManager>().pawnPromotionMenu.GetComponent<ClickToPromotePawn>().pawnToPromote = null;
        MenuManager.Instance.gamePaused = false;

        AudioManager.Instance.PlayButtonSoundEffect();
    }

    public void InitializePiece(GameObject newPiece, string player)
    // initialize a new piece on the board (promoted pawn)
    {
        newPiece.GetComponent<PieceController>().board = board;
        newPiece.GetComponent<PieceController>().player = player;
        newPiece.GetComponent<PieceMovement>().board = board;

        newPiece.GetComponent<PieceController>().Start();
        newPiece.GetComponent<PieceMovement>().Start();
    }

}
