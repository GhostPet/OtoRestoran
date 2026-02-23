using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoTamamlama : MonoBehaviour {
	public TMP_InputField kodInput;
	public GameObject oneriPanel;
	public GameObject oneriButonPrefab;

	private readonly List<string> komutlar = new()
	{
		"yazdır(\"\")"
	};

	void Start() {
		kodInput.onValueChanged.AddListener(OnerileriKontrolEt);
		oneriPanel.SetActive(false);
	}

	void OnerileriKontrolEt(string metin) {
		string sonKelime = SonKelimeyiAl(metin);

		if (string.IsNullOrEmpty(sonKelime)) {
			oneriPanel.SetActive(false);
			return;
		}

		List<string> eslesenler = komutlar.FindAll(k => k.StartsWith(sonKelime));

		if (eslesenler.Count == 0) {
			oneriPanel.SetActive(false);
			return;
		}

		OnerileriOlustur(eslesenler);
	}

	string SonKelimeyiAl(string metin) {
		string[] parcalar = metin.Split(' ', '\n');
		return parcalar[^1];
	}

	void OnerileriOlustur(List<string> oneriler) {
		foreach (Transform child in oneriPanel.transform)
			Destroy(child.gameObject);

		foreach (string komut in oneriler) {
			GameObject btn = Instantiate(oneriButonPrefab, oneriPanel.transform);
			btn.GetComponentInChildren<TMP_Text>().text = komut;

			btn.GetComponent<Button>().onClick.AddListener(() => {
				KomutuTamamla(komut);
			});
		}

		oneriPanel.SetActive(true);
	}

	void KomutuTamamla(string komut) {
		string metin = kodInput.text;
		string sonKelime = SonKelimeyiAl(metin);

		int index = metin.LastIndexOf(sonKelime);
		if (index >= 0) {
			metin = metin.Remove(index, sonKelime.Length);
			metin = metin.Insert(index, komut);
		}

		kodInput.text = metin;
		kodInput.caretPosition = metin.Length;

		oneriPanel.SetActive(false);
	}
}
