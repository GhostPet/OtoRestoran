using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;

    private GridCell[,] grid;

    void Awake()
    {
        grid = new GridCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new GridCell();
            }
        }
    }

    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        Vector3 origin = transform.position -
            new Vector3(width * cellSize / 2, 0, height * cellSize / 2);

        float percentX = (worldPos.x - origin.x) / cellSize;
        float percentY = (worldPos.z - origin.z) / cellSize;

        int x = Mathf.RoundToInt(percentX);
        int y = Mathf.RoundToInt(percentY);

        return new Vector2Int(x, y);
    }



    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        Vector3 origin = transform.position -
            new Vector3(width * cellSize / 2, 0, height * cellSize / 2);

        return origin + new Vector3(
            gridPos.x * cellSize,
            0.5f, // <-- bunu ekledik
            gridPos.y * cellSize
        );
    }



    public bool IsCellOccupied(Vector2Int pos)
    {
        if (!IsInsideGrid(pos)) return true;
        return grid[pos.x, pos.y].isOccupied;
    }

    public void SetOccupied(Vector2Int pos, bool value)
    {
        if (!IsInsideGrid(pos)) return;
        grid[pos.x, pos.y].isOccupied = value;
    }

    bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width &&
               pos.y >= 0 && pos.y < height;
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        Vector3 origin = transform.position -
            new Vector3(width * cellSize / 2, 0, height * cellSize / 2);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = origin + new Vector3(
                    x * cellSize,
                    0,
                    y * cellSize
                );

                Gizmos.DrawWireCube(
                    worldPos + new Vector3(cellSize / 2, 0, cellSize / 2),
                    new Vector3(cellSize, 0.05f, cellSize)
                );
            }
        }
    }

}

public class GridCell
{
    public bool isOccupied = false;
}
