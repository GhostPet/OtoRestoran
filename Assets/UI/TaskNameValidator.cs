using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class TaskNameValidator : MonoBehaviour {
	[SerializeField] private TMP_InputField input;

	[Header("Rules")]
	[SerializeField] private bool blockLeadingSpecial = true;
	[SerializeField] private bool blockDoubleSpecial = true;

	void Awake() {
		if (input == null)
			input = GetComponent<TMP_InputField>();

		input.onValidateInput += ValidateChar;
	}

	private char ValidateChar(string text, int charIndex, char addedChar) {
		// Harf veya sayı (Unicode → Türkçe dahil)
		if (char.IsLetterOrDigit(addedChar))
			return addedChar;

		// İzin verilen özel karakterler
		if (addedChar == '_' || addedChar == '-') {
			// Başta _ veya - olmasın
			if (blockLeadingSpecial && charIndex == 0)
				return '\0';

			// Arka arkaya __ veya -- olmasın
			if (blockDoubleSpecial && text.Length > 0) {
				char lastChar = text[^1];

				if ((lastChar == '_' && addedChar == '_') ||
					(lastChar == '-' && addedChar == '-'))
					return '\0';
			}

			return addedChar;
		}

		// Diğer tüm karakterleri reddet
		return '\0';
	}
}