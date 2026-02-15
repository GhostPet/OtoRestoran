using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeTest : MonoBehaviour {
	public TMP_InputField codeInput;
	public Button runButton;

	private void Awake() {
		runButton.onClick.AddListener(Run);
	}


	void Run() {
		string code;
		if (codeInput != null && !string.IsNullOrWhiteSpace(codeInput.text)) {
			code = codeInput.text;
		} else {
			code = string.Empty;
		}


		Debug.Log("==== LEXER TEST ====");
		Lexer lexer = new(code);
		List<Token> tokens = lexer.Tokenize();

		/*foreach (var t in tokens) {
			Debug.Log($"Token: {t.Type} | Lexeme: '{t.Lexeme}' | Line: {t.Line}");
		}*/

		Debug.Log("==== PARSER TEST ====");
		Parser parser = new(tokens);

		// parseResult yerine doğrudan function listesi alıyoruz
		List<FunctionDefNode> functions = parser.Parse();

		/*if (functions == null || functions.Count == 0) {
			Debug.LogError("Parser Error: AST oluşturulamadı veya fonksiyon bulunamadı.");
			return;
		}*/

		//Debug.Log($"Parser produced {functions.Count} functions.");

		/*for (int i = 0; i < functions.Count; i++) {
			Debug.Log($"Function {i + 1}: {functions[i]}");
		}*/

		// Basit interpreter ile çalıştırma — komutlar çağrılmadan önce ilgili satır burada ayarlanır
		var interpreterObj = new GameObject("AstInterpreter");
		var interpreter = interpreterObj.AddComponent<AstInterpreter>();

		// temiz değişken tablosu ve çalıştır
		CommandExecutionContext.ClearVariables();
		interpreter.StartExecution(functions, "main");
	}
}
