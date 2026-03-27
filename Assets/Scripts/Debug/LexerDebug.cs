using System.Text;
using UnityEngine;

public class LexerDebug : MonoBehaviour {
	[TextArea(5, 20)]
	public string testCode = @"
def main():
	while has_order():
		table = find_dirty_table()
		if table:
			move_to(table)
			clean(table)
		else:
			wait(1)
";

	void Start() {
		RunLexer();
	}

	[ContextMenu("Run Lexer")]
	public void RunLexer() {
		try {
			var lexer = new Lexer(testCode);
			var tokens = lexer.Tokenize();

			var sb = new StringBuilder();
			foreach (var token in tokens) {
				sb.AppendLine(token.ToString());
			}

			Debug.Log(sb.ToString());
		} catch (LexerException ex) {
			Debug.LogError(ex.Message);
		}
	}
}
