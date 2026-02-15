using System;
using System.Collections.Generic;

public class Validator {
	/// <summary>
	/// Kod satırlarını validate eder
	/// </summary>
	/// <param name="lines">DSL kod satırları</param>
	/// <returns>Hata listesi, boşsa valid</returns>
	public List<ValidationError> Validate(List<string> lines) {
		var errors = new List<ValidationError>();

		for (int i = 0; i < lines.Count; i++) {
			string line = lines[i].Trim();
			if (string.IsNullOrEmpty(line))
				continue;

			try {
				// Komut ve argümanları ayır
				ParseLine(line, out string commandName, out string[] args);

				// Komut var mı?
				if (!BuiltinCommandRegistry.IsBuiltin(commandName)) {
					errors.Add(new UndefinedVariableError($"Undefined command '{commandName}'", i + 1));
					continue;
				}

				// Parametre sayısı kontrolü
				var command = BuiltinCommandRegistry.GetCommand(commandName);
				if (args.Length != command.ExpectedArgumentCount) {
					// InvalidArgumentCountError constructor expects (functionName, expected, actual, line)
					errors.Add(new InvalidArgumentCountError(
						commandName,
						command.ExpectedArgumentCount,
						args.Length,
						i + 1
					));
				}
			} catch (Exception ex) {
				errors.Add(new InvalidFunctionCallError($"Syntax error: {ex.Message}", i + 1));
			}
		}

		return errors;
	}

	/// <summary>
	/// Satırı command ve argümanlara ayırır
	/// Örnek: move_to(table1) -> commandName = move_to, args = ["table1"]
	/// </summary>
	private void ParseLine(string line, out string commandName, out string[] args) {
		int parenIndex = line.IndexOf('(');
		if (parenIndex < 0)
			throw new Exception("Missing opening parenthesis");

		commandName = line[..parenIndex].Trim();

		int closeIndex = line.IndexOf(')', parenIndex);
		if (closeIndex < 0)
			throw new Exception("Missing closing parenthesis");

		string argString = line.Substring(parenIndex + 1, closeIndex - parenIndex - 1).Trim();

		if (string.IsNullOrEmpty(argString))
			args = new string[0];
		else {
			args = argString.Split(',');
			for (int i = 0; i < args.Length; i++)
				args[i] = args[i].Trim();
		}
	}
}
