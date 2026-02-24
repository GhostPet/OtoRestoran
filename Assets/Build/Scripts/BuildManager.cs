using UnityEngine;
using TMPro;

public class BuildManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public Camera cam;
    public LayerMask groundLayer;
    public TextMeshProUGUI buttonText;

    [Header("Build UI")]
    public GameObject buildPanel;
    public BuildInventory inventory;

    [Header("Current Selection")]
    public GameObject currentPrefab;

    private bool buildMode = false;
    private GameObject ghost;
    private int rotation = 0;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        if (!buildMode)
            return;

        if (currentPrefab == null || gridManager == null)
            return;

        if (Input.GetKeyDown(KeyCode.R))
            Rotate();

        UpdateGhost();

        if (Input.GetMouseButtonDown(0))
            TryPlace();

        if (Input.GetMouseButtonDown(1))
            TryRemove();
    }

    #region BUILD MODE

    public void ToggleBuildModeUI()
    {
        buildMode = !buildMode;

        if (buildPanel != null)
            buildPanel.SetActive(buildMode);

        if (buildMode)
            inventory.GenerateUI(this);

        if (!buildMode && ghost != null)
            Destroy(ghost);

        if (buttonText != null)
            buttonText.text = buildMode ? "Build Mode: ON" : "Build Mode: OFF";
    }

    #endregion

    #region ROTATION

    void Rotate()
    {
        rotation += 90;
        if (rotation >= 360)
            rotation = 0;

        if (ghost != null)
            ghost.transform.rotation = Quaternion.Euler(0, rotation, 0);
    }

    #endregion

    #region GHOST

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

    #endregion

    #region PLACE

    void TryPlace()
    {
        if (!inventory.HasItem(currentPrefab))
            return;

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

        GameObject placedGO = Instantiate(currentPrefab, finalPos, Quaternion.Euler(0, rotation, 0));

        // Ensure the instantiated object has its PlaceableObject data set so removal can
        // correctly calculate which grid cells to free and which prefab to return to inventory.
        var placedData = placedGO.GetComponent<PlaceableObject>();
        if (placedData != null)
        {
            placedData.placedGridPosition = gridPos;
            placedData.placedRotation = rotation;
            // Ensure originalPrefab reference is set on the instance in case the prefab asset
            // wasn't assigned in the inspector.
            if (placedData.originalPrefab == null)
                placedData.originalPrefab = currentPrefab;
        }

        gridManager.SetOccupiedArea(gridPos, w, h, true);

        inventory.RemoveItem(currentPrefab);
        inventory.GenerateUI(this);
    }

    #endregion

    #region REMOVE

    void TryRemove()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        PlaceableObject placed = hit.collider.GetComponentInParent<PlaceableObject>();
        if (placed == null)
            return;

        // Use stored grid position and rotation from the placed object so we free the exact
        // area that was originally reserved when placing.
        Vector2Int gridPos = placed.placedGridPosition;

        int w = placed.width;
        int h = placed.height;

        int placedRot = placed.placedRotation;
        if (placedRot == 90 || placedRot == 270)
        {
            int temp = w;
            w = h;
            h = temp;
        }

        gridManager.SetOccupiedArea(gridPos, w, h, false);

        inventory.AddItem(placed.originalPrefab);

        Destroy(placed.gameObject);

        inventory.GenerateUI(this);
    }

    #endregion

    #region VISUAL

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

    #endregion

    public void SetCurrentPrefab(GameObject prefab)
    {
        currentPrefab = prefab;

        if (ghost != null)
            Destroy(ghost);
    }
}