using UnityEngine;

/// <summary>
/// Aynı buton ile shop penceresini açıp kapatmak için kullanılan küçük yardımcı script.
/// Buton objesine eklenir ve OnClick üzerinden ToggleWindow çağrılır.
/// </summary>
public class ShopWindowToggleButton : MonoBehaviour
{
    [SerializeField] private GameObject targetWindow;
    [SerializeField] private ShopUIController shopUIController;

    public void ToggleWindow()
    {
        if (targetWindow == null)
        {
            return;
        }

        bool willOpen = !targetWindow.activeSelf;
        targetWindow.SetActive(willOpen);

        if (!willOpen)
        {
            return;
        }

        if (shopUIController == null)
        {
            shopUIController = targetWindow.GetComponent<ShopUIController>();
        }

        if (shopUIController != null)
        {
            shopUIController.RebuildAllUI();
        }
    }
}
