using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RobotCodeEntry {
	[SerializeField] private string codeId;
	[SerializeField] private string robotBindingKey;
	[SerializeField] private string displayName;
	[SerializeField][TextArea(5, 20)] private string code;

	public string CodeId => codeId;
	public string RobotBindingKey => robotBindingKey;
	public string DisplayName => displayName;
	public string Code => code;

	public void Initialize(string newCodeId, string newRobotBindingKey, string newDisplayName) {
		codeId = newCodeId;
		robotBindingKey = newRobotBindingKey;
		displayName = newDisplayName;
		code = string.Empty;
	}

	public void UpdateContent(string newDisplayName, string newCode) {
		displayName = string.IsNullOrWhiteSpace(newDisplayName) ? "Yeni Kod" : newDisplayName.Trim();
		code = newCode ?? string.Empty;
	}
}

internal sealed class ArchivedRobotCodeEntry {
	public ArchivedRobotCodeEntry(string displayName, string code) {
		DisplayName = displayName;
		Code = code;
	}

	public string DisplayName { get; }
	public string Code { get; }
}

public class RobotCodeRegistry : MonoBehaviour {
	[SerializeField] private List<RobotCodeEntry> codes = new List<RobotCodeEntry>();
	[SerializeField] private string defaultCodePrefix = "Yeni Kod";

	private readonly List<ArchivedRobotCodeEntry> archivedCodes = new List<ArchivedRobotCodeEntry>();

	public event Action CodesChanged;

	public List<RobotCodeEntry> GetCodesForRobot(string robotBindingKey) {
		List<RobotCodeEntry> results = new List<RobotCodeEntry>();
		if (string.IsNullOrWhiteSpace(robotBindingKey)) {
			return results;
		}

		for (int i = 0; i < codes.Count; i++) {
			RobotCodeEntry entry = codes[i];
			if (entry == null) {
				continue;
			}

			if (entry.RobotBindingKey == robotBindingKey) {
				results.Add(entry);
			}
		}

		return results;
	}

	public RobotCodeEntry CreateCode(string robotBindingKey, string preferredName = null) {
		if (string.IsNullOrWhiteSpace(robotBindingKey)) {
			return null;
		}

		RobotCodeEntry entry = new RobotCodeEntry();
		entry.Initialize(Guid.NewGuid().ToString("N"), robotBindingKey, GenerateCodeName(robotBindingKey, preferredName));
		codes.Add(entry);
		CodesChanged?.Invoke();
		return entry;
	}

	public bool TryGetCode(string codeId, out RobotCodeEntry codeEntry) {
		for (int i = 0; i < codes.Count; i++) {
			RobotCodeEntry entry = codes[i];
			if (entry == null) {
				continue;
			}

			if (entry.CodeId == codeId) {
				codeEntry = entry;
				return true;
			}
		}

		codeEntry = null;
		return false;
	}

	public void SaveCode(string codeId, string displayName, string code) {
		RobotCodeEntry entry;
		if (!TryGetCode(codeId, out entry)) {
			return;
		}

		entry.UpdateContent(displayName, code);
		CodesChanged?.Invoke();
	}

	public void HandleSpawnPointPlaced(RobotSpawnPoint spawnPoint) {
		if (spawnPoint == null || archivedCodes.Count == 0) {
			return;
		}

		string robotBindingKey = spawnPoint.ProgramBindingKey;
		if (string.IsNullOrWhiteSpace(robotBindingKey) || GetCodesForRobot(robotBindingKey).Count > 0) {
			return;
		}

		for (int i = 0; i < archivedCodes.Count; i++) {
			ArchivedRobotCodeEntry archivedEntry = archivedCodes[i];
			if (archivedEntry == null) {
				continue;
			}

			RobotCodeEntry entry = new RobotCodeEntry();
			entry.Initialize(Guid.NewGuid().ToString("N"), robotBindingKey, GenerateCodeName(robotBindingKey, archivedEntry.DisplayName));
			entry.UpdateContent(archivedEntry.DisplayName, archivedEntry.Code);
			codes.Add(entry);
		}

		archivedCodes.Clear();
		CodesChanged?.Invoke();
	}

	public void HandleSpawnPointRemoved(RobotSpawnPoint spawnPoint) {
		archivedCodes.Clear();
		if (spawnPoint == null) {
			return;
		}

		string robotBindingKey = spawnPoint.ProgramBindingKey;
		if (string.IsNullOrWhiteSpace(robotBindingKey)) {
			return;
		}

		List<int> removedIndices = null;
		for (int i = 0; i < codes.Count; i++) {
			RobotCodeEntry entry = codes[i];
			if (entry == null || entry.RobotBindingKey != robotBindingKey) {
				continue;
			}

			archivedCodes.Add(new ArchivedRobotCodeEntry(entry.DisplayName, entry.Code));
			if (removedIndices == null) {
				removedIndices = new List<int>();
			}

			removedIndices.Add(i);
		}

		if (removedIndices == null) {
			return;
		}

		for (int i = removedIndices.Count - 1; i >= 0; i--) {
			codes.RemoveAt(removedIndices[i]);
		}

		CodesChanged?.Invoke();
	}

	public bool DeleteCode(string codeId) {
		for (int i = 0; i < codes.Count; i++) {
			RobotCodeEntry entry = codes[i];
			if (entry == null) {
				continue;
			}

			if (entry.CodeId == codeId) {
				codes.RemoveAt(i);
				CodesChanged?.Invoke();
				return true;
			}
		}

		return false;
	}

	private string GenerateCodeName(string robotBindingKey, string preferredName) {
		string baseName = string.IsNullOrWhiteSpace(preferredName) ? defaultCodePrefix : preferredName.Trim();
		HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < codes.Count; i++) {
			RobotCodeEntry entry = codes[i];
			if (entry == null || entry.RobotBindingKey != robotBindingKey) {
				continue;
			}

			usedNames.Add(entry.DisplayName);
		}

		if (!usedNames.Contains(baseName)) {
			return baseName;
		}

		int index = 2;
		while (usedNames.Contains(baseName + " " + index)) {
			index++;
		}

		return baseName + " " + index;
	}
}
