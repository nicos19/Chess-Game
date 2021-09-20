using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

public class BoardManager : MonoBehaviour
{
    public float tileSize = 1f;  // height/width of a single tile
    public string boardSize = "classic";  // "classic" represents an 8x8 chess board
    public string activePlayer = "white";  // which player ("white" or "black") moves next
    public string ending;  // "stillRunning" or "whiteWins" or "blackWins" or "tie"
    public bool readyForNextMove;
    public bool activeSelection;  // is currently any piece selected
    public bool inCheck;  // is currently a king in chess
    public bool activeMenu;  // is currently any menu open
    public List<GameObject> checkSetter;  // pieces that threaten enemy's king causing chess
    public Tilemap map;
    public TileBase brightTile, brightTileSelected, brightTileCheck, brightTileLastTurn, brightTileLegalToMove,
        darkTile, darkTileSelected, darkTileCheck, darkTileLastTurn, darkTileLegalToMove;
    public GameObject whitePieces, blackPieces;  // parent objects of all chess piece objects
    public List<GameObject> whitePiecesList, blackPiecesList;  // lists with all white/black pieces on the board
    public GameObject whiteKing, blackKing;
    public Dictionary<Vector2, GameObject> occupiedTiles = new Dictionary<Vector2, GameObject>();  // (key, value) = (Vector2 tile, GameObject chess piece at tile)
    public TMP_Text textPieceUnmoveable, textWrongPlayer, textInCheck, textOwnKingInCheck, textStillOwnKingInCheck;
    public TMP_Text textWhiteWins, textBlackWins, textTie;
    public GameObject pawnPromotionMenu;

    // Start is called before the first frame update
    void Start()
    {
        ending = "stillRunning";
        readyForNextMove = true;
        activeSelection = false;
        inCheck = false;
        activeMenu = false;
        checkSetter = new List<GameObject>();

        foreach (Transform child in whitePieces.transform)
        {
            whitePiecesList.Add(child.gameObject);
        }
        foreach (Transform child in blackPieces.transform)
        {
            blackPiecesList.Add(child.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!readyForNextMove && !activeMenu)
        {
            GameObject activeKing;
            Vector2 kingTile;
            if (activePlayer == "white")
            {
                activeKing = whiteKing;
            } else
            {
                activeKing = blackKing;
            }
            kingTile = activeKing.GetComponent<PieceController>().GetTileForPosition(activeKing.transform.position);
            // check if activePlayer's king is in check/checkmate
            checkSetter = KingInCheckBy(kingTile, activePlayer);  // enemy pieces (of activePlayer) that set check
            if (checkSetter.Count != 0)
            {
                // remember that king is in check
                inCheck = true;
                // check if activePlayer's king is in checkmate
                if (!PlayerCanMove())
                {
                    // checkmate
                    ending = EnemyOfActivePlayer() + "Wins";
                }
                else
                {
                    // only check, no checkmate
                    StartCoroutine(activeKing.GetComponent<PieceController>().ActivateAndDeactivate(textInCheck.gameObject, 3));
                }
            }
            // check if game is tied
            if (ending == "stillRunning" && !PlayerCanMove())
            {
                ending = "tie";
            }
            readyForNextMove = true;
        }
        // check if game is over
        if (ending != "stillRunning")
        {
            DrawEndingScreen(ending);
        }
    }

    public void ChangeActivePlayer(string oldPlayer)  
        // change activePlayer from "oldPlayer" to either "white" or "black" 
    {
        if (oldPlayer == "white")
        {
            activePlayer = "black";
        } else
        {
            activePlayer = "white";
        }
    }

    public List<GameObject> KingInCheckBy(Vector2 kingTile, string attackedPlayer)
    // returns a list of all check setter against king of "attackedPlayer" in current state
    // returns empty list if king of "attackedPlayer" is not in check
    {
        List<GameObject> chessSetter = new List<GameObject>();
        List<GameObject> enemyPiecesOfAttackedPlayer;
        if (attackedPlayer == "white")
        {
            enemyPiecesOfAttackedPlayer = blackPiecesList;
        }
        else
        {
            enemyPiecesOfAttackedPlayer = whitePiecesList;
        }

        foreach (GameObject piece in enemyPiecesOfAttackedPlayer)
        {
            if (piece == null || !piece.activeSelf)
            {
                // piece was already hitted and destroyed/deactivated -> cannot cause check
                continue;
            }

            List<Vector2> threatendTiles = piece.GetComponent<PieceController>().GetLegalToMoveTiles();
            foreach (Vector2 tile in threatendTiles)
            {
                if (tile == kingTile)
                {
                    chessSetter.Add(piece);
                }
            }
        }
        return chessSetter;
    }

    private bool PlayerCanMove()
    // true if currently active player can do some legal move next
    {
        List<GameObject> alliedPieces;
        if (activePlayer == "white")
        {
            alliedPieces = whitePiecesList;
        } else
        {
            alliedPieces = blackPiecesList;
        }

        List<Vector2> possibleTargetTiles;
        foreach (GameObject piece in alliedPieces)
        {
            if (piece == null || !piece.activeSelf)
            {
                // "piece" was already hitted and deactivated/destroyed -> cannot move anymore
                continue;
            }
            // check if "piece" can do any move
            possibleTargetTiles = piece.GetComponent<PieceController>().GetLegalToMoveTiles();
            foreach (Vector2 tile in possibleTargetTiles)
            {
                Vector3 targetPos = new Vector3(tile.x, tile.y, piece.transform.position.z);
                if (piece.GetComponent<PieceController>().TryMove(targetPos, true))
                {
                    return true;  // executeable move found
                }
            }
            // "piece" cannot move -> check next allied piece
        }
        // no allied piece can move
        return false;
    }

    private string EnemyOfActivePlayer()
    {
        if (activePlayer == "white")
        {
            return "black";
        } else
        {
            return "white";
        }
    }

    public void DrawEndingScreen(string ending)
        // Game is over. Draw the ending screen.
    {
        if (ending == "whiteWins")
        {
            textWhiteWins.gameObject.SetActive(true);
        }
        else if (ending == "blackWins")
        {
            textBlackWins.gameObject.SetActive(true);
        }
        else if (ending == "tie")
        {
            textTie.gameObject.SetActive(true);
        } else
        {
            throw new System.ArgumentException("ending='" + ending + "'" + " should not cause end of the game");
        }
    }

}
