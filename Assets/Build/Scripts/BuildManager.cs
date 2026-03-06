using System.Collections.Generic;
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
    public GameObject chairPrefab;

    private bool buildMode = false;
    private GameObject ghost;
    private GameObject ghostChair;
    private int rotation = 0;
    // highlighting (outline instances)
    private PlaceableObject highlighted = null;
    private List<GameObject> highlightInstances = new List<GameObject>();
    private Material outlineMaterial;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        // always update highlight (show outlines even outside build mode)
        UpdateHighlight();
        UpdateChairGhost();

        if (!buildMode)
            return;

        if (gridManager == null)
            return;

        if (Input.GetKeyDown(KeyCode.R) && currentPrefab != null)
            Rotate();

        if (currentPrefab != null)
        {
            UpdateGhost();

            if (Input.GetMouseButtonDown(0))
                TryPlace();
        }

        // allow removing objects even when no prefab is currently selected
        if (Input.GetMouseButtonDown(1))
            TryRemove();
    }

    #region BUILD MODE

    public void ToggleBuildModeUI()
    {
        buildMode = !buildMode;

        // Clear any current selection when toggling build mode on or off
        SetCurrentPrefab(null);

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

    // ---------- table/chair helpers ----------
    bool OccupiesCell(PlaceableObject p, Vector2Int cell)
    {
        if (p == null) return false;
        Vector2Int start = p.placedGridPosition;
        int w = p.width;
        int h = p.height;
        if (p.placedRotation == 90 || p.placedRotation == 270)
        {
            int tmp = w; w = h; h = tmp;
        }

        return cell.x >= start.x && cell.x < start.x + w &&
               cell.y >= start.y && cell.y < start.y + h;
    }

    PlaceableObject GetPlaceableAtCell(Vector2Int cell)
    {
        var all = FindObjectsOfType<PlaceableObject>();
        foreach (var p in all)
        {
            if (OccupiesCell(p, cell)) return p;
        }
        return null;
    }

    List<Vector2Int> GetOccupiedCells(PlaceableObject p)
    {
        var res = new List<Vector2Int>();
        if (p == null) return res;
        Vector2Int start = p.placedGridPosition;
        int w = p.width;
        int h = p.height;
        if (p.placedRotation == 90 || p.placedRotation == 270)
        {
            int tmp = w; w = h; h = tmp;
        }

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                res.Add(new Vector2Int(start.x + x, start.y + y));

        return res;
    }

    void RemoveChairsAroundTable(PlaceableObject table)
    {
        if (table == null) return;

        // compute adjacent cells around table footprint
        var occupied = GetOccupiedCells(table);
        var adjacent = new HashSet<Vector2Int>();
        Vector2Int[] dirs = { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1) };
        foreach (var cell in occupied)
        {
            foreach (var d in dirs)
            {
                adjacent.Add(new Vector2Int(cell.x + d.x, cell.y + d.y));
            }
        }

        // find chairs that occupy any of these adjacent cells
        var all = FindObjectsOfType<PlaceableObject>();
        foreach (var p in all)
        {
            if (p.type != PlaceableType.Chair) continue;

            var chairCells = GetOccupiedCells(p);
            bool intersects = false;
            foreach (var c in chairCells)
            {
                if (adjacent.Contains(c)) { intersects = true; break; }
            }

            if (!intersects) continue;

            // remove chair: free grid, add to inventory, destroy
            gridManager.SetOccupiedArea(p.placedGridPosition, p.width, p.height, false);
            if (p.originalPrefab != null)
                inventory.AddItem(p.originalPrefab);
            if (highlighted == p)
                ClearHighlight();
            Destroy(p.gameObject);
        }
    }

    bool IsAdjacentToTable(Vector2Int cell)
    {
        Vector2Int[] dirs = { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1) };
        foreach (var d in dirs)
        {
            Vector2Int n = new Vector2Int(cell.x + d.x, cell.y + d.y);
            var p = GetPlaceableAtCell(n);
            if (p != null && p.type == PlaceableType.Table) return true;
        }
        return false;
    }

    bool CanPlaceTableWithChairs(Vector2Int tablePos, int tableW, int tableH)
    {
        // require the four adjacent cells around the table's footprint to be free (1x1)
        Vector2Int[] checkCells = new Vector2Int[4];
        checkCells[0] = new Vector2Int(tablePos.x + 1, tablePos.y);
        checkCells[1] = new Vector2Int(tablePos.x - 1, tablePos.y);
        checkCells[2] = new Vector2Int(tablePos.x, tablePos.y + 1);
        checkCells[3] = new Vector2Int(tablePos.x, tablePos.y - 1);

        foreach (var c in checkCells)
        {
            if (!gridManager.IsInsideGrid(c)) return false;
            if (gridManager.IsCellOccupied(c)) return false;
        }

        return true;
    }

    void PlaceChairsAroundTable(PlaceableObject table)
    {
        Vector2Int start = table.placedGridPosition;
        Vector2Int[] dirs = { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1) };
        foreach (var d in dirs)
        {
            Vector2Int cell = new Vector2Int(start.x + d.x, start.y + d.y);
            if (!gridManager.IsInsideGrid(cell)) continue;
            if (gridManager.IsCellOccupied(cell)) continue;

            Vector3 world = gridManager.GetWorldPosition(cell);
            GameObject chair = Instantiate(chairPrefab, world, Quaternion.identity);
            var po = chair.GetComponent<PlaceableObject>();
            if (po != null)
            {
                po.placedGridPosition = cell;
                po.placedRotation = 0;
                if (po.originalPrefab == null) po.originalPrefab = chairPrefab;
                po.type = PlaceableType.Chair;
            }
            gridManager.SetOccupiedArea(cell, 1, 1, true);
        }
    }

    void PlaceChairAt(Vector2Int cell)
    {
        if (!gridManager.IsInsideGrid(cell)) return;
        if (gridManager.IsCellOccupied(cell)) return;
        Vector3 world = gridManager.GetWorldPosition(cell);
        GameObject chair = Instantiate(chairPrefab, world, Quaternion.identity);
        var po = chair.GetComponent<PlaceableObject>();
        if (po != null)
        {
            po.placedGridPosition = cell;
            po.placedRotation = 0;
            if (po.originalPrefab == null) po.originalPrefab = chairPrefab;
            po.type = PlaceableType.Chair;
        }
        gridManager.SetOccupiedArea(cell, 1, 1, true);
    }
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
            SetGhostMaterial(ghost, new Color(0, 1, 0, 0.5f));
            Debug.Log("Created ghost object for " + currentPrefab.name);
        }

        ghost.SetActive(true);
        ghost.transform.position = finalPos;
        ghost.transform.rotation = Quaternion.Euler(0, rotation, 0);

        if (gridManager.CanPlace(gridPos, w, h))
            SetGhostColor(ghost, Color.green);
        else
            SetGhostColor(ghost, Color.red);
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

        // if we've used up the last item, clear current selection so it can't be placed anymore
        if (!inventory.HasItem(currentPrefab))
        {
            SetCurrentPrefab(null);
            inventory.GenerateUI(this);
        }

        // Special behavior: if this is a table, attempt to place chairs on its four sides
        var placedPO = placedGO.GetComponent<PlaceableObject>();
        if (placedPO != null && placedPO.type == PlaceableType.Table && chairPrefab != null)
        {
            PlaceChairsAroundTable(placedPO);
        }
    }

    #endregion

    #region REMOVE

    void TryRemove()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        PlaceableObject placed = FindPlaceableFromCollider(hit.collider);
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

        // if removing a table, also remove adjacent chairs
        if (placed.type == PlaceableType.Table)
        {
            RemoveChairsAroundTable(placed);
        }

        // add back to inventory and (for non-chair) make it the current selection so player can place it again
        if (placed.originalPrefab != null)
        {
            inventory.AddItem(placed.originalPrefab);
            if (placed.type != PlaceableType.Chair)
                SetCurrentPrefab(placed.originalPrefab);
        }

        // clear highlight if we're removing the highlighted object
        if (highlighted == placed)
            ClearHighlight();

        Destroy(placed.gameObject);

        inventory.GenerateUI(this);
    }

    #endregion

    #region VISUAL

    void SetGhostMaterial(GameObject obj, Color color)
    {
        if (obj == null) return;
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = color;
            r.material = m;
        }
    }

    void SetGhostColor(GameObject obj, Color c)
    {
        if (obj == null) return;
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            try { r.material.color = new Color(c.r, c.g, c.b, 0.5f); } catch { }
        }
    }

    // Try to find a PlaceableObject related to a collider robustly. This helps if scene
    // objects were modified at runtime (ghost destroyed, nested colliders, etc.).
    PlaceableObject FindPlaceableFromCollider(Collider col)
    {
        if (col == null) return null;

        var p = col.GetComponentInParent<PlaceableObject>();
        if (p != null) return p;

        p = col.GetComponent<PlaceableObject>();
        if (p != null) return p;

        var root = col.transform.root;
        if (root != null)
            return root.GetComponentInChildren<PlaceableObject>();

        return null;
    }

    void UpdateHighlight()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            ClearHighlight();
            return;
        }

        var po = FindPlaceableFromCollider(hit.collider);
        if (po == null)
        {
            ClearHighlight();
            return;
        }

        // don't highlight the ghost preview
        if (ghost != null && (po.gameObject == ghost || po.transform.IsChildOf(ghost.transform)))
        {
            ClearHighlight();
            return;
        }

        if (highlighted == po) return;

        ClearHighlight();
        ApplyHighlight(po);
    }

    void UpdateChairGhost()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            if (ghostChair != null) ghostChair.SetActive(false);
            return;
        }

        Vector2Int gridPos = gridManager.GetGridPosition(hit.point);

        // if pointing at an existing chair, show orange ghost over it
        var po = FindPlaceableFromCollider(hit.collider);
        if (po != null && po.type == PlaceableType.Chair)
        {
            if (ghostChair == null)
            {
                ghostChair = Instantiate(chairPrefab);
            }
            ghostChair.SetActive(true);
            ghostChair.transform.position = gridManager.GetWorldPosition(po.placedGridPosition);
            SetGhostMaterial(ghostChair, new Color(1f, 0.5f, 0f, 0.6f));

            if (Input.GetMouseButtonDown(0))
            {
                // remove chair on left click
                gridManager.SetOccupiedArea(po.placedGridPosition, po.width, po.height, false);
                Destroy(po.gameObject);
            }

            return;
        }

        // if grid cell is empty and adjacent to a table, show yellow chair ghost and allow placement
        if (gridManager.IsInsideGrid(gridPos) && !gridManager.IsCellOccupied(gridPos) && IsAdjacentToTable(gridPos))
        {
            if (ghostChair == null)
                ghostChair = Instantiate(chairPrefab);

            ghostChair.SetActive(true);
            ghostChair.transform.position = gridManager.GetWorldPosition(gridPos);
            SetGhostMaterial(ghostChair, new Color(1f, 1f, 0f, 0.6f));

            if (Input.GetMouseButtonDown(0))
            {
                PlaceChairAt(gridPos);
            }

            return;
        }

        if (ghostChair != null)
            ghostChair.SetActive(false);
    }

    void ApplyHighlight(PlaceableObject po)
    {
        if (po == null) return;

        highlighted = po;

        // create outline material if needed
        if (outlineMaterial == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            outlineMaterial = new Material(sh ?? Shader.Find("Unlit/Color"));
            // use an orange outline and ensure we render only backfaces to get a rim (inverted hull)
            outlineMaterial.color = new Color(1f, 0.5f, 0f, 1f);
            outlineMaterial.renderQueue = 3000;
            // cull front faces so only the backfaces (expanded hull) are visible -> creates outline effect
            try { outlineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front); } catch { }
            // don't write depth so outline draws over the object edges
            try { outlineMaterial.SetInt("_ZWrite", 0); } catch { }
        }

        // create outline objects for MeshFilter + MeshRenderer
        foreach (var mf in po.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;

            var parent = mf.transform;
            GameObject go = new GameObject("__outline_" + mf.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * 1.02f;

            var of = go.AddComponent<MeshFilter>();
            of.sharedMesh = mf.sharedMesh;
            var or = go.AddComponent<MeshRenderer>();
            or.material = outlineMaterial;
            or.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            or.receiveShadows = false;

            highlightInstances.Add(go);
        }

        // support SkinnedMeshRenderer outlines
        foreach (var smr in po.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
        {
            if (smr.sharedMesh == null) continue;

            var parent = smr.transform;
            GameObject go = new GameObject("__outline_smr_" + smr.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * 1.02f;

            var of = go.AddComponent<MeshFilter>();
            of.sharedMesh = smr.sharedMesh;
            var or = go.AddComponent<MeshRenderer>();
            or.material = outlineMaterial;
            or.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            or.receiveShadows = false;

            highlightInstances.Add(go);
        }
    }

    void ClearHighlight()
    {
        if (highlighted == null && highlightInstances.Count == 0) return;

        foreach (var go in highlightInstances)
        {
            if (go != null)
                Destroy(go);
        }

        highlightInstances.Clear();
        highlighted = null;
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