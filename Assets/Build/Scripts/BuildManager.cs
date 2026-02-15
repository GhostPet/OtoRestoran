using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public GridManager gridManager;
    public Camera cam;
    public GameObject currentPrefab;
    public LayerMask groundLayer;

    private GameObject ghostObject;
    private Vector2Int lastGridPos;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        UpdateGhost();

        if (Input.GetMouseButtonDown(0))
        {
            TryPlace();
        }
    }

    void UpdateGhost()
    {
        if (currentPrefab == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
        {
            Vector2Int gridPos = gridManager.GetGridPosition(hit.point);
            lastGridPos = gridPos;

            Vector3 worldPos = gridManager.GetWorldPosition(gridPos);

            if (ghostObject == null)
            {
                ghostObject = Instantiate(currentPrefab);
                SetGhostMaterial(ghostObject);
            }

            ghostObject.SetActive(true);
            ghostObject.transform.position = worldPos;
        }
    }



    void TryPlace()
    {
        if (gridManager.IsCellOccupied(lastGridPos)) return;

        Vector3 worldPos = gridManager.GetWorldPosition(lastGridPos);
        Instantiate(currentPrefab, worldPos, Quaternion.identity);

        gridManager.SetOccupied(lastGridPos, true);
    }

    void SetGhostMaterial(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0, 1, 0, 0.5f);
            r.material = mat;
        }
    }


    void SetColor(Color color)
    {
        foreach (Renderer r in ghostObject.GetComponentsInChildren<Renderer>())
        {
            r.material.color = new Color(color.r, color.g, color.b, 0.5f);
        }
    }
}
