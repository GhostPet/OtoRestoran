using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class SavedProgramItem : MonoBehaviour, IPointerClickHandler
{
    [Header("Program Verileri")]
    public string programName;
    [TextArea(3, 10)] // Inspector'da kodun tamamını rahatça görmek için
    public string programCode; 

    private float doubleClickDelay = 0.3f;

    public void Setup(string pName, string pCode)
    {
        programName = pName;
        programCode = pCode;
        
        TMP_Text itemText = GetComponentInChildren<TMP_Text>();
        if (itemText != null)
        {
            itemText.text = programName;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 1)
        {
            Invoke("SingleClickAction", doubleClickDelay);
        }
        else if (eventData.clickCount == 2)
        {
            CancelInvoke("SingleClickAction");
            DoubleClickAction();
        }
    }

    private void SingleClickAction()
    {
        // 1 KERE TIKLANDI: Sistemi çalıştır
        SaveSystemUI saveSystem = FindObjectOfType<SaveSystemUI>();
        if (saveSystem != null)
        {
            saveSystem.RunProgram(programName, programCode);
        }
    }

    private void DoubleClickAction()
    {
        // 2 KERE TIKLANDI: Kodu düzenlemek için editöre geri yükle
        SaveSystemUI saveSystem = FindObjectOfType<SaveSystemUI>();
        if (saveSystem != null)
        {
            saveSystem.LoadProgramToEditor(programName, programCode);
        }
    }
}