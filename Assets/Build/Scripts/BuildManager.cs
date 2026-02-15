using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;
    public Camera cam;
    public GameObject currentPrefab;
    public LayerMask groundLayer;

    private GameObject ghost;
    private Vector2Int currentGridPos;

    private int rotation = 0; // 0, 90, 180, 270

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        if (currentPrefab == null || gridManager == null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetKeyDown(KeyCode.R))
            Rotate();

        UpdateGhost();

        if (Input.GetMouseButtonDown(0))
            TryPlace();
    }

    void Rotate()
    {
        rotation += 90;
        if (rotation >= 360)
            rotation = 0;

        if (ghost != null)
            ghost.transform.rotation = Quaternion.Euler(0, rotation, 0);
    }

    void UpdateGhost()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            if (ghost != null)
                ghost.SetActive(false);
            return;
        }

        Vector2Int gridPos = gridManager.GetGridPosition(hit.point);

        if (!gridManager.IsInsideGrid(gridPos))
        {
            if (ghost != null)
                ghost.SetActive(false);
            return;
        }

        currentGridPos = gridPos;

        var data = currentPrefab.GetComponent<PlaceableObject>();
        if (data == null) return;

        int w = data.width;
        int h = data.height;

        if (rotation == 90 || rotation == 270)
        {
            int temp = w;
            w = h;
            h = temp;
        }

        Vector3 basePos = gridManager.GetWorldPosition(gridPos);

        Vector3 offset = new Vector3(
            (w - 1) * gridManager.cellSize / 2f,
            0,
            (h - 1) * gridManager.cellSize / 2f
        );

        Vector3 finalPos = basePos + offset;

        if (ghost == null)
        {
            ghost = Instantiate(currentPrefab);
            SetGhostMaterial(ghost);
        }

        ghost.SetActive(true);
        ghost.transform.position = finalPos;
        ghost.transform.rotation = Quaternion.Euler(0, rotation, 0);

        if (gridManager.CanPlace(gridPos, w, h))
            SetColor(Color.green);
        else
            SetColor(Color.red);
    }


    void TryPlace()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
            return;

        Vector2Int gridPos = gridManager.GetGridPosition(hit.point);

        var data = currentPrefab.GetComponent<PlaceableObject>();
        if (data == null) return;

        int w = data.width;
        int h = data.height;

        if (rotation == 90 || rotation == 270)
        {
            int temp = w;
            w = h;
            h = temp;
        }

        if (!gridManager.CanPlace(gridPos, w, h))
            return;

        Vector3 basePos = gridManager.GetWorldPosition(gridPos);

        Vector3 offset = new Vector3(
            (w - 1) * gridManager.cellSize / 2f,
            0,
            (h - 1) * gridManager.cellSize / 2f
        );

        Vector3 finalPos = basePos + offset;

        Instantiate(currentPrefab, finalPos, Quaternion.Euler(0, rotation, 0));
        gridManager.SetOccupiedArea(gridPos, w, h, true);
    }


    void SetGhostMaterial(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = new Color(0, 1, 0, 0.5f);
            r.material = m;
        }
    }

    void SetColor(Color c)
    {
        if (ghost == null) return;

        foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>())
        {
            r.material.color = new Color(c.r, c.g, c.b, 0.5f);
        }
    }
}
