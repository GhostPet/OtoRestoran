using TMPro;
using UnityEngine;

public class SaveSystemUI : MonoBehaviour {
	[Header("Pop-up Ayarları (İsim Kaydetme)")]
	public GameObject savePopupPanel;
	public TMP_InputField nameInputField;

	[Header("Liste Ayarları")]
	public GameObject savedListWindow;
	public Transform listContentParent;
	public GameObject savedItemPrefab;

	[Header("Kod Editörü Ayarları (YENİ)")]
	public GameObject codeEditorWindow; // Editör penceresinin ana objesi (Kapalıysa açmak için)
	public TMP_InputField mainCodeInputField; // Kodların yazıldığı o büyük metin kutusu

	public void OpenSavePopup() {
		savePopupPanel.SetActive(true);
	}

	public void CloseSavePopup() {
		savePopupPanel.SetActive(false);
		nameInputField.text = "";
	}

	public void OnConfirmSaveClicked() {
		string programName = nameInputField.text.Trim();

		if (!string.IsNullOrWhiteSpace(programName)) {
			GameObject newItem = Instantiate(savedItemPrefab, listContentParent, false);
			newItem.transform.localPosition = Vector3.zero;
			newItem.transform.localScale = Vector3.one;

			// Yeşil kutunun üzerindeki scripti bul
			SavedProgramItem savedItem = newItem.GetComponent<SavedProgramItem>();
			if (savedItem != null) {
				// YENİ: Büyük editördeki kodu al (Eğer boşsa standart bir metin koy)
				string currentCode = "";
				if (mainCodeInputField != null) {
					currentCode = mainCodeInputField.text;
				}

				// Hem ismi hem de güncel kodu kutunun hafızasına kaydet
				savedItem.Setup(programName, currentCode);
			}

			CloseSavePopup();
		} else {
			Debug.LogWarning("Kayıt ismi boş bırakılamaz!");
		}
	}

	public void OpenSavedListWindow() {
		savedListWindow.SetActive(true);
	}

	// YENİ: Çift tıklandığında bu fonksiyon çalışacak ve kodu editöre geri yükleyecek
	public void LoadProgramToEditor(string pName, string pCode) {
		// 1. Editör penceresi kapalıysa aç
		if (codeEditorWindow != null) {
			codeEditorWindow.SetActive(true);
		}

		// 2. Hafızadaki kodu büyük metin kutusuna yazdır
		if (mainCodeInputField != null) {
			mainCodeInputField.text = pCode;
		}

		Debug.Log("Editöre başarıyla yüklendi: " + pName);
	}

	// YENİ: Tek tıklandığında kodun çalışmasını tetikleyecek merkez (Şimdilik sadece mesaj veriyor)
	public void RunProgram(string pName, string pCode) {
		Debug.Log("ÇALIŞTIRILIYOR: " + pName + "\nKod İçeriği:\n" + pCode);
		// İleride robotun hareket komutlarını buraya bağlayacağız
	}
}