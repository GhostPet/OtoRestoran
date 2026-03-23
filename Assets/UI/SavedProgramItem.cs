using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SavedProgramItem : MonoBehaviour, IPointerClickHandler {
	[Header("Program Verileri")]
	public string programName;
	[TextArea(3, 10)] // Inspector'da kodun tamamını rahatça görmek için
	public string programCode;

	private SaveSystemUI owner;

	public void Setup(string pName, string pCode) {
		Setup(pName, pCode, null);
	}

	public void Setup(string pName, string pCode, SaveSystemUI saveSystem) {
		programName = pName;
		programCode = pCode;
		owner = saveSystem;

		TMP_Text itemText = GetComponentInChildren<TMP_Text>();
		if (itemText != null) {
			itemText.text = programName;
		}
	}

	public void OnPointerClick(PointerEventData eventData) {
		if (eventData.clickCount == 2) {
			DoubleClickAction();
		}
	}

	private void DoubleClickAction() {
		// 2 KERE TIKLANDI: Bağlı editör penceresini tekrar aç
		SaveSystemUI saveSystem = ResolveOwner();
		if (saveSystem != null) {
			saveSystem.ShowLinkedProgram();
		}
	}

	public void OnRunButtonClicked() {
		SaveSystemUI saveSystem = ResolveOwner();
		if (saveSystem != null) {
			saveSystem.RunCurrentProgram();
		}
	}

	private SaveSystemUI ResolveOwner() {
		if (owner != null) {
			return owner;
		}

		owner = GetComponentInParent<SaveSystemUI>();
		return owner;
	}
}