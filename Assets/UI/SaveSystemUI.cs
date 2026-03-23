using TMPro;
using UnityEngine;

public class SaveSystemUI : MonoBehaviour {
	[Header("Liste Ayarları")]
	public GameObject savedListWindow;
	public Transform listContentParent;
	public GameObject savedItemPrefab;

	[Header("Kod Editörü Ayarları")]
	public GameObject codeEditorWindow; // Editör penceresinin ana objesi (Kapalıysa açmak için)
	public TMP_InputField taskNameInputField; // Kod başlığının yazıldığı alan
	public TMP_InputField mainCodeInputField; // Kodların yazıldığı o büyük metin kutusu

	private GameCodeRunner gameCodeRunner;
	private RobotExecutor robotExecutor;
	private SavedProgramItem currentSavedItem;

	public void OnConfirmSaveClicked() {
		SaveCurrentProgram();
	}

	private void Awake() {
		// Eğer prefab içinden sahneye bağlı referanslar bağlanmadıysa, runtime'da arayıp buluruz.
		// Öncelik: SavedListAnchor component'i, sonra tag/name, sonra child isimlendirmesi.
		if (savedListWindow == null) {
			SavedListAnchor anchor = FindAnyObjectByType<SavedListAnchor>();
			if (anchor != null) {
				savedListWindow = anchor.gameObject;
				Debug.Log("SaveSystemUI: found savedListWindow via SavedListAnchor.");
			} else {
				GameObject byTag;
				try {
					byTag = GameObject.FindWithTag("SavedListWindow");
				} catch { byTag = null; }
				if (byTag != null) {
					savedListWindow = byTag;
					Debug.Log("SaveSystemUI: found savedListWindow via tag.");
				} else {
					GameObject byName = GameObject.Find("SavedListWindow");
					if (byName != null) {
						savedListWindow = byName;
						Debug.Log("SaveSystemUI: found savedListWindow via name.");
					}
				}
			}
		}

		if (listContentParent == null) {
			// Önce savedListWindow altında tipik Content adlarını kontrol et
			if (savedListWindow != null) {
				Transform content = savedListWindow.transform.Find("Content");
				if (content == null) {
					Transform viewport = savedListWindow.transform.Find("Viewport");
					if (viewport != null) content = viewport.Find("Content");
				}
				if (content != null) {
					listContentParent = content;
					Debug.Log("SaveSystemUI: found listContentParent under savedListWindow.");
				}
			}

			// Fallback: özel bir marker component ara
			if (listContentParent == null) {
				SavedListContentMarker marker = FindAnyObjectByType<SavedListContentMarker>();
				if (marker != null) {
					listContentParent = marker.transform;
					Debug.Log("SaveSystemUI: found listContentParent via SavedListContentMarker.");
				}
			}
		}
	}

	private void Start() {
		EnsureLinkedProgramEntry();
	}

	public void SaveCurrentProgram() {
		if (!EnsureLinkedProgramEntry()) {
			return;
		}

		string programName = GetProgramNameFromInput();
		string currentCode = GetCurrentCode();

		if (string.IsNullOrWhiteSpace(programName)) {
			Debug.LogWarning("Kayıt ismi boş bırakılamaz!");
			return;
		}

		currentSavedItem.Setup(programName, currentCode, this);
	}

	public void BindRobotContext(GameCodeRunner runner, RobotExecutor executor) {
		gameCodeRunner = runner;
		robotExecutor = executor;
	}

	public bool EnsureLinkedProgramEntry() {
		if (currentSavedItem != null) {
			return true;
		}

		if (savedItemPrefab == null) {
			Debug.LogWarning("SaveSystemUI: savedItemPrefab atanmadığı için kayıt yapılamadı.");
			return false;
		}

		if (listContentParent == null) {
			Debug.LogWarning("SaveSystemUI: listContentParent bulunamadığı için kayıt yapılamadı.");
			return false;
		}

		string programName = GetProgramNameFromInput();
		if (string.IsNullOrWhiteSpace(programName)) {
			programName = GenerateDefaultProgramName();
			if (taskNameInputField != null) {
				taskNameInputField.text = programName;
			}
		}

		string currentCode = GetCurrentCode();
		GameObject newItem = Instantiate(savedItemPrefab, listContentParent, false);
		newItem.transform.localPosition = Vector3.zero;
		newItem.transform.localScale = Vector3.one;

		if (!newItem.TryGetComponent<SavedProgramItem>(out currentSavedItem)) {
			Debug.LogWarning("SaveSystemUI: savedItemPrefab üzerinde SavedProgramItem bulunamadı.");
			Destroy(newItem);
			return false;
		}

		currentSavedItem.Setup(programName, currentCode, this);
		return true;
	}

	public void OpenSavedListWindow() {
		if (savedListWindow != null) {
			savedListWindow.SetActive(true);
		}
	}

	public void ShowEditorWindow() {
		gameObject.SetActive(true);
		if (codeEditorWindow != null) {
			codeEditorWindow.SetActive(true);
		}
	}

	// YENİ: Çift tıklandığında bu fonksiyon çalışacak ve kodu editöre geri yükleyecek
	public void LoadProgramToEditor(string pName, string pCode) {
		ShowEditorWindow();

		// 2. Hafızadaki kodu büyük metin kutusuna yazdır
		if (mainCodeInputField != null) {
			mainCodeInputField.text = pCode;
		}

		if (taskNameInputField != null) {
			taskNameInputField.text = pName;
		}

		Debug.Log("Editöre başarıyla yüklendi: " + pName);
	}

	public void ShowLinkedProgram() {
		ShowEditorWindow();
	}

	public void RunCurrentProgram() {
		EnsureLinkedProgramEntry();

		string programName = GetProgramNameFromInput();
		if (string.IsNullOrWhiteSpace(programName) && currentSavedItem != null) {
			programName = currentSavedItem.programName;
		}

		string currentCode = GetCurrentCode();
		if (currentSavedItem != null) {
			currentSavedItem.Setup(programName, currentCode, this);
		}

		if (gameCodeRunner != null && robotExecutor != null && gameCodeRunner.TryRunCode(robotExecutor, currentCode)) {
			Debug.Log("ÇALIŞTIRILIYOR: " + programName + " -> " + robotExecutor.name);
			return;
		}

		Debug.Log("ÇALIŞTIRILIYOR: " + programName + "\nKod İçeriği:\n" + currentCode);
	}

	// YENİ: Tek tıklandığında kodun çalışmasını tetikleyecek merkez (Şimdilik sadece mesaj veriyor)
	public void RunProgram(string pName, string pCode) {
		LoadProgramToEditor(pName, pCode);
		RunCurrentProgram();
	}

	private string GetProgramNameFromInput() {
		if (taskNameInputField == null) {
			return string.Empty;
		}

		return taskNameInputField.text.Trim();
	}

	private string GetCurrentCode() {
		if (mainCodeInputField == null) {
			return string.Empty;
		}

		return mainCodeInputField.text;
	}

	private string GenerateDefaultProgramName() {
		int index = 1;
		while (IsProgramNameTaken("Task" + index)) {
			index++;
		}

		return "Task" + index;
	}

	private bool IsProgramNameTaken(string candidateName) {
		if (listContentParent == null) {
			return false;
		}

		int childCount = listContentParent.childCount;
		for (int i = 0; i < childCount; i++) {
			Transform child = listContentParent.GetChild(i);
			if (child == null) {
				continue;
			}

			SavedProgramItem item = child.GetComponent<SavedProgramItem>();
			if (item == null || item == currentSavedItem) {
				continue;
			}

			if (item.programName == candidateName) {
				return true;
			}
		}

		return false;
	}
}