using UnityEngine;

public class PrefabSaveHelper : MonoBehaviour {
	// Bu fonksiyonu Prefab'ın içindeki butona bağlayacağız
	public void OnSaveButtonClicked() {
		SaveSystemUI saveManager = GetComponentInParent<SaveSystemUI>();

		if (saveManager != null) {
			saveManager.SaveCurrentProgram();
		} else {
			Debug.LogError("Kaydet butonunun parent zincirinde SaveSystemUI bulunamadı!");
		}
	}
}