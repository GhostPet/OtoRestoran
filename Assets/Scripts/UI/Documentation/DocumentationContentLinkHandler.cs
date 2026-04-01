using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DocumentationContentLinkHandler : MonoBehaviour, IPointerClickHandler {
	private TMP_Text targetText;
	private DocumentationWindowUI owner;

	public void Initialize(TMP_Text textComponent, DocumentationWindowUI window) {
		targetText = textComponent;
		owner = window;
	}

	public void OnPointerClick(PointerEventData eventData) {
		if (targetText == null || owner == null || eventData == null) {
			return;
		}

		int linkIndex = TMP_TextUtilities.FindIntersectingLink(targetText, eventData.position, eventData.pressEventCamera);
		if (linkIndex < 0) {
			return;
		}

		TMP_LinkInfo linkInfo = targetText.textInfo.linkInfo[linkIndex];
		owner.HandleContentLink(linkInfo.GetLinkID());
	}
}