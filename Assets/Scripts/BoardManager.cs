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
    public TileBase brightTile, brightTileHighlighted, darkTile, darkTileHighlighted;
    public GameObject whitePieces, blackPieces;  // parent objects of all chess piece objects
    public List<GameObject> whitePiecesList, blackPiecesList;  // lists with all white/black pieces on the board
    public GameObject whiteKing, blackKing;
    public Dictionary<Vector2, GameObject> occupiedTiles = new Dictionary<Vector2, GameObject>();  // (key, value) = (Vector2 tile, GameObject chess piece at tile)
    public TMP_Text textPieceUnmoveable, textWrongPlayer, textInCheck, textOwnKingInCheck, textStillOwnKingInCheck;
    public TMP_Text textWhiteWins, textBlackWins, textTie;
    public GameObject pawnPromotionMenu;
    public int i = 1;

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
        Debug.Log($"BoardManager.Update()-Call {i}");
        foreach (var el in occupiedTiles)
        {
            Debug.Log($"BoardManager Update {i} DICT: {el.Key}  {el.Value}  pos={el.Value.transform.position}      {Time.realtimeSinceStartup}");
        }
        i += 1;


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


            foreach (var el in occupiedTiles)
            {
                Debug.Log($"INSIDE BoardManager Update {i - 1} DICT: {el.Key}  {el.Value}  pos={el.Value.transform.position}      {Time.realtimeSinceStartup}");
            }


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

            foreach (var el in occupiedTiles)
            {
                Debug.Log($"INSIDE-2 BoardManager Update {i - 1} DICT: {el.Key}  {el.Value}  pos={el.Value.transform.position}      {Time.realtimeSinceStartup}");
            }


            Debug.Log("before tie-check");

            // check if game is tied
            if (ending == "stillRunning" && !PlayerCanMove())
            {
                ending = "tie";
            }

            Debug.Log("after tie-check");


            foreach (var el in occupiedTiles)
            {
                Debug.Log($"INSIDE-3 BoardManager Update {i - 1} DICT: {el.Key}  {el.Value}  pos={el.Value.transform.position}      {Time.realtimeSinceStartup}");
            }


            Debug.Log("set readyForNextMove=true");
            readyForNextMove = true;
        }

        // check if game is over
        if (ending != "stillRunning")
        {
            DrawEndingScreen(ending);
        }



        Debug.Log($"BoardManager.Update()-Call-ENDE {i - 1} folgt nach dict-Daten");
        foreach (var el in occupiedTiles)
        {
            Debug.Log($"BoardManager Update {i-1} DICT: {el.Key}  {el.Value}  pos={el.Value.transform.position}      {Time.realtimeSinceStartup}");
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
        Debug.Log("PlayerCanMove()-Call  " + Time.realtimeSinceStartup);
        List<GameObject> alliedPieces;
        if (activePlayer == "white")
        {
            alliedPieces = whitePiecesList;
        } else
        {
            alliedPieces = blackPiecesList;
        }
        Debug.Log($"alliedPieces.count = {alliedPieces.Count}    {Time.realtimeSinceStartup}");

        List<Vector2> possibleTargetTiles;

        Debug.Log("AFTER possibleTargetTiles");

        foreach (GameObject piece in alliedPieces)
        {
            /*Debug.Log($"PCM() piece: {piece} tag={piece.tag}");
            foreach (var el in occupiedTiles)
            {
                Debug.Log($"PCM pair: {el.Key}  {el.Value}  pos={el.Value.transform.position}      {Time.realtimeSinceStartup}");
            }*/

            if (piece == null || !piece.activeSelf)
            {
                Debug.Log("PCM() null/destroyed object found.");
                // "piece" was already hitted and deactivated/destroyed -> cannot move anymore
                continue;
            }
            // check if "piece" can do any move
            possibleTargetTiles = piece.GetComponent<PieceController>().GetLegalToMoveTiles();
            foreach (Vector2 tile in possibleTargetTiles)
            {
                Debug.Log("PCM() possibleTargetTile: " + tile);
                Vector3 targetPos = new Vector3(tile.x, tile.y, piece.transform.position.z);
                Debug.Log("PCM() before TryMove()");
                if (piece.GetComponent<PieceController>().TryMove(targetPos, true))
                {
                    Debug.Log("PlayerCanMove()-End TRUE " + Time.realtimeSinceStartup);
                    return true;  // executeable move found
                }
            }
            // "piece" cannot move -> check next allied piece
        }
        // no allied piece can move
        Debug.Log("PlayerCanMove()-End FALSE");
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
