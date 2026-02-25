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

    private bool buildMode = false;
    private GameObject ghost;
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
            Debug.Log("Created ghost object for " + currentPrefab.name);
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

        // if we've used up the last item, clear current selection so it can't be placed anymore
        if (!inventory.HasItem(currentPrefab))
        {
            SetCurrentPrefab(null);
            inventory.GenerateUI(this);
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

        // add back to inventory and make it the current selection so player can place it again
        if (placed.originalPrefab != null)
        {
            inventory.AddItem(placed.originalPrefab);
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

    void SetGhostMaterial(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = new Color(0, 1, 0, 0.5f);
            r.material = m;
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