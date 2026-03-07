#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public static class SetReadWrite {
	[MenuItem("Tools/Set Read/Write Enabled for Selected Models")]
	static void EnableReadWriteForSelected() {
		foreach (var obj in Selection.objects) {
			var path = AssetDatabase.GetAssetPath(obj);
			if (string.IsNullOrEmpty(path)) continue;

			var importer = AssetImporter.GetAtPath(path) as ModelImporter;
			if (importer != null) {
				importer.isReadable = true;
				importer.SaveAndReimport();
				Debug.Log($"Read/Write enabled: {path}");
			}
		}
	}

	[MenuItem("Tools/Set Read/Write Enabled for Selected Models", true)]
	static bool ValidateEnableReadWriteForSelected() {
		return Selection.objects.Length > 0;
	}
}
#endif