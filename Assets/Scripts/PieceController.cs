using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PieceController : MonoBehaviour
{
    private bool isSelected;  // whether the piece is scelected by the active player
    private Vector3 mousePos;
    private Vector3 mouseWorldPos;
    private Bounds colliderBounds;
    private List<Vector2Int> legalTiles;
    private Vector2Int moveDirection;  
    private GameObject ownKing;
    private Dictionary<Vector2Int, GameObject> castling;  // for king: (key, value) = (castlingTargetTile for king, corresponding rook)
    private Vector2Int tileUnderMousePrev = new Vector2Int(-100, -100);  // the tile under the mouse during the previous frame

    public GameObject board;
    public BoardManager boardManager;
    public Tilemap map;
    public string player;  // the player ("white" or "black") that is associated with the piece
    public bool activeOnJustTry;  // is the piece active during a TryMove(justTry=true) call
    public bool atStart = true;  // whether the piece is still at its starting position (and did not move yet)


    // Start is called before the first frame update
    public void Start()
    {
        isSelected = false;
        castling = new Dictionary<Vector2Int, GameObject>();
        boardManager = board.GetComponent<BoardManager>();
        map = boardManager.map;
        activeOnJustTry = true;

        if (!boardManager.occupiedTiles.ContainsKey(GetTileForPosition(transform.position)))
        {
            // after a piece is created by pawn promotion, Start() was called already during promotion process
            // therefore the key might be added already -> only add key if it is not in the dictionary yet
            boardManager.occupiedTiles.Add(GetTileForPosition(transform.position), gameObject);
        }

        if (player == "white")
        {
            moveDirection = new Vector2Int(0, 1);
            ownKing = boardManager.whiteKing;
        } else if (player == "black")
        {
            moveDirection = new Vector2Int(0, -1);
            ownKing = boardManager.blackKing;
        }
    }

    private void OnMouseDown() // when chess piece is clicked then select or deselect piece
    {
        if (boardManager.ending != "stillRunning" || MenuManager.Instance.gamePaused)
        {
            return;  // game is over or paused 
        }
        if (!boardManager.readyForNextMove)
        {
            return;  // wait for BoardManager.Update()
        }
        if (OnlineMultiplayerActive.Instance.isOnline && 
            boardManager.onlineMultiplayerManager.player != boardManager.activePlayer)
        {
            // online multiplayer active: other player must move next
            boardManager.ingameMessagesManager.StartNewIngameMessage(boardManager.textOpponentsTurn.gameObject, 3);
            return;
        }

        StartCoroutine(WaitSelector());
    }

    private IEnumerator WaitSelector()  
        // select or deselect a clicked piece
        // this function ensures that a hitted (deactivated) piece does not registrate a selection-try 
    {
        yield return null;
        if (boardManager.activePlayer != player && gameObject.activeSelf)
        {
            if (boardManager.activeSelection)
            {
                // enemy piece not reachable -> cannot be hitted by currently selected piece
                boardManager.ingameMessagesManager.StartNewIngameMessage(boardManager.textEnemyNotReachable.gameObject, 3);
            } else
            {
                // wrong player: enemy pieces cannot be selected
                boardManager.ingameMessagesManager.StartNewIngameMessage(boardManager.textWrongPlayer.gameObject, 3);
            }
        } else
        {
            if (!isSelected && gameObject.activeSelf)
            {
                if (!boardManager.activeSelection)
                {
                    SelectPiece();
                    AudioManager.Instance.PlaySelectSoundEffect();
                }
            }
            else if(isSelected && gameObject.activeSelf)
            {
                DeselectPiece();
                AudioManager.Instance.PlaySelectSoundEffect();
            }
        }
    }

    private void SelectPiece()
    {
        legalTiles = GetLegalToMoveTiles();
        if (legalTiles.Count == 0)
        {
            // no "legal to move tiles" -> piece cannot move -> player must choose other piece
            boardManager.ingameMessagesManager.StartNewIngameMessage(boardManager.textPieceUnmoveable.gameObject, 3);
            return;
        }

        isSelected = true;
        boardManager.activeSelection = true;
        boardManager.activeSelectionLegalTiles = CloneListVector2(legalTiles);

        // highlight tile of selected piece
        boardManager.ChangeTile(GetTileForPosition(transform.position), 
            new TileBase[2] { boardManager.brightTileSelected, boardManager.darkTileSelected });
        // highlight all "legal to move" tiles
        foreach (Vector2Int tile in legalTiles)
        {
            boardManager.SetCorrectTile(tile);
        }
        // for king: calculate possible castling moves
        if (tag == "King")
        {
            castling = GetLegalCastlingTiles();
            foreach (Vector2Int tile in castling.Keys)  // highlight castling tiles as "legal to move"
            {
                boardManager.activeSelectionLegalTiles.Add(tile);
                boardManager.SetCorrectTile(tile);
            }
        }
    }

    private void DeselectPiece()
    {
        isSelected = false;
        boardManager.activeSelection = false;
        boardManager.activeSelectionLegalTiles.Clear();

        // dehighlight the tile of deselected piece
        boardManager.SetCorrectTile(GetTileForPosition(transform.position));
        // dehighlight the "legal to move" tiles
        foreach (Vector2Int tile in legalTiles)
        {
            boardManager.SetCorrectTile(tile);
        }
        // for king: dehighlight castling tiles
        if (tag == "King")
        {
            foreach (Vector2Int tile in castling.Keys)
            {
                boardManager.SetCorrectTile(tile);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (boardManager.ending != "stillRunning" || MenuManager.Instance.gamePaused)
        {
            return;  // game is over or paused
        }
        if (!boardManager.readyForNextMove)
        {
            return;  // wait for BoardManager.Update()
        }

        // check if player just moved 
        if (boardManager.activePlayer != player)
        {
            return;  // leave update because player is not allowed to move (other player must move)
        }

        colliderBounds = GetComponent<BoxCollider2D>().bounds;
        mousePos = Input.mousePosition;
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
        mouseWorldPos.z = transform.position.z;  // ACHTUNG: Muss dies angepasst werden, falls Schachfiguren und Schachbrett andere z-Werte haben?

        // for king: create mouseover effect for possible castling moves
        if (tag == "King" && isSelected)
        {
            Vector2Int tileUnderMouse = GetTileForPosition(mouseWorldPos);
            foreach (KeyValuePair<Vector2Int, GameObject> c in castling)
            {
                if (tileUnderMousePrev == new Vector2Int(-100, -100) || tileUnderMousePrev == tileUnderMouse)
                {
                    break;  // mouse position did not change 
                }
                if (RectContain(c.Key, boardManager.tileSize, new Vector2(mouseWorldPos.x, mouseWorldPos.y))) {
                    // mouse is over a possible target tile for castling
                    boardManager.ChangeTile(GetTileForPosition(c.Value.transform.position),
                        new TileBase[2] { boardManager.brightTileSelected, boardManager.darkTileSelected });
                } else if (c.Key == tileUnderMousePrev)
                {
                    // mouse was over possible target tile for castling (and is not anymore)
                    boardManager.SetCorrectTile(GetTileForPosition(c.Value.transform.position));
                }
            }
            tileUnderMousePrev = tileUnderMouse;
        }

        if (isSelected && Input.GetMouseButtonDown(0) && !colliderBounds.Contains(mouseWorldPos))
        {
            // check if clicked position is a "legal to move" tile
            foreach (Vector2Int tile in legalTiles)
            {
                Vector2Int lowerLeftCorner = new Vector2Int(tile.x, tile.y);
                if (RectContain(lowerLeftCorner, boardManager.tileSize, new Vector2(mouseWorldPos.x, mouseWorldPos.y)))
                {
                    Vector3 targetPos = new Vector3((float)System.Math.Floor(mouseWorldPos.x) + (float)boardManager.tileSize / 2, 
                        (float)System.Math.Floor(mouseWorldPos.y) + (float)boardManager.tileSize / 2, mouseWorldPos.z);
                    // TRY MOVE!
                    TryMove(targetPos);
                    break;
                }
            }
            // check if castling is possible
            if (tag == "King" && castling.Count != 0)
            {
                foreach (KeyValuePair<Vector2Int, GameObject> c in castling)
                {
                    if (RectContain(c.Key, boardManager.tileSize, new Vector2(mouseWorldPos.x, mouseWorldPos.y)))
                    {
                        Vector3 targetPos = new Vector3((float)System.Math.Floor(mouseWorldPos.x) + (float)boardManager.tileSize / 2,
                            (float)System.Math.Floor(mouseWorldPos.y) + (float)boardManager.tileSize / 2, mouseWorldPos.z);
                        // TRY CASTLING-MOVE!
                        TryMove(targetPos);
                        break;
                    }
                }
            }
        }
    }

    public bool TryMove(Vector3 targetPos, bool justTry=false)
        // move to "targetPos" if own king is not in chess afterwards, do not move otherwise
        // return true if move was executed, false otherwise
        // wenn TryMove() called to check for checkmate/tie: 
        //       then "justTry" = true -> only try if move is possible, but do not execute it (even if possible)
    {
        Vector2Int currentKingTile = GetTileForPosition(ownKing.transform.position);  // used for dehighlighting of king that was in check
        Vector2Int kingTile = GetTileForPosition(ownKing.transform.position);
        if (tag == "King")
        {
            kingTile = GetTileForPosition(targetPos);
        }
        Vector2Int prevTile = GetTileForPosition(transform.position);
        List<Vector2Int> lastMoveList = new List<Vector2Int>();

        if (boardManager.occupiedTiles.ContainsKey(GetTileForPosition(targetPos)))
        {
            // target tile is occupied by enemy -> hit (deactivate enemy's piece)
            GameObject otherPiece = boardManager.occupiedTiles[GetTileForPosition(targetPos)];
            SetActive(otherPiece, false, justTry);
            boardManager.occupiedTiles[GetTileForPosition(targetPos)] = gameObject;  // update position of moved chess piece in dictionary
            boardManager.occupiedTiles.Remove(GetTileForPosition(transform.position));  // mark old position as empty in dictionary
            if (boardManager.KingInCheckBy(kingTile, player).Count != 0)
            {
                // move not allowed since own king would be in chess afterwards -> reverse changes above
                SetActive(otherPiece, true, justTry);
                boardManager.occupiedTiles[GetTileForPosition(targetPos)] = otherPiece;
                boardManager.occupiedTiles[GetTileForPosition(transform.position)] = gameObject;
                if (!justTry)
                {
                    Message_KingWouldBeInCheck();
                }
                return false;
            }
            // move is allowed -> execute move (if "justTry" = false)
            if (justTry)
            {
                // move should not be executed -> reverse changes above
                SetActive(otherPiece, true, justTry);
                boardManager.occupiedTiles[GetTileForPosition(targetPos)] = otherPiece;
                boardManager.occupiedTiles[GetTileForPosition(transform.position)] = gameObject;
                return true; 
            }
            Destroy(otherPiece);  // destroy hitted enemy piece finally
            if (isSelected)
            {
                DeselectPiece();
            }
            transform.position = targetPos;  // actual move
            AudioManager.Instance.PlayHitSoundEffect();
        } else
        {
            // check if castling move was selected
            if (castling.ContainsKey(GetTileForPosition(targetPos)) && !justTry)
            {
                GameObject rook = castling[GetTileForPosition(targetPos)];  // the rook used during castling move
                Vector3 targetPosRook;
                if (transform.position.x < targetPos.x)  // king wants to move rightwards during castling
                {
                    targetPosRook = new Vector3(rook.transform.position.x - 2, rook.transform.position.y, rook.transform.position.z);
                } else  // king wants to move leftwards during castling
                {
                    targetPosRook = new Vector3(rook.transform.position.x + 3, rook.transform.position.y, rook.transform.position.z);
                }
                boardManager.occupiedTiles[GetTileForPosition(targetPos)] = gameObject;  // update occupiedTiles for king
                boardManager.occupiedTiles.Remove(GetTileForPosition(transform.position));  // update occupiedTiles for king
                boardManager.occupiedTiles[GetTileForPosition(targetPosRook)] = rook;  // update occupiedTiles for rook
                boardManager.occupiedTiles.Remove(GetTileForPosition(rook.transform.position));  // update occupiedTiles for rook
                if (boardManager.KingInCheckBy(kingTile, player).Count != 0)
                {
                    // move not allowed since own king would be in chess afterwards -> reverse changes above
                    boardManager.occupiedTiles.Remove(GetTileForPosition(targetPos));
                    boardManager.occupiedTiles[GetTileForPosition(transform.position)] = gameObject;
                    boardManager.occupiedTiles.Remove(GetTileForPosition(targetPosRook));
                    boardManager.occupiedTiles[GetTileForPosition(rook.transform.position)] = rook;
                    Message_KingWouldBeInCheck();
                    return false;
                }
                lastMoveList.Add(GetTileForPosition(rook.transform.position));
                lastMoveList.Add(GetTileForPosition(targetPosRook));
                // move is allowed -> execute move (remark: "justTry" = true is never called for castling) 
                DeselectPiece();
                boardManager.SetCorrectTile(GetTileForPosition(rook.transform.position));  // dehighlight rook tile
                transform.position = targetPos;  // actual move of king
                rook.transform.position = targetPosRook;  // move of rook
                AudioManager.Instance.PlayMoveSoundEffect();
            } else
            {
                // target tile is empty (no castling)
                boardManager.occupiedTiles[GetTileForPosition(targetPos)] = gameObject;  // update position of moved chess piece in dictionary
                boardManager.occupiedTiles.Remove(GetTileForPosition(transform.position));  // mark old position as empty in dictionary
                if (boardManager.KingInCheckBy(kingTile, player).Count != 0)
                {
                    // move not allowed since own king would be in chess afterwards -> reverse changes above
                    boardManager.occupiedTiles.Remove(GetTileForPosition(targetPos));
                    boardManager.occupiedTiles[GetTileForPosition(transform.position)] = gameObject;
                    if (!justTry)
                    {
                        Message_KingWouldBeInCheck();
                    }
                    return false;
                }
                // move is allowed -> execute move (if "justTry" = false)
                if (justTry)
                {
                    // move should not be executed -> reverse changes above
                    boardManager.occupiedTiles.Remove(GetTileForPosition(targetPos));
                    boardManager.occupiedTiles[GetTileForPosition(transform.position)] = gameObject;
                    return true;
                }
                if (isSelected)
                {
                    DeselectPiece();
                }
                transform.position = targetPos;  // actual move
                AudioManager.Instance.PlayMoveSoundEffect();
            }
        }

        // AFTER the move:
        if (atStart)
        {
            atStart = false;  // remember that piece is not at start position any more
        }
        if (boardManager.inCheck)
        {
            List<GameObject> formerCheckSetter = CloneListGameObject(boardManager.checkSetter);
            // remember that own king is not in check anymore
            boardManager.inCheck = false;
            boardManager.checkSetter.Clear();
            boardManager.textInCheck.gameObject.SetActive(false);
            // dehighlight king (which was in check) and all former check setter
            boardManager.SetCorrectTile(currentKingTile);
            foreach (GameObject piece in formerCheckSetter)
            {
                boardManager.SetCorrectTile(GetTileForPosition(piece.transform.position));
            }
        }
        // check if pawn must be promoted
        CheckPawnPromotion(gameObject);
        // remember last move (and ensure that last move is displayed correctly if required)
        List<Vector2Int> oldLastMove = CloneListVector2(boardManager.lastMove);
        lastMoveList.Add(prevTile);
        lastMoveList.Add(GetTileForPosition(transform.position));
        boardManager.lastMove = lastMoveList;
        foreach (Vector2Int tile in lastMoveList)
        {
            boardManager.SetCorrectTile(tile);
        }
        foreach (Vector2Int tile in oldLastMove)
        {
            boardManager.SetCorrectTile(tile);
        }
        // document last move
        boardManager.DocumentMove(boardManager.lastMove, Application.persistentDataPath + "/allMoves.txt");
        boardManager.DocumentMove(boardManager.lastMove, Application.dataPath + "/allMoves.txt");
        // other player must move next
        boardManager.ChangeActivePlayer(player);
        boardManager.readyForNextMove = false;  // -> so BoardManager.Update() can check if enemy king is set check/checkmate/tied
        
        // if online multiplayer active: tell opponent player which move was made
        if (OnlineMultiplayerActive.Instance.isOnline)
        {
            if (player == "white" && boardManager.onlineMultiplayerManager.player == "white")
            {
                boardManager.onlineMultiplayerManager.CmdTellServerLastMoveAll(true, false, gameObject.name, targetPos, boardManager.pawnPromotionMenu.activeSelf);
            } else if (player == "black" && boardManager.onlineMultiplayerManager.player == "black")
            {
                boardManager.onlineMultiplayerManager.CmdTellServerLastMoveAll(false, true, gameObject.name, targetPos, boardManager.pawnPromotionMenu.activeSelf);
            }
        }
        return true;
    }

    public List<Vector2Int> GetLegalToMoveTiles() 
        // calculate tiles which chess piece can move next (represented by tile's lower left corner)
    {
        switch (tag)  // tag is the type of the piece ("Pawn", "Rook", "Knight", "Bishop", "King" or "Queen")
        {
            case "Pawn":
                return GetComponent<PieceMovement>().GetLegalToMoveTilesPawn(moveDirection, atStart);
            case "Rook":
                return GetComponent<PieceMovement>().GetLegalToMoveTilesRook();
            case "Knight":
                return GetComponent<PieceMovement>().GetLegalToMoveTilesKnight();
            case "Bishop":
                return GetComponent<PieceMovement>().GetLegalToMoveTilesBishop();
            case "Queen":
                return GetComponent<PieceMovement>().GetLegalToMoveTilesQueen();
            case "King":
                return GetComponent<PieceMovement>().GetLegalToMoveTilesKing();
        }
        throw new System.Exception("Chess piece has unknown tag.");
    }

    public static Vector2Int GetTileForPosition(Vector3 worldPos) 
        // calculate the lower left corner coordinates of the corresponding tile for a given world position
    {
        return new Vector2Int((int)System.Math.Floor(worldPos.x), (int)System.Math.Floor(worldPos.y));
    }

    public bool RectContain(Vector2 lowerLeftCorner, float rectSize, Vector2 point)
    {
        return point.x > lowerLeftCorner.x && point.x < lowerLeftCorner.x + rectSize && point.y > lowerLeftCorner.y && point.y < lowerLeftCorner.y + rectSize;
    }

    private void Message_KingWouldBeInCheck()
    {
        if (boardManager.inCheck)
        {
            boardManager.ingameMessagesManager.StartNewIngameMessage(boardManager.textStillOwnKingInCheck.gameObject, 3);
        } else
        {
            boardManager.ingameMessagesManager.StartNewIngameMessage(boardManager.textOwnKingInCheck.gameObject, 3);
        }
    }

    private void CheckPawnPromotion(GameObject piece)
        // if "piece" is a pawn that reached board end -> give player option to do promotion
    {
        if (piece.tag != "Pawn" || !IsPromotionTile(GetTileForPosition(piece.GetComponent<PieceController>().transform.position), player)) {
            return; 
        }
        // promotion
        boardManager.pawnPromotionMenu.SetActive(true);  // open menu to select promotion for pawn
        boardManager.pawnPromotionMenu.GetComponent<ClickToPromotePawn>().pawnToPromote = piece;
        MenuManager.Instance.gamePaused = true;

        if (OnlineMultiplayerActive.Instance.isOnline)
        {
            // tell server that this player has an open pawn promotion menu
            boardManager.onlineMultiplayerManager.CmdSetActivePawnPromotion(true);
        }
    }

    private bool IsPromotionTile(Vector2Int tile, string player)
        // can a pawn of "player" be promoted at "tile" 
    {
        return (player == "white" && tile.y == 7) || (player == "black" && tile.y == 0);
    }

    private Dictionary<Vector2Int, GameObject> GetLegalCastlingTiles()
        // for king piece: get tiles (and corresponding rooks) that are "legal to move" by castling
        // (key, value) = (castlingTargetTile for king, corresponding rook)
    {
        Vector2Int kingTile = GetTileForPosition(transform.position);
        Dictionary<Vector2Int, GameObject> castlingTiles = new Dictionary<Vector2Int, GameObject>(); ;
        if (!atStart || boardManager.inCheck)
        {
            return castlingTiles;  // castling requirements violated -> castling not possible
        }

        Vector2Int tile;
        // long castling (left side of king)
        for (int i = 1; i <= 4; i++)
        {
            tile = new Vector2Int(kingTile.x - i, kingTile.y);
            if (boardManager.occupiedTiles.ContainsKey(tile)) 
            {
                if (boardManager.occupiedTiles[tile].tag == "Rook" && boardManager.occupiedTiles[tile].GetComponent<PieceController>().atStart)
                {
                    if (CastlingThroughThreat(kingTile, "long"))
                    {
                        break;
                    }
                    castlingTiles[new Vector2Int(kingTile.x - 2, kingTile.y)] = boardManager.occupiedTiles[tile];  // long castling possible
                }
                break;
            }
        }
        // short castling (right side of king)
        for (int i = 1; i <= 3; i++)
        {
            tile = new Vector2Int(kingTile.x + i, kingTile.y);
            if (boardManager.occupiedTiles.ContainsKey(tile))
            {
                if (boardManager.occupiedTiles[tile].tag == "Rook" && boardManager.occupiedTiles[tile].GetComponent<PieceController>().atStart)
                {
                    if (CastlingThroughThreat(kingTile, "short"))
                    {
                        break;
                    }
                    castlingTiles[new Vector2Int(kingTile.x + 2, kingTile.y)] = boardManager.occupiedTiles[tile];  // short castling possible
                }
                break;
            }
        }

        return castlingTiles;
    }

    private bool CastlingThroughThreat(Vector2Int kingTile, string typeOfCastling)
        // returns true if castling move (defined by king's startPos "kingTile" and the type of castling - "long" or "short")
        // would go through a threatend tile
    {
        Vector2Int passedTile;  // tile that is passed by king during castling
        List<GameObject> enemyPieces;
        if (player == "white")
        {
            enemyPieces = boardManager.blackPiecesList;
        }
        else
        {
            enemyPieces = boardManager.whitePiecesList;
        }

        if (typeOfCastling == "long")
        {
            passedTile = new Vector2Int(kingTile.x - 1, kingTile.y);
        } else  // typeOfCastling == "short"
        {
            passedTile = new Vector2Int(kingTile.x + 1, kingTile.y);
        }

        // check if any enemy piece threats "passedTile"
        foreach (GameObject piece in enemyPieces)
        {
            if (piece == null || !piece.activeSelf)
            {
                // piece was already hitted and destroyed/deactivated -> cannot threat any tile
                continue;
            }

            List<Vector2Int> threatendTiles = piece.GetComponent<PieceController>().GetLegalToMoveTiles();
            foreach (Vector2Int tile in threatendTiles)
            {
                if (tile == passedTile)
                {
                    return true;  // castling would go through a threatened tile
                }
            }
        }
        return false;
    }


    private void SetActive(GameObject obj, bool newState, bool justTry)
        // activates ("newState" = true) or deactivates ("newState" = false) an object
        // if "justTry" = true: do not really deactivate an object -> ensures that coroutines cannot run infinitely
    {
        if (justTry)
        {
            obj.GetComponent<PieceController>().activeOnJustTry = newState;
            return;
        } else
        {
            obj.SetActive(newState);
        }
    }

    private static List<Vector2Int> CloneListVector2(List<Vector2Int> originalList)
        // creates a copy of "originalList"
    {
        List<Vector2Int> newList = new List<Vector2Int>();
        foreach (Vector2Int obj in originalList)
        {
            newList.Add(obj);
        }
        return newList;
    }

    private static List<GameObject> CloneListGameObject(List<GameObject> originalList)
    // creates a copy of "originalList" (just copies references of the GameObjects)
    {
        List<GameObject> newList = new List<GameObject>();
        foreach (GameObject obj in originalList)
        {
            newList.Add(obj);
        }
        return newList;
    }

}
