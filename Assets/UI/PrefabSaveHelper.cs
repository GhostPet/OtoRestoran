using UnityEngine;

public class PrefabSaveHelper : MonoBehaviour {
	// Bu fonksiyonu Prefab'ın içindeki butona bağlayacağız
	public void OnSaveButtonClicked() {
		// Sahnede SaveSystemUI scriptini taşıyan objeyi (Canvas_Main) kod ile bul
		SaveSystemUI saveManager = FindAnyObjectByType<SaveSystemUI>();

		if (saveManager != null) {
			// Bulduysa, o scriptteki Pop-up açma fonksiyonunu çalıştır
			saveManager.OpenSavePopup();
		} else {
			Debug.LogError("Sahnede SaveSystemUI bulunamadı! Canvas_Main'de ekli olduğundan emin olun.");
		}
	}
}