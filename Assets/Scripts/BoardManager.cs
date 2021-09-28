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
    public List<Vector2> activeSelectionLegalTiles;  // "legalToMove" tiles of currently active selection
    public bool inCheck;  // is currently a king in check
    public bool activeMenu;  // is currently any menu open
    public bool activeIngameMessage;  // is currently some ingame message active
    public GameObject activeText = null;  // the ingame message that is currently active
    public IEnumerator activeCoroutine = null;  // represents currently active ingame message
    public IEnumerator newCoroutine = null;  // represents ingame message that should be displayed next
    public List<GameObject> checkSetter;  // pieces that threaten enemy's king causing chess
    public List<Vector2> lastMove;
    public bool lastMoveActive;  // is the last move currently highlighted or not
    public Tilemap map;
    public TileBase brightTile, brightTileSelected, brightTileLegalToMove, brightTileCheckSetter, brightTileInCheck,
        brightTileLastMoveOrigin, brightTileLastMoveTarget, brightTileLastMoveTargetCheckSetter,
        brightTileLegalToMoveLastMoveOrigin, brightTileLegalToMoveLastMoveTarget,
        brightTileLegalToMoveCheckSetter, brightTileLegalToMoveLastMoveTargetCheckSetter,

        darkTile, darkTileSelected, darkTileLegalToMove, darkTileCheckSetter, darkTileInCheck,
        darkTileLastMoveOrigin, darkTileLastMoveTarget, darkTileLastMoveTargetCheckSetter,
        darkTileLegalToMoveLastMoveOrigin, darkTileLegalToMoveLastMoveTarget,
        darkTileLegalToMoveCheckSetter, darkTileLegalToMoveLastMoveTargetCheckSetter;
    public GameObject whitePieces, blackPieces;  // parent objects of all chess piece objects
    public List<GameObject> whitePiecesList, blackPiecesList;  // lists with all white/black pieces on the board
    public GameObject whiteKing, blackKing;
    public Dictionary<Vector2, GameObject> occupiedTiles = new Dictionary<Vector2, GameObject>();  // (key, value) = (Vector2 tile, GameObject chess piece at tile)
    public TMP_Text textPieceUnmoveable, textWrongPlayer, textInCheck, textOwnKingInCheck, textStillOwnKingInCheck, textEnemyNotReachable;
    public TMP_Text textWhiteWins, textBlackWins, textTie;
    public GameObject pawnPromotionMenu;
    public GameObject moveSoundEffectObject;
    public GameObject hitSoundEffectObject;
    public GameObject selectSoundEffectObject;
    public GameObject errorSoundEffectObject;
    public GameObject winSoundEffectObject;
    public GameObject tieSoundEffectObject;

    private TileBase[] brightTileVariants;
    private TileBase[] darkTileVariants;

    // Start is called before the first frame update
    void Start()
    {
        ending = "stillRunning";
        readyForNextMove = true;
        activeSelection = false;
        activeSelectionLegalTiles = new List<Vector2>();
        inCheck = false;
        activeMenu = false;
        activeIngameMessage = false;
        checkSetter = new List<GameObject>();
        lastMove = new List<Vector2>();
        lastMoveActive = false;

        foreach (Transform child in whitePieces.transform)
        {
            whitePiecesList.Add(child.gameObject);
        }
        foreach (Transform child in blackPieces.transform)
        {
            blackPiecesList.Add(child.gameObject);
        }

        brightTileVariants = new TileBase[] { brightTile, brightTileSelected, brightTileLegalToMove, brightTileCheckSetter, brightTileInCheck, brightTileLastMoveOrigin, brightTileLastMoveTarget, brightTileLastMoveTargetCheckSetter, brightTileLegalToMoveLastMoveOrigin, brightTileLegalToMoveLastMoveTarget, brightTileLegalToMoveCheckSetter, brightTileLegalToMoveLastMoveTargetCheckSetter };

        darkTileVariants = new TileBase[] { darkTile, darkTileSelected, darkTileLegalToMove, darkTileCheckSetter, darkTileInCheck, darkTileLastMoveOrigin, darkTileLastMoveTarget, darkTileLastMoveTargetCheckSetter, darkTileLegalToMoveLastMoveOrigin, darkTileLegalToMoveLastMoveTarget, darkTileLegalToMoveCheckSetter, darkTileLegalToMoveLastMoveTargetCheckSetter };
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
            }
            else
            {
                // currently no ingame message displayed
                DisplayIngameMessage(newCoroutine);  // display new message
            }
        }

        
        // when "SPACE" held pressed: highlight last move
        if (Input.GetButtonDown("Show Last Move"))
        {
            lastMoveActive = true;
            ShowOrHideLastMove(true);
        }
        // if "SPACE" is released: dehighlight last move
        if (Input.GetButtonUp("Show Last Move"))
        {
            lastMoveActive = false;
            ShowOrHideLastMove(false);
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
            kingTile = PieceController.GetTileForPosition(activeKing.transform.position);
            // check if activePlayer's king is in check/checkmate
            checkSetter = KingInCheckBy(kingTile, activePlayer);  // enemy pieces (of activePlayer) that set check
            if (checkSetter.Count != 0)
            {
                // remember that king is in check
                inCheck = true;
                // highlight king in check and all check setter
                SetCorrectTile(kingTile);
                foreach (GameObject piece in checkSetter)
                {
                    SetCorrectTile(PieceController.GetTileForPosition(piece.transform.position));
                }
                // check if activePlayer's king is in checkmate
                if (!PlayerCanMove())
                {
                    // checkmate
                    ending = EnemyOfActivePlayer() + "Wins";
                    StartCoroutine(EndingSoundEffect(winSoundEffectObject, 0));
                }
                else
                {
                    // only check, no checkmate
                    textInCheck.gameObject.SetActive(true);
                }
            }
            // check if game is tied
            if (ending == "stillRunning" && !PlayerCanMove())
            {
                ending = "tie";
                StartCoroutine(EndingSoundEffect(tieSoundEffectObject, 0));
            }

            //RotateCamera();

            // deactivate ingame messages before next players turn (except: in-check and ending-messages)
            if (activeIngameMessage && activeText != textInCheck.gameObject 
                && activeText != textWhiteWins.gameObject && activeText != textBlackWins.gameObject && activeText != textTie.gameObject)
            {
                StopCoroutine(activeCoroutine);
                activeText.SetActive(false);
                activeIngameMessage = false;
                activeCoroutine = null;
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
        errorSoundEffectObject.GetComponent<AudioSource>().Play();
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

    public IEnumerator EndingSoundEffect(GameObject soundEffectObject, float delay)
        // after "delay" seconds: play given ending sound effect
    {
        yield return new WaitForSecondsRealtime(delay);
        soundEffectObject.GetComponent<AudioSource>().Play();
    }

    private void RotateCamera180()
        // Rotate Camera 180° and adjust its position so the screen shows the board correctly, also rotate all chess pieces
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

    private void RotateCamera()
    {
        if (Camera.main.transform.position.y == 4.5f)
        {
            // rotate from original position 180°
            RotateCamera180();
        } else if (Camera.main.transform.position.y == 3.5f)
        {
            // rotate back to original position
            RotateCameraOriginal();
        } else
        {
            throw new System.Exception($"Unknown/Wrong Camera position/rotation. Camera.postion.y == {Camera.main.transform.position.y}");
        }
    }

    public void ChangeTile(Vector2 tilePos, TileBase[] newTile)
    // replace tile at "tilePos" with newTile[0] or newTile[1] for a brightTile variant or a darkTile variant respectively
    {
        Vector3Int tilePosInt = new Vector3Int((int)tilePos.x, (int)tilePos.y, 0);  // WARNING: tilePos.z = 0 only if Tilemap's z-value = 0
        if (System.Array.Exists(brightTileVariants, element => element.Equals(map.GetTile(tilePosInt))))
        {
            map.SetTile(tilePosInt, newTile[0]);
        }
        else if (System.Array.Exists(darkTileVariants, element => element.Equals(map.GetTile(tilePosInt))))
        {
            map.SetTile(tilePosInt, newTile[1]);
        }
        else
        {
            throw new System.ArgumentException($"Unknown/Wrong tile at {tilePosInt}! Found tile: {map.GetTile(tilePosInt)}");
        }
    }

    public void SetCorrectTile(Vector2 tilePos)
    // replace tile at "tilePos" with correct tile regarding current state of the chess game
    {
        Vector3Int tilePosInt = new Vector3Int((int)tilePos.x, (int)tilePos.y, 0);  // WARNING: tilePos.z = 0 only if Tilemap's z-value = 0

        if (System.Array.Exists(brightTileVariants, element => element.Equals(map.GetTile(tilePosInt))))  // tile is bright
        {
            if (activeSelectionLegalTiles.Contains(tilePos))  // tile is "legalToMove"
            {
                if (CheckSetterAt(tilePos))  // tile has a checkSetter 
                {
                    if (WasLastMoveTarget(tilePos))  // tile was target of last move
                    {
                        map.SetTile(tilePosInt, brightTileLegalToMoveLastMoveTarget);
                    } else  // tile was not part of last move (since it cannot have been origin of last move)
                    {
                        map.SetTile(tilePosInt, brightTileLegalToMoveCheckSetter);
                    }
                } else  // tile has no checkSetter
                {
                    if (WasLastMoveOrigin(tilePos))  // tile was origin of last move
                    {
                        map.SetTile(tilePosInt, brightTileLegalToMoveLastMoveOrigin);
                    } else if (WasLastMoveTarget(tilePos))  // tile was target of last Move
                    {
                        map.SetTile(tilePosInt, brightTileLegalToMoveLastMoveTarget);
                    } else  // tile was not part of last move
                    {
                        map.SetTile(tilePosInt, brightTileLegalToMove);
                    }
                }
            } else  // tile is not "legalToMove"
            {
                if (CheckSetterAt(tilePos))  // tile has a checkSetter 
                {
                    if (WasLastMoveTarget(tilePos))  // tile was target of last move
                    {
                        map.SetTile(tilePosInt, brightTileLastMoveTarget);
                    } else  // tile was not part of last move (since it cannot have been origin of last move)
                    {
                        map.SetTile(tilePosInt, brightTileCheckSetter);
                    }
                } else  // tile has no checkSetter
                {
                    if (WasLastMoveOrigin(tilePos))  // tile was origin of last move
                    {
                        map.SetTile(tilePosInt, brightTileLastMoveOrigin);
                    } else if (WasLastMoveTarget(tilePos))  // tile was target of last Move
                    {
                        map.SetTile(tilePosInt, brightTileLastMoveTarget);
                    } else if ((activePlayer == "white" && PieceController.GetTileForPosition(whiteKing.transform.position) == tilePos) ||
                        (activePlayer == "black" && PieceController.GetTileForPosition(blackKing.transform.position) == tilePos))
                    {
                        if (inCheck)  // tile has a king inCheck
                        {
                            map.SetTile(tilePosInt, brightTileInCheck);
                        } else
                        {
                            map.SetTile(tilePosInt, brightTile);
                        }
                    }
                    else  
                    {
                        map.SetTile(tilePosInt, brightTile);
                    }
                }
            }
        }
        else if (System.Array.Exists(darkTileVariants, element => element.Equals(map.GetTile(tilePosInt))))  // tile is dark
        {
            if (activeSelectionLegalTiles.Contains(tilePos))  // tile is "legalToMove"
            {
                if (CheckSetterAt(tilePos))  // tile has a checkSetter 
                {
                    if (WasLastMoveTarget(tilePos))  // tile was target of last move
                    {
                        map.SetTile(tilePosInt, darkTileLegalToMoveLastMoveTarget);
                    } else  // tile was not part of last move (since it cannot have been origin of last move)
                    {
                        map.SetTile(tilePosInt, darkTileLegalToMoveCheckSetter);
                    }
                } else  // tile has no checkSetter
                {
                    if (WasLastMoveOrigin(tilePos))  // tile was origin of last move
                    {
                        map.SetTile(tilePosInt, darkTileLegalToMoveLastMoveOrigin);
                    } else if (WasLastMoveTarget(tilePos))  // tile was target of last Move
                    {
                        map.SetTile(tilePosInt, darkTileLegalToMoveLastMoveTarget);
                    } else  // tile was not part of last move
                    {
                        map.SetTile(tilePosInt, darkTileLegalToMove);
                    }
                }
            } else  // tile is not "legalToMove"
            {
                if (CheckSetterAt(tilePos))  // tile has a checkSetter 
                {
                    if (WasLastMoveTarget(tilePos))  // tile was target of last move
                    {
                        map.SetTile(tilePosInt, darkTileLastMoveTarget);
                    } else  // tile was not part of last move (since it cannot have been origin of last move)
                    {
                        map.SetTile(tilePosInt, darkTileCheckSetter);
                    }
                } else  // tile has no checkSetter
                {
                    if (WasLastMoveOrigin(tilePos))  // tile was origin of last move
                    {
                        map.SetTile(tilePosInt, darkTileLastMoveOrigin);
                    }
                    else if (WasLastMoveTarget(tilePos))  // tile was target of last Move
                    {
                        map.SetTile(tilePosInt, darkTileLastMoveTarget);
                    } else if ((activePlayer == "white" && PieceController.GetTileForPosition(whiteKing.transform.position) == tilePos) ||
                      (activePlayer == "black" && PieceController.GetTileForPosition(blackKing.transform.position) == tilePos))
                    {
                        if (inCheck)  // tile has a king inCheck
                        {
                            map.SetTile(tilePosInt, darkTileInCheck);
                        } else
                        {
                            map.SetTile(tilePosInt, darkTile);
                        }
                    } else
                    {
                        map.SetTile(tilePosInt, darkTile);
                    }
                }
            }
        }
        else
        {
            throw new System.ArgumentException($"Unknown/Wrong tile at {tilePosInt}! Found tile: {map.GetTile(tilePosInt)}");
        }
    }

    private void ShowOrHideLastMove(bool show)
        // highlight ("show" = true) or dehighlight ("show" = false) tiles that were part of the last move
    {
        if (!show)
        {
            // dehighlight last move
            if (lastMove.Count == 2)
            {
                SetCorrectTile(lastMove[0]);
                SetCorrectTile(lastMove[1]);
            }
            else if (lastMove.Count == 4)  // last move was a castling move
            {
                SetCorrectTile(lastMove[0]);
                SetCorrectTile(lastMove[1]);
                SetCorrectTile(lastMove[2]);
                SetCorrectTile(lastMove[3]);
            }
        } else
        {
            // highlight last move
            if (lastMove.Count == 2)
            {
                SetCorrectTile(lastMove[0]);
                SetCorrectTile(lastMove[1]);
            }
            else if (lastMove.Count == 4)  // last move was a castling move
            {
                SetCorrectTile(lastMove[0]);
                SetCorrectTile(lastMove[1]);
                SetCorrectTile(lastMove[2]);
                SetCorrectTile(lastMove[3]);
            }
        }
    }

    private bool CheckSetterAt(Vector2 tile)
        // returns true if there is a checkSetter at "tile"
    {
        foreach (GameObject piece in checkSetter)
        {
            if (PieceController.GetTileForPosition(piece.transform.position) == tile)
            {
                return true;
            }
        }
        return false;
    }

    private bool WasLastMoveOrigin(Vector2 tile)
        // returns true if "tile" was the origin of the lastMove made in the game
        // (if lastMoveActive = false, then always return false)
    {
        if (!lastMoveActive)
        {
            return false;
        }

        if (lastMove.Count == 2)
        {
            return lastMove[0] == tile;
        } else if (lastMove.Count == 4)
        {
            return lastMove[0] == tile || lastMove[2] == tile;
        }
        return false;
    }

    private bool WasLastMoveTarget(Vector2 tile)
        // returns true if "tile" was the target of the lastMove made in the game
        // (if lastMoveActive = false, then always return false)
    {
        if (!lastMoveActive)
        {
            return false;
        }

        if (lastMove.Count == 2)
        {
            return lastMove[1] == tile;
        }
        else if (lastMove.Count == 4)
        {
            return lastMove[1] == tile || lastMove[3] == tile;
        }
        return false;
    }

}
