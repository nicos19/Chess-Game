using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class BoardManager : MonoBehaviour
{
    public int tileSize = 1;  // height/width of a single tile
    public string boardSize = "classic";  // "classic" represents an 8x8 chess board
    public string activePlayer = "white";  // which player ("white" or "black") moves next
    public string ending;  // "stillRunning" or "whiteWins" or "blackWins" or "tie"
    public bool readyForNextMove;
    public bool activeSelection;  // is currently any piece selected
    public List<Vector2Int> activeSelectionLegalTiles;  // "legalToMove" tiles of currently active selection
    public bool inCheck;  // is currently a king in check
    public bool activeIngameMessage;  // is currently some ingame message active
    public GameObject activeText = null;  // the ingame message that is currently active
    public IEnumerator activeCoroutine = null;  // represents currently active ingame message
    public IEnumerator newCoroutine = null;  // represents ingame message that should be displayed next
    public List<GameObject> checkSetter;  // pieces that threaten enemy's king causing chess
    public List<Vector2Int> lastMove;
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
    public Dictionary<Vector2Int, GameObject> occupiedTiles = new Dictionary<Vector2Int, GameObject>();  // (key, value) = (Vector2 tile, GameObject chess piece at tile)
    public TMP_Text textPieceUnmoveable, textWrongPlayer, textInCheck, textNoCheck, textOwnKingInCheck, textStillOwnKingInCheck, textEnemyNotReachable, textOpponentsTurn;
    public TMP_Text textWhiteWins, textBlackWins, textTie;
    public GameObject whitesTurn, blacksTurn, yourTurnWhite, yourTurnBlack, opponentsTurnWhite, opponentsTurnBlack;
    public GameObject spaceForLastMove;
    public GameObject pawnPromotionMenu;
    public GameObject ingameMessages;
    public IngameMessagesManager ingameMessagesManager;
    public GameObject onlineMultiplayerManagerObject;
    public OnlineMultiplayerManager onlineMultiplayerManager;
    public GameObject loadingScreen;

    private TileBase[] brightTileVariants;
    private TileBase[] darkTileVariants;
    private bool savegameLoaded;  // whether a savegame has been loaded or not yet
    private int loadSavegameCalledAtFrame;  // frame when "LoadSavegame" was called

    // Start is called before the first frame update
    void Start()
    {
        ending = "stillRunning";
        readyForNextMove = true;
        activeSelection = false;
        activeSelectionLegalTiles = new List<Vector2Int>();
        inCheck = false;
        activeIngameMessage = false;
        checkSetter = new List<GameObject>();
        lastMove = new List<Vector2Int>();
        lastMoveActive = false;
        ingameMessagesManager = ingameMessages.GetComponent<IngameMessagesManager>();
        onlineMultiplayerManager = onlineMultiplayerManagerObject.GetComponent<OnlineMultiplayerManager>();

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
        
        if (MenuManager.Instance.loadGame)
        {
            loadingScreen.SetActive(true);
        }

        savegameLoaded = false;
    }

    // Update is called once per frame
    void Update()
    {
        // load savegame if player clicked Load Game button in main menu
        if (MenuManager.Instance.loadGame)
        {
            if (OnlineMultiplayerActive.Instance.isOnline && !onlineMultiplayerManagerObject.activeSelf)
            {
                return;  // online game, wait until connection has been set properly)
            }
            LoadSavegame();
            loadingScreen.SetActive(false);
        } else
        {
            // for online, newly started game only
            if (onlineMultiplayerManagerObject.activeSelf && !onlineMultiplayerManager.isHost && !onlineMultiplayerManager.newGameReady && !savegameLoaded && !onlineMultiplayerManager.isLoadedGame)
            {
                onlineMultiplayerManager.CmdSetNewGameReady();  // tell host that the new game is ready for playing
            }
            // for online game only
            if (onlineMultiplayerManagerObject.activeSelf && onlineMultiplayerManager.isLoadedGame && !savegameLoaded)
            {
                // host loaded savegame but player2 tries to start a new game -> disconnect (only player2)
                ButtonController.Disconnect();
                return;
            }
        }

        // is there a new ingame message to be displayed?
        if (!Object.ReferenceEquals(newCoroutine, null))  
        {
            if (activeIngameMessage)
            {
                // new ingame message should replace currently active message
                StopCoroutine(activeCoroutine);
                activeText.SetActive(false);  // remove active message
                activeIngameMessage = false;
                activeCoroutine = null;
                ingameMessagesManager.DisplayIngameMessage(newCoroutine);  // display new message
            }
            else
            {
                // currently no ingame message displayed
                ingameMessagesManager.DisplayIngameMessage(newCoroutine);  // display new message
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

        if (!readyForNextMove && !MenuManager.Instance.gamePaused)
        {
            textNoCheck.gameObject.SetActive(true);

            GameObject activeKing;
            Vector2Int kingTile;
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
                textNoCheck.gameObject.SetActive(false);
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
                    StartCoroutine(AudioManager.Instance.PlayEndingSoundEffect("win", 0));
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
                StartCoroutine(AudioManager.Instance.PlayEndingSoundEffect("tie", 0));
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

        // online multiplayer
        if (OnlineMultiplayerActive.Instance.isOnline)
        {
            if ((onlineMultiplayerManager.player == "white" && onlineMultiplayerManager.blackMoved) || 
                (onlineMultiplayerManager.player == "black" && onlineMultiplayerManager.whiteMoved))
            {
                if (MenuManager.Instance.loadGame || (savegameLoaded && (Time.frameCount - loadSavegameCalledAtFrame) < 10))
                {
                    // online game is a loaded game and savegame must still be loaded
                    // remark: "|| (savegameLoaded && ...)" is there to ensure that enough time elapsed to execute all instantiate/destroy calls in "LoadSavegame"
                    return;  // -> wait until savegame is loaded
                }

                if (onlineMultiplayerManager.pawnPromotion && onlineMultiplayerManager.pawnPromotionResult == "")
                {
                    // opponent made pawn promotion move, but has not chosen promotion yet
                    return;  // -> wait until opponent chose in the promotion menu
                }

                onlineMultiplayerManager.whiteMoved = false;
                onlineMultiplayerManager.blackMoved = false;

                // opponent moved -> execute move locally
                GameObject opponentPiece = GameObject.Find(onlineMultiplayerManager.lastMovedPiece);
                Vector3 opponentTargetPos = onlineMultiplayerManager.lastMoveTargetPos;
                opponentPiece.GetComponent<PieceController>().TryMove(opponentTargetPos);

                if (onlineMultiplayerManager.pawnPromotion)
                {
                    // execute actual pawn promotion based on choice of opponent
                    pawnPromotionMenu.GetComponent<ClickToPromotePawn>().PawnToPieceByTag(onlineMultiplayerManager.pawnPromotionResult);
                    onlineMultiplayerManager.CmdResetPawnPromotion();
                }
            }
        }
    }

    public void ChangeActivePlayer(string oldPlayer)  
        // change activePlayer from "oldPlayer" to either "white" or "black" 
    {
        if (oldPlayer == "white")
        {
            activePlayer = "black";
            blacksTurn.SetActive(true);
            whitesTurn.SetActive(false);
        } else
        {
            activePlayer = "white";
            whitesTurn.SetActive(true);
            blacksTurn.SetActive(false);
        }
    }

    public void SetWhitesTurnBlacksTurn()
        // for online games: set "whitesTurn" and "blacksTurn" correctly
    {
        if (onlineMultiplayerManager.player == "white")
        {
            if (whitesTurn.activeSelf)
            {
                whitesTurn.SetActive(false);
                yourTurnWhite.SetActive(true);
            } else
            {
                blacksTurn.SetActive(false);
                opponentsTurnBlack.SetActive(true);
            }

            whitesTurn = yourTurnWhite;
            blacksTurn = opponentsTurnBlack;
        } else
        {
            if (whitesTurn.activeSelf)
            {
                whitesTurn.SetActive(false);
                opponentsTurnWhite.SetActive(true);
            } else
            {
                blacksTurn.SetActive(false);
                yourTurnBlack.SetActive(true);
            }

            whitesTurn = opponentsTurnWhite;
            blacksTurn = yourTurnBlack;
        }
    }

    public List<GameObject> KingInCheckBy(Vector2Int kingTile, string attackedPlayer)
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
            if (piece == null || !piece.activeSelf || !piece.GetComponent<PieceController>().activeOnJustTry)
            {
                // piece was already hitted and destroyed/deactivated -> cannot cause check
                continue;
            }

            List<Vector2Int> threatendTiles = piece.GetComponent<PieceController>().GetLegalToMoveTiles();
            foreach (Vector2Int tile in threatendTiles)
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

        List<Vector2Int> possibleTargetTiles;
        foreach (GameObject piece in alliedPieces)
        {
            if (piece == null || !piece.activeSelf)
            {
                // "piece" was already hitted and deactivated/destroyed -> cannot move anymore
                continue;
            }
            // check if "piece" can do any move
            possibleTargetTiles = piece.GetComponent<PieceController>().GetLegalToMoveTiles();
            foreach (Vector2Int tile in possibleTargetTiles)
            {
                Vector3 targetPos = new Vector3(tile.x + (float)tileSize / 2, tile.y + (float)tileSize / 2, piece.transform.position.z);

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
        spaceForLastMove.SetActive(false);

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

    private void RotateCamera180()
        // Rotate Camera 180? and adjust its position so the screen shows the board correctly, also rotate all chess pieces
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
            // rotate from original position 180?
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

    public void ChangeTile(Vector2Int tilePos, TileBase[] newTile)
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

    public void SetCorrectTile(Vector2Int tilePos)
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

    private bool CheckSetterAt(Vector2Int tile)
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

    private bool WasLastMoveOrigin(Vector2Int tile)
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

    private bool WasLastMoveTarget(Vector2Int tile)
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

    public void DocumentMove(List<Vector2Int> move, string filePath)
        // document the last made move in the file represented by "filePath"
    {
        string path = filePath;
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "");  // create new empty file
        }

        string newLine;
        if (move.Count == 2)
        {
            newLine = $"{activePlayer}\t{move[0]}->{move[1]}\t\t{TilePositionAsString(move[0])}->{TilePositionAsString(move[1])}\n";
        } else  // (move.Count == 4)
        {
            newLine = $"{activePlayer}\t{move[0]}->{move[1]}, {move[2]}->{move[3]}" +
                $"\t\t{TilePositionAsString(move[0])}->{TilePositionAsString(move[1])}, {TilePositionAsString(move[2])}->{TilePositionAsString(move[3])}\n";
        }
        File.AppendAllText(path, newLine);  // add last move
    }

    private string TilePositionAsString(Vector2Int tile)
        // returns string representation of a tile's position, e.g. (0, 0) => A1
    {
        string tileX = tile.x switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            3 => "D",
            4 => "E",
            5 => "F",
            6 => "G",
            7 => "H",
            _ => "UnknownX-Position",
        };
        string tileY = (tile.y + 1).ToString();
        return tileX + tileY;
    }

    private Savegame CreateSavegame()
        // create a savegame instance that contains all relevant information about current game state
    {
        Savegame savegame = new Savegame();

        // save board information
        savegame.activePlayer = activePlayer;
        foreach (Vector2Int tile in lastMove)
        {
            savegame.lastMoveX.Add(tile.x);
            savegame.lastMoveY.Add(tile.y);
        }

        // save information about chess pieces
        foreach (GameObject piece in whitePiecesList)
        {
            if (piece == null || !piece.activeSelf)
            {
                // piece already hitted and destroyed/deactivated
                continue;
            }

            PieceSavegame pieceSavegame = new PieceSavegame();
            pieceSavegame.pieceName = piece.name;
            pieceSavegame.player = "white";
            pieceSavegame.pieceTag = piece.tag;
            pieceSavegame.positionX = piece.transform.position.x;
            pieceSavegame.positionY = piece.transform.position.y;
            pieceSavegame.positionZ = piece.transform.position.z;
            pieceSavegame.atStart = piece.GetComponent<PieceController>().atStart;
            savegame.allPieces.Add(pieceSavegame);
        }

        foreach (GameObject piece in blackPiecesList)
        {
            if (piece == null || !piece.activeSelf)
            {
                // piece already hitted and destroyed/deactivated
                continue;
            }

            PieceSavegame pieceSavegame = new PieceSavegame();
            pieceSavegame.pieceName = piece.name;
            pieceSavegame.player = "black";
            pieceSavegame.pieceTag = piece.tag;
            pieceSavegame.positionX = piece.transform.position.x;
            pieceSavegame.positionY = piece.transform.position.y;
            pieceSavegame.positionZ = piece.transform.position.z;
            pieceSavegame.atStart = piece.GetComponent<PieceController>().atStart;
            savegame.allPieces.Add(pieceSavegame);
        }

        return savegame;
    }

    public void CreateSavegameFile()
        // create a savegame on disk that can be deserialized when savegame is loaded
    {
        Savegame savegame = CreateSavegame();

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/savegame.save");
        bf.Serialize(file, savegame);
        file.Close();
    }

    public void LoadSavegame()
        // use savegame (if available) to load a previously saved game
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(Application.persistentDataPath + "/savegame.save", FileMode.Open);
        Savegame savegame = (Savegame)bf.Deserialize(file);
        file.Close();

        // for online game
        if (OnlineMultiplayerActive.Instance.isOnline)
        {
            if (onlineMultiplayerManager.isHost)
            {
                onlineMultiplayerManager.CmdSetIsLoadedGame();  // remember that a savegame was loaded
                // host tells player2 which savegame is used by host
                onlineMultiplayerManager.savegameOfHost = savegame.ToString();
            }
            else
            {
                if (!onlineMultiplayerManager.isLoadedGame)
                {
                    // disconnect since host started a new game but player2 tries to load a savegame
                    ButtonController.Disconnect();
                    return;
                }

                // player2 tells host which savegame is used by player2 -> both check for savegame synchronization
                onlineMultiplayerManager.CmdTellServerPlayer2Savegame(savegame.ToString());
                onlineMultiplayerManager.CmdCheckSavegameSynchronization();
            }
        }

        // clear chess board
        occupiedTiles.Clear();
        foreach (GameObject piece in whitePiecesList)
        {
            Destroy(piece);
        }
        foreach (GameObject piece in blackPiecesList)
        {
            Destroy(piece);
        }
        whitePiecesList = new List<GameObject>();
        blackPiecesList = new List<GameObject>();

        // restore state of chess board based on savegame
        activePlayer = savegame.activePlayer;
        if (activePlayer == "white")
        {
            whitesTurn.SetActive(true);
            blacksTurn.SetActive(false);
        } else
        {
            whitesTurn.SetActive(false);
            blacksTurn.SetActive(true);
        }

        // restore lastMove
        List<Vector2Int> restoredLastMove = new List<Vector2Int>();
        for (int i = 0; i < savegame.lastMoveX.Count; i++)
        {
            restoredLastMove.Add(new Vector2Int(savegame.lastMoveX[i], savegame.lastMoveY[i]));
        }
        lastMove = restoredLastMove;
        
        // restore chess pieces
        foreach (PieceSavegame pieceSavegame in savegame.allPieces)
        {
            gameObject.GetComponent<PieceCreator>().CreatePieceFromSavegame(pieceSavegame);
        }

        readyForNextMove = false;
        MenuManager.Instance.loadGame = false;
        savegameLoaded = true;
        loadSavegameCalledAtFrame = Time.frameCount;
    }

}
