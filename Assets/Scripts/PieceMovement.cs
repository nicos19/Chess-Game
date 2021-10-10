using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceMovement : MonoBehaviour
{ 
    private PieceController pieceController;
    private BoardManager boardManager;
    private float tileSize;

    public GameObject board;

    // Start is called before the first frame update
    public void Start()
    {
        pieceController = GetComponent<PieceController>();
        boardManager = board.GetComponent<BoardManager>();
        tileSize = boardManager.tileSize;
    }

    private Vector2 GetTileForPosition(Vector3 worldPos) // calculate the lower left corner coordinates of the corresponding tile for a given world position
    {
        return new Vector2((float)System.Math.Floor(worldPos.x), (float)System.Math.Floor(worldPos.y));
    }

    private bool TileInBounce(Vector2 tile, string boardSize, float tileSize)
    {
        if (boardSize == "classic")
        {
            return pieceController.RectContain(new Vector2(0, 0), 8, new Vector2(tile.x + tileSize / 2, tile.y + tileSize / 2));
            // new Vector(0, 0) since lower left corner of chess board is at (0, 0);  8 since boardSize = "classic"
        }
        else
        {
            throw new System.ArgumentException("'boardSize' unknown");
        }
    }

    private int IncrementAbs(int variable)
        // increment "variable" by 1, while keeping positive/negative sign
    {
        if (variable == 0)
        {
            return variable;
        } else if (variable > 0)
        {
            return variable + 1;
        } else
        {
            return variable - 1;
        }
    }


    public List<Vector2> GetLegalToMoveTilesPawn(Vector2 moveDirection, bool atStart)  // moveDirection is (0, 1) for "white" and (0, -1) for "black" in classic chess
    {
        List<Vector2> legalTiles = new List<Vector2>();
        Vector2 currentTile = GetTileForPosition(transform.position);

        // pawn can move ONE FIELD FORWARDS
        Vector2 newTile = new Vector2(currentTile.x, currentTile.y + moveDirection.y);
        if (!boardManager.occupiedTiles.ContainsKey(newTile) && TileInBounce(newTile, boardManager.boardSize, boardManager.tileSize))
        {
            legalTiles.Add(newTile);
            // at start position, pawn can move TWO FIELDS FORWARDS
            newTile = new Vector2(currentTile.x, currentTile.y + moveDirection.y * 2);
            if (atStart && !boardManager.occupiedTiles.ContainsKey(newTile) 
                && TileInBounce(newTile, boardManager.boardSize, boardManager.tileSize))
            {
                legalTiles.Add(newTile);
            }
        }
        // pawn can hit enemies ONE FIELD LEFT-FORWARD
        newTile = new Vector2(currentTile.x - 1, currentTile.y + moveDirection.y);
        if (boardManager.occupiedTiles.ContainsKey(newTile) && TileInBounce(newTile, boardManager.boardSize, boardManager.tileSize))
        {
            if (boardManager.occupiedTiles[newTile].GetComponent<PieceController>().player != GetComponent<PieceController>().player)
            {
                // piece at newTile is enemy -> newTile is legal to move
                legalTiles.Add(newTile);
            }
        }
        // pawn can hit enemies ONE FIELD RIGHT-FORWARD
        newTile = new Vector2(currentTile.x + 1, currentTile.y + moveDirection.y);
        if (boardManager.occupiedTiles.ContainsKey(newTile) && TileInBounce(newTile, boardManager.boardSize, boardManager.tileSize))
        {
            if (boardManager.occupiedTiles[newTile].GetComponent<PieceController>().player != GetComponent<PieceController>().player)
            {
                // piece at newTile is enemy -> newTile is legal to move
                legalTiles.Add(newTile);
            }
        }
        return legalTiles;
    }

    public List<Vector2> GetLegalToMoveTilesRook()
    {
        List<Vector2> legalTiles = new List<Vector2>();
        Vector2 currentTile = GetTileForPosition(transform.position);

        // rook can move leftwards
        legalTiles.AddRange(GetLegalToMoveTilesDirection(currentTile, new Vector2Int(-1, 0)));
        // rook can move rightwards
        legalTiles.AddRange(GetLegalToMoveTilesDirection(currentTile, new Vector2Int(1, 0)));
        // rook can move upwards
        legalTiles.AddRange(GetLegalToMoveTilesDirection(currentTile, new Vector2Int(0, 1)));
        // rook can move downwards
        legalTiles.AddRange(GetLegalToMoveTilesDirection(currentTile, new Vector2Int(0, -1)));

        return legalTiles;
    }

    public List<Vector2> GetLegalToMoveTilesKnight()
    {
        List<Vector2> legalTiles = new List<Vector2>();
        Vector2 currentTile = GetTileForPosition(transform.position);

        // knight can jump left-upwards*2
        legalTiles.AddRange(GetLegalToMoveTilesJump(currentTile, new Vector2Int(-1, 2)));
        // knight can jump right-upwards*2
        legalTiles.AddRange(GetLegalToMoveTilesJump(currentTile, new Vector2Int(1, 2)));
        // knight can jump left-downwards*2
        legalTiles.AddRange(GetLegalToMoveTilesJump(currentTile, new Vector2Int(-1, -2)));
        // knight can jump right-downwards*2
        legalTiles.AddRange(GetLegalToMoveTilesJump(currentTile, new Vector2Int(1, -2)));
        // knight can jump left*2-upwards
        legalTiles.AddRange(GetLegalToMoveTilesJump(currentTile, new Vector2Int(-2, 1)));
        // knight can jump right*2-upwards
        legalTiles.AddRange(GetLegalToMoveTilesJump(currentTile, new Vector2Int(2, 1)));
        // knight can jump left*2-downwards
        legalTiles.AddRange(GetLegalToMoveTilesJump(currentTile, new Vector2Int(-2, -1)));
        // knight can jump right*2-downwards
        legalTiles.AddRange(GetLegalToMoveTilesJump(currentTile, new Vector2Int(2, -1)));

        return legalTiles;
    }

    public List<Vector2> GetLegalToMoveTilesBishop()
    {
        List<Vector2> legalTiles = new List<Vector2>();
        Vector2 currentTile = GetTileForPosition(transform.position);

        // bishop can move diagonally leftwards/upwards
        legalTiles.AddRange(GetLegalToMoveTilesDirection(currentTile, new Vector2Int(-1, 1)));
        // bishop can move diagonally rightwards/upwards
        legalTiles.AddRange(GetLegalToMoveTilesDirection(currentTile, new Vector2Int(1, 1)));
        // bishop can move diagonally leftwards/downwards
        legalTiles.AddRange(GetLegalToMoveTilesDirection(currentTile, new Vector2Int(-1, -1)));
        // bishop can move diagonally rightwards/downwards
        legalTiles.AddRange(GetLegalToMoveTilesDirection(currentTile, new Vector2Int(1, -1)));

        return legalTiles;
    }

    public List<Vector2> GetLegalToMoveTilesQueen()
    {
        List<Vector2> legalTiles = new List<Vector2>();
        Vector2 currentTile = GetTileForPosition(transform.position);

        // queen can do all rook moves
        legalTiles.AddRange(GetLegalToMoveTilesRook());
        // queen can do all bishop moves
        legalTiles.AddRange(GetLegalToMoveTilesBishop());

        return legalTiles;
    }

    public List<Vector2> GetLegalToMoveTilesKing()
    {
        List<Vector2> queenTiles = new List<Vector2>();
        List<Vector2> legalTiles = new List<Vector2>();
        Vector2 currentTile = GetTileForPosition(transform.position);

        // king can do same moves like queen, but only with a range of one field
        queenTiles.AddRange(GetLegalToMoveTilesQueen());
        foreach (Vector2 tile in queenTiles)
        {
            float distToKingX = Mathf.Abs(tile.x - currentTile.x);
            float distToKingY = Mathf.Abs(tile.y - currentTile.y);
            if (distToKingX == tileSize && (distToKingY == tileSize || distToKingY == 0) || 
                (distToKingX == tileSize || distToKingX == 0) && distToKingY == tileSize)
            {
                legalTiles.Add(tile);
            }
        }

        return legalTiles;
    }

    public List<Vector2> GetLegalToMoveTilesDirection(Vector2 currentTile, Vector2Int direction)
        // direction: leftward = (-1, 0), rightward = (1, 0), upward = (0, 1), downward = (0, -1)
        //            diagonal = (-1, 1) / (1, 1) / (-1, -1) / (1, -1)
    {
        List<Vector2> legalTiles = new List<Vector2>();
        int dir_x = direction.x;
        int dir_y = direction.y;

        Vector2 newTile = new Vector2(currentTile.x + tileSize * dir_x, currentTile.y + tileSize * dir_y);

        while (TileInBounce(newTile, boardManager.boardSize, tileSize))
        {
            if (boardManager.occupiedTiles.ContainsKey(newTile))
            {
                if (boardManager.occupiedTiles[newTile].GetComponent<PieceController>().player != GetComponent<PieceController>().player)
                {
                    // piece at newTile is enemy -> newTile is legal to move
                    legalTiles.Add(newTile);
                    break;
                }
                break;  // since newTile is occupied by piece of "player"
            }
            legalTiles.Add(newTile);  // newTile is not occupied -> legal to move
            dir_x = IncrementAbs(dir_x);
            dir_y = IncrementAbs(dir_y);
            newTile = new Vector2(currentTile.x + tileSize * dir_x, currentTile.y + tileSize * dir_y);
        }
        return legalTiles;
    }

    public List<Vector2> GetLegalToMoveTilesJump(Vector2 currentTile, Vector2Int jumpDirection)
        // jumpDirection: left*2-upwards = (-2, 1)
        //                right-downwards*2 = (1, -2) etc.
    {
        List<Vector2> legalTiles = new List<Vector2>();
        Vector2 newTile = new Vector2(currentTile.x + tileSize * jumpDirection.x, 
                                      currentTile.y + tileSize * jumpDirection.y);

        if (TileInBounce(newTile, boardManager.boardSize, tileSize))
        {
            if (boardManager.occupiedTiles.ContainsKey(newTile))
            {
                if (boardManager.occupiedTiles[newTile].GetComponent<PieceController>().player != GetComponent<PieceController>().player)
                {
                    // piece at newTile is enemy -> newTile is legal to move
                    legalTiles.Add(newTile);
                }
            }
            else
            {
                // newTile is not occupied -> legal to move
                legalTiles.Add(newTile);
            }
        }
        return legalTiles;
    }

}
