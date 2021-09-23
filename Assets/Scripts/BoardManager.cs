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
    public bool activeIngameMessage;  // is currently some ingame message active
    public GameObject activeText = null;  // the ingame message that is currently active
    public IEnumerator activeCoroutine = null;  // represents currently active ingame message
    public IEnumerator newCoroutine = null;  // represents ingame message that should be displayed next
    public List<GameObject> checkSetter;  // pieces that threaten enemy's king causing chess
    public Tilemap map;
    public TileBase brightTile, brightTileSelected, brightTileCheck, brightTileLastTurn, brightTileLegalToMove,
        darkTile, darkTileSelected, darkTileCheck, darkTileLastTurn, darkTileLegalToMove;
    public GameObject whitePieces, blackPieces;  // parent objects of all chess piece objects
    public List<GameObject> whitePiecesList, blackPiecesList;  // lists with all white/black pieces on the board
    public GameObject whiteKing, blackKing;
    public Dictionary<Vector2, GameObject> occupiedTiles = new Dictionary<Vector2, GameObject>();  // (key, value) = (Vector2 tile, GameObject chess piece at tile)
    public TMP_Text textPieceUnmoveable, textWrongPlayer, textInCheck, textOwnKingInCheck, textStillOwnKingInCheck, textEnemyNotReachable;
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
        activeIngameMessage = false;
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
        if (!Object.ReferenceEquals(newCoroutine, null))  // is there a new ingame message to be displayed?
        {
            if (activeIngameMessage)
            {
                // new ingame message should replace currently active message
                StopCoroutine(activeCoroutine);
                activeText.SetActive(false);  // remove active message
                activeIngameMessage = false;
                activeCoroutine = null;
                DisplayIngameMessage(newCoroutine);  // display new message
            } else
            {
                // currently no ingame message displayed
                DisplayIngameMessage(newCoroutine);  // display new message
            }
        }

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
                    StartNewIngameMessage(textInCheck.gameObject, 3);
                }
            }
            // check if game is tied
            if (ending == "stillRunning" && !PlayerCanMove())
            {
                ending = "tie";
            }
            readyForNextMove = true;

            // deactivate ingame messages before next players turn (except: in-check and ending-messages)
            if (activeIngameMessage && activeText != textInCheck.gameObject 
                && activeText != textWhiteWins.gameObject && activeText != textBlackWins.gameObject && activeText != textTie.gameObject)
            {
                StopCoroutine(activeCoroutine);
                activeText.SetActive(false);
                activeIngameMessage = false;
                activeCoroutine = null;
            }

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
        List<GameObject> checkSetterLocal = new List<GameObject>();
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
                    checkSetterLocal.Add(piece);
                }
            }
        }
        return checkSetterLocal;
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

    public void DisplayIngameMessage(IEnumerator newMessage)
    {
        activeIngameMessage = true;
        activeCoroutine = newMessage;
        newCoroutine = null;

        StartCoroutine(newMessage);
    }

    public void StartNewIngameMessage(GameObject text, float waitTime)
        // tell BoardManager that a new ingame message ("text") shall be displayed (and removed from screen after "waitTime" seconds)
    {
        newCoroutine = ActivateAndDeactivateMessage(text, waitTime);
    }

    public IEnumerator ActivateAndDeactivateMessage(GameObject obj, float waitTime)
    // this functions activates an object (a displayed text) and deactivates it after a delay of "waitTime" seconds
    {
        obj.SetActive(true);
        activeText = obj;
        
        yield return new WaitForSecondsRealtime(waitTime);
        
        obj.SetActive(false);
        activeIngameMessage = false;
        activeCoroutine = null;
        activeText = null;
    }

    private void RotateCamera180()
        // Rotate Camera 180� and adjust its position so the screen shows the board correctly, also rotate all chess pieces
    {
        Camera.main.transform.Rotate(0, 0, 180);
        Camera.main.transform.position = new Vector3(6.2f, 3.5f, -10);

        foreach (GameObject piece in whitePiecesList)
        {
            piece.transform.Rotate(0, 0, 180);
        }
        foreach (GameObject piece in blackPiecesList)
        {
            piece.transform.Rotate(0, 0, 180);
        }
    }

    private void RotateCameraOriginal()
        // Rotate Camera back in its original state, also rotate all chess pieces back
    {
        Camera.main.transform.Rotate(0, 0, -180);
        Camera.main.transform.position = new Vector3(1.8f, 4.5f, -10);

        foreach (GameObject piece in whitePiecesList)
        {
            piece.transform.Rotate(0, 0, -180);
        }
        foreach (GameObject piece in blackPiecesList)
        {
            piece.transform.Rotate(0, 0, -180);
        }
    }

}
