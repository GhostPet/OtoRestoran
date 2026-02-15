using UnityEngine;
using UnityEngine.EventSystems;

public class GridManager : MonoBehaviour
{
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;

    private GridCell[,] grid;
    private Vector3 origin;

    void Awake()
    {
        grid = new GridCell[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = new GridCell();

        origin = transform.position -
                 new Vector3(width * cellSize / 2f, 0, height * cellSize / 2f);
    }

    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - origin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.z - origin.z) / cellSize);

        return new Vector2Int(x, y);
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        return origin + new Vector3(
            gridPos.x * cellSize + cellSize / 2f,
            0.5f,
            gridPos.y * cellSize + cellSize / 2f
        );
    }

    public bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width &&
               pos.y >= 0 && pos.y < height;
    }

    public bool IsCellOccupied(Vector2Int pos)
    {
        if (!IsInsideGrid(pos)) return true;
        return grid[pos.x, pos.y].isOccupied;
    }

    public void SetOccupiedArea(Vector2Int startPos, int objWidth, int objHeight, bool value)
    {
        for (int x = 0; x < objWidth; x++)
            for (int y = 0; y < objHeight; y++)
            {
                Vector2Int p = new Vector2Int(startPos.x + x, startPos.y + y);
                if (IsInsideGrid(p))
                    grid[p.x, p.y].isOccupied = value;
            }
    }

    public bool CanPlace(Vector2Int startPos, int objWidth, int objHeight)
    {
        for (int x = 0; x < objWidth; x++)
            for (int y = 0; y < objHeight; y++)
            {
                Vector2Int p = new Vector2Int(startPos.x + x, startPos.y + y);

                if (!IsInsideGrid(p) || IsCellOccupied(p))
                    return false;
            }

        return true;
    }
}

public class GridCell
{
    public bool isOccupied = false;
}
