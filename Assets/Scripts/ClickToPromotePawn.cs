using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickToPromotePawn : MonoBehaviour
{
    public GameObject pawnToPromote;
    public GameObject prefabQueen;
    public GameObject prefabRook;
    public GameObject prefabKnight;
    public GameObject prefabBishop;
    public GameObject whitePieces;
    public GameObject blackPieces;
    public GameObject board;

    public void PawnToQueen()
    {
        PawnToX(prefabQueen);
    }

    public void PawnToRook()
    {
        PawnToX(prefabRook);
    }

    public void PawnToKnight()
    {
        PawnToX(prefabKnight);
    }

    public void PawnToBishop()
    {
        PawnToX(prefabBishop);
    }

    public void PawnToX(GameObject prefabPiece)
        // promote pawn that reached board end to new game object using "prefabPiece"
    {
        Vector3 position = pawnToPromote.transform.position;
        string player = pawnToPromote.GetComponent<PieceController>().player;
        Transform parent;
        if (player == "white")
        {
            parent = whitePieces.transform;
        } else
        {
            parent = blackPieces.transform;
        }

        // create new piece using "prefabPiece", destroy pawn
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
        board.GetComponent<BoardManager>().activeMenu = false;
    }

    public void InitializePiece(GameObject newPiece, string player)
    // initialize a new piece on the board (promoted pawn)
    {
        newPiece.GetComponent<PieceController>().board = board;
        newPiece.GetComponent<PieceController>().player = player;

        // update "occupiedTiles" -> remove pawn
        board.GetComponent<BoardManager>().occupiedTiles.Remove(
            PieceController.GetTileForPosition(newPiece.transform.position));
    }

}
