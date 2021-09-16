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
    private bool atStart;
    private List<Vector2> legalTiles;
    private Vector2 moveDirection;
    //private List<GameObject> alliedPieces, enemyPieces;  
    private GameObject ownKing;
    //private string enemy;

    public GameObject board;
    public BoardManager boardManager;
    public Tilemap map;
    public string player;  // the player ("white" or "black") that is associated with the piece
    public Sprite spriteNormal;
    public Sprite spriteHighlighted;


    // Start is called before the first frame update
    void Start()
    {
        isSelected = false;
        atStart = true;
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
            StartCoroutine(ActivateAndDeactivate(boardManager.textWrongPlayer.gameObject, 3));
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

    public IEnumerator ActivateAndDeactivate(GameObject obj, float waitTime)
    // this functions activates an object (especially a displayed text) and deactivates it after a delay of "waitTime" seconds
    {
        obj.SetActive(true);
        yield return new WaitForSecondsRealtime(waitTime);
        obj.SetActive(false);
    }

    private void SelectPiece()
    {
        legalTiles = GetLegalToMoveTiles();
        if (legalTiles.Count == 0)
        {
            // no "legal to move tiles" -> piece cannot move -> player must choose other piece
            StartCoroutine(ActivateAndDeactivate(boardManager.textPieceUnmoveable.gameObject, 3));

            // TODO: Display message "Tie" if player cannot move any piece

            return;
        }
        GetComponent<SpriteRenderer>().sprite = spriteHighlighted;  // highlight selected piece
        
        foreach (Vector2 tile in legalTiles)
        {
            // highlight all "legal to move" tiles
            Vector3Int tileInt = new Vector3Int((int)tile.x, (int)tile.y, 0);  // ACHTUNG: tileInt.z = 0 nur solange Tilemap z-Wert = 0 hat
            if (map.GetTile(tileInt) == boardManager.brightTile)
            {
                map.SetTile(tileInt, boardManager.brightTileHighlighted);
            }
            else if (map.GetTile(tileInt) == boardManager.darkTile)
            {
                map.SetTile(tileInt, boardManager.darkTileHighlighted);
            }
        }
        isSelected = true;
        boardManager.activeSelection = true;
    }

    private void DeselectPiece()
    {
        GetComponent<SpriteRenderer>().sprite = spriteNormal;  // dehighlight the selected piece
        foreach (Vector2 tile in legalTiles)
        {
            // dehighlight the "legal to move" tiles
            Vector3Int tileInt = new Vector3Int((int)tile.x, (int)tile.y, 0);  // ACHTUNG: tileInt.z = 0 nur solange Tilemap z-Wert = 0 hat
            if (map.GetTile(tileInt) == boardManager.brightTileHighlighted)
            {
                map.SetTile(tileInt, boardManager.brightTile);
            }
            else if (map.GetTile(tileInt) == boardManager.darkTileHighlighted)
            {
                map.SetTile(tileInt, boardManager.darkTile);
            }
        }
        isSelected = false;
        boardManager.activeSelection = false;
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

        if (isSelected && Input.GetMouseButtonDown(0) && !colliderBounds.Contains(mouseWorldPos))
        {
            // check if clicked position is a "legal to move" tile
            foreach (Vector3 tile in legalTiles)
            {
                Vector2 lowerLeftCorner = new Vector2(tile.x, tile.y);
                if (RectContain(lowerLeftCorner, boardManager.tileSize, new Vector2(mouseWorldPos.x, mouseWorldPos.y)))
                {
                    if (atStart)
                    {
                        atStart = false;
                    }
                    Vector3 targetPos = new Vector3((float)System.Math.Floor(mouseWorldPos.x) + boardManager.tileSize / 2, 
                        (float)System.Math.Floor(mouseWorldPos.y) + boardManager.tileSize / 2, mouseWorldPos.z);
                    // MOVE!
                    TryMove(targetPos);
                    break;
                }
            }
            // player clicked illegal tile
            // TODO: Nachricht, dass Tile illegal war

        }
    }

    public bool TryMove(Vector3 targetPos, bool justTry=false)
        // move to "targetPos" if own king is not in chess afterwards, do not move otherwise
        // return true if move was executed, false otherwise
        // wenn TryMove() called to check for checkmate/tie: 
        //       then "justTry" = true -> only try if move is possible, but do not execute it (even if possible)
    {
        Vector2 kingTile = GetTileForPosition(ownKing.transform.position);
        if (gameObject.tag == "King")
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
            transform.position = targetPos;  // actual move
            DeselectPiece();
            // AFTER the move:
            // remember that own king is not in chess (anymore)
            boardManager.inCheck = false;
            boardManager.checkSetter.Clear();
            // check if pawn must be promoted
            CheckPawnPromotion(gameObject);
            // other player must move next
            boardManager.ChangeActivePlayer(player);
            boardManager.readyForNextMove = false;  // -> so BoardManager.Update() can check if enemy king is set check/checkmate/tied
        } else
        {
            // target tile is empty
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
            transform.position = targetPos;  // actual move
            DeselectPiece();
            // AFTER the move:
            // remember that own king is not in chess (anymore)
            boardManager.inCheck = false;
            boardManager.checkSetter.Clear();
            // check if pawn must be promoted
            CheckPawnPromotion(gameObject);
            // other player must move next
            boardManager.ChangeActivePlayer(player);
            boardManager.readyForNextMove = false;  // -> so BoardManager.Update() can check if enemy king is set check/checkmate/tied
        }
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
            StartCoroutine(ActivateAndDeactivate(boardManager.textStillOwnKingInCheck.gameObject, 3));
        } else
        {
            StartCoroutine(ActivateAndDeactivate(boardManager.textOwnKingInCheck.gameObject, 3));
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
