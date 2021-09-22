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
    private bool atStart;  // whether the piece is still at its starting position (and did not move yet)
    private List<Vector2> legalTiles;
    private Vector2 moveDirection;  
    private GameObject ownKing;
    private Dictionary<Vector2, GameObject> castling;  // for king: chances for castling
    private Vector2 tileUnderMousePrev = new Vector2(-100, -100);  // the tile under the mouse during the previous frame

    public GameObject board;
    public BoardManager boardManager;
    public Tilemap map;
    public string player;  // the player ("white" or "black") that is associated with the piece


    // Start is called before the first frame update
    void Start()
    {
        isSelected = false;
        atStart = true;
        castling = new Dictionary<Vector2, GameObject>();
        boardManager = board.GetComponent<BoardManager>();
        map = boardManager.map;
        boardManager.occupiedTiles.Add(GetTileForPosition(transform.position), gameObject);

        if (player == "white")
        {
            moveDirection = new Vector2(0, 1);
            ownKing = boardManager.whiteKing;
        } else if (player == "black")
        {
            moveDirection = new Vector2(0, -1);
            ownKing = boardManager.blackKing;
        }
    }

    private void OnMouseDown() // when chess piece is clicked then select or deselect piece
    {
        if (boardManager.ending != "stillRunning" || boardManager.activeMenu)
        {
            return;  // game is over or paused 
        }
        if (!boardManager.readyForNextMove)
        {
            return;  // wait for BoardManager.Update()
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
            // (wrong player: the other player has to move)
            boardManager.StartNewIngameMessage(boardManager.textWrongPlayer.gameObject, 3);
        } else
        {
            if (!isSelected && gameObject.activeSelf)
            {
                if (!boardManager.activeSelection)
                {
                    SelectPiece();
                }
            }
            else if(isSelected && gameObject.activeSelf)
            {
                DeselectPiece();
            }
        }
    }

    /*public IEnumerator ActivateAndDeactivate(GameObject obj, float waitTime)
    // this functions activates an object (especially a displayed text) and deactivates it after a delay of "waitTime" seconds
    {
        obj.SetActive(true);
        yield return new WaitForSecondsRealtime(waitTime);
        obj.SetActive(false);
    }*/

    private void SelectPiece()
    {
        legalTiles = GetLegalToMoveTiles();
        if (legalTiles.Count == 0)
        {
            // no "legal to move tiles" -> piece cannot move -> player must choose other piece
            boardManager.StartNewIngameMessage(boardManager.textPieceUnmoveable.gameObject, 3);
            return;
        }
        // highlight tile of selected piece
        ChangeTile(GetTileForPosition(transform.position), new TileBase[2] { boardManager.brightTileSelected, boardManager.darkTileSelected });
        // highlight all "legal to move" tiles
        foreach (Vector2 tile in legalTiles)
        {
            ChangeTile(tile, new TileBase[2] { boardManager.brightTileLegalToMove, boardManager.darkTileLegalToMove });
        }
        // for king: calculate possible castling moves
        if (tag == "King")
        {
            castling = GetLegalCastlingTiles();
            foreach (Vector2 tile in castling.Keys)  // highlight castling tiles as "legal to move"
            {
                ChangeTile(tile, new TileBase[2] { boardManager.brightTileLegalToMove, boardManager.darkTileLegalToMove });
            }
        }
        isSelected = true;
        boardManager.activeSelection = true;
    }

    private void DeselectPiece()
    {
        // dehighlight the tile of deselected piece
        ChangeTileReverse(GetTileForPosition(transform.position));
        // dehighlight the "legal to move" tiles
        foreach (Vector2 tile in legalTiles)
        {
            ChangeTileReverse(tile);
        }
        // for king: dehighlight castling tiles
        if (tag == "King")
        {
            foreach (Vector2 tile in castling.Keys)
            {
                ChangeTileReverse(tile);
            }
        }
        isSelected = false;
        boardManager.activeSelection = false;
    } 

    private void ChangeTile(Vector2 tilePos, TileBase[] newTile)
        // replace tile at "tilePos" with newTile[0] or newTile[1] for a brighTile or a darkTile respectively
    {
        Vector3Int tilePosInt = new Vector3Int((int)tilePos.x, (int)tilePos.y, 0);  // WARNING: tilePos.z = 0 only if Tilemap's z-value = 0
        if (map.GetTile(tilePosInt) == boardManager.brightTile)
        {
            map.SetTile(tilePosInt, newTile[0]);
        }
        else if (map.GetTile(tilePosInt) == boardManager.darkTile)
        {
            map.SetTile(tilePosInt, newTile[1]);
        }
        else
        {
            throw new System.ArgumentException("Unknown/Wrong Tile!");
        }
    }

    private void ChangeTileReverse(Vector2 tilePos)
        // replace tile at "tilePos" with corresponding original tile
    {
        Vector3Int tilePosInt = new Vector3Int((int)tilePos.x, (int)tilePos.y, 0);  // WARNING: tilePos.z = 0 only if Tilemap's z-value = 0
        if (map.GetTile(tilePosInt) == boardManager.brightTileSelected || map.GetTile(tilePosInt) == boardManager.brightTileCheck ||
                map.GetTile(tilePosInt) == boardManager.brightTileLastTurn || map.GetTile(tilePosInt) == boardManager.brightTileLegalToMove)
        {
            map.SetTile(tilePosInt, boardManager.brightTile);
        }
        else if (map.GetTile(tilePosInt) == boardManager.darkTileSelected || map.GetTile(tilePosInt) == boardManager.darkTileCheck ||
                map.GetTile(tilePosInt) == boardManager.darkTileLastTurn || map.GetTile(tilePosInt) == boardManager.darkTileLegalToMove)
        {
            map.SetTile(tilePosInt, boardManager.darkTile);
        }
        else
        {
            throw new System.ArgumentException($"Unknown/Wrong Tile at {tilePosInt}! Found {map.GetTile(tilePosInt)}");
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (boardManager.ending != "stillRunning" || boardManager.activeMenu)
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
            Vector2 tileUnderMouse = GetTileForPosition(mouseWorldPos);
            foreach (KeyValuePair<Vector2, GameObject> c in castling)
            {
                if (tileUnderMousePrev == new Vector2(-100, -100) || tileUnderMousePrev == tileUnderMouse)
                {
                    break;  // mouse position did not change 
                }
                if (RectContain(c.Key, boardManager.tileSize, new Vector2(mouseWorldPos.x, mouseWorldPos.y))) {
                    // mouse is over a possible target tile for castling
                    ChangeTile(GetTileForPosition(c.Value.transform.position),
                        new TileBase[2] { boardManager.brightTileSelected, boardManager.darkTileSelected });
                } else if (c.Key == tileUnderMousePrev)
                {
                    // mouse was over possible target tile for castling (and is not anymore)
                    ChangeTileReverse(GetTileForPosition(c.Value.transform.position));
                }
            }
            tileUnderMousePrev = tileUnderMouse;
        }

        if (isSelected && Input.GetMouseButtonDown(0) && !colliderBounds.Contains(mouseWorldPos))
        {
            // check if clicked position is a "legal to move" tile
            foreach (Vector3 tile in legalTiles)
            {
                Vector2 lowerLeftCorner = new Vector2(tile.x, tile.y);
                if (RectContain(lowerLeftCorner, boardManager.tileSize, new Vector2(mouseWorldPos.x, mouseWorldPos.y)))
                {
                    Vector3 targetPos = new Vector3((float)System.Math.Floor(mouseWorldPos.x) + boardManager.tileSize / 2, 
                        (float)System.Math.Floor(mouseWorldPos.y) + boardManager.tileSize / 2, mouseWorldPos.z);
                    // TRY MOVE!
                    TryMove(targetPos);
                    break;
                }
            }
            // check if castling is possible
            if (tag == "King" && castling.Count != 0)
            {
                foreach (KeyValuePair<Vector2, GameObject> c in castling)
                {
                    if (RectContain(c.Key, boardManager.tileSize, new Vector2(mouseWorldPos.x, mouseWorldPos.y)))
                    {
                        Vector3 targetPos = new Vector3((float)System.Math.Floor(mouseWorldPos.x) + boardManager.tileSize / 2,
                        (float)System.Math.Floor(mouseWorldPos.y) + boardManager.tileSize / 2, mouseWorldPos.z);
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
        Vector2 kingTile = GetTileForPosition(ownKing.transform.position);
        if (tag == "King")
        {
            kingTile = GetTileForPosition(targetPos);
        } 

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
                boardManager.occupiedTiles[transform.position] = gameObject;
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
                boardManager.occupiedTiles[transform.position] = gameObject;
                return true; 
            }
            Destroy(otherPiece);  // destroy hitted enemy piece finally
            DeselectPiece();
            transform.position = targetPos;  // actual move
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
                // move is allowed -> execute move (remark: "justTry" = true is never called for castling) 
                DeselectPiece();
                ChangeTileReverse(GetTileForPosition(rook.transform.position));  // dehighlight rook
                transform.position = targetPos;  // actual move of king
                rook.transform.position = targetPosRook;  // move of rook
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
                DeselectPiece();
                transform.position = targetPos;  // actual move
            }
        }

        // AFTER the move:
        if (atStart)
        {
            atStart = false;  // remember that piece is not at start position any more
        }
        // remember that own king is not in chess (anymore)
        boardManager.inCheck = false;
        boardManager.checkSetter.Clear();
        // check if pawn must be promoted
        CheckPawnPromotion(gameObject);
        // other player must move next
        boardManager.ChangeActivePlayer(player);
        boardManager.readyForNextMove = false;  // -> so BoardManager.Update() can check if enemy king is set check/checkmate/tied
        return true;
    }

    public List<Vector2> GetLegalToMoveTiles() 
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

    public Vector2 GetTileForPosition(Vector3 worldPos) 
        // calculate the lower left corner coordinates of the corresponding tile for a given world position
    {
        return new Vector2((float)System.Math.Floor(worldPos.x), (float)System.Math.Floor(worldPos.y));
    }

    public bool RectContain(Vector2 lowerLeftCorner, float rectSize, Vector2 point)
    {
        return point.x >= lowerLeftCorner.x && point.x <= lowerLeftCorner.x + rectSize && point.y >= lowerLeftCorner.y && point.y <= lowerLeftCorner.y + rectSize;
    }

    private void Message_KingWouldBeInCheck()
    {
        if (boardManager.inCheck)
        {
            boardManager.StartNewIngameMessage(boardManager.textStillOwnKingInCheck.gameObject, 3);
        } else
        {
            boardManager.StartNewIngameMessage(boardManager.textOwnKingInCheck.gameObject, 3);
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
        boardManager.activeMenu = true;
    }

    private bool IsPromotionTile(Vector2 tile, string player)
        // can a pawn of "player" be promoted at "tile" 
    {
        return (player == "white" && tile.y == 7) || (player == "black" && tile.y == 0);
    }

    private Dictionary<Vector2, GameObject> GetLegalCastlingTiles()
        // for king piece: get tiles (and corresponding rooks) that are "legal to move" by castling
        // (key, value) = (castlingTargetTile for king, corresponding rook)
    {
        Vector2 kingTile = GetTileForPosition(transform.position);
        Dictionary<Vector2, GameObject> castlingTiles = new Dictionary<Vector2, GameObject>(); ;
        if (!atStart || boardManager.inCheck)
        {
            return castlingTiles;  // castling requirements violated -> castling not possible
        }

        Vector2 tile;
        // long castling (left side of king)
        for (int i = 1; i <= 4; i++)
        {
            tile = new Vector2(kingTile.x - i, kingTile.y);
            if (boardManager.occupiedTiles.ContainsKey(tile)) 
            {
                if (boardManager.occupiedTiles[tile].tag == "Rook" && boardManager.occupiedTiles[tile].GetComponent<PieceController>().atStart)
                {
                    if (CastlingThroughThreat(kingTile, "long"))
                    {
                        break;
                    }
                    castlingTiles[new Vector2(kingTile.x - 2, kingTile.y)] = boardManager.occupiedTiles[tile];  // long castling possible
                }
                break;
            }
        }
        // short castling (right side of king)
        for (int i = 1; i <= 3; i++)
        {
            tile = new Vector2(kingTile.x + i, kingTile.y);
            if (boardManager.occupiedTiles.ContainsKey(tile))
            {
                if (boardManager.occupiedTiles[tile].tag == "Rook" && boardManager.occupiedTiles[tile].GetComponent<PieceController>().atStart)
                {
                    if (CastlingThroughThreat(kingTile, "short"))
                    {
                        break;
                    }
                    castlingTiles[new Vector2(kingTile.x + 2, kingTile.y)] = boardManager.occupiedTiles[tile];  // short castling possible
                }
                break;
            }
        }

        return castlingTiles;
    }

    private bool CastlingThroughThreat(Vector2 kingTile, string typeOfCastling)
        // returns true if castling move (defined by king's startPos "kingTile" and the type of castling - "long" or "short")
        // would go through a threatend tile
    {
        Vector2 passedTile;  // tile that is passed by king during castling
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
            passedTile = new Vector2(kingTile.x - 1, kingTile.y);
        } else  // typeOfCastling == "short"
        {
            passedTile = new Vector2(kingTile.x + 1, kingTile.y);
        }

        // check if any enemy piece threats "passedTile"
        foreach (GameObject piece in enemyPieces)
        {
            if (piece == null || !piece.activeSelf)
            {
                // piece was already hitted and destroyed/deactivated -> cannot threat any tile
                continue;
            }

            List<Vector2> threatendTiles = piece.GetComponent<PieceController>().GetLegalToMoveTiles();
            foreach (Vector2 tile in threatendTiles)
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
        // if "justTry" = true: do nothing -> ensures that coroutines cannot run infinitely
    {
        if (justTry)
        {
            return;
        } else
        {
            obj.SetActive(newState);
        }
    }

}
