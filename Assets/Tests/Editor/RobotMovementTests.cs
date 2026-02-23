using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RobotMovementTests {
	// Test-dostu MonoBehaviour sahte robot; IRobot arayüzünü uygular.
	private class FakeRobotMono : MonoBehaviour, IRobot {
		public Vector3 Position => transform.position;
		public float MoveSpeed => 1f;

		private Vector3 _target;
		public bool IsMoving { get; private set; } = false;

		public Vector3 LastStartedTarget { get; private set; }

		public void MoveTo(Vector3 worldPosition) {
			transform.position = worldPosition;
			IsMoving = false;
		}

		public void StartMoveTo(Vector3 worldPosition) {
			_target = worldPosition;
			LastStartedTarget = worldPosition;
			IsMoving = true;
		}

		// Test sırasında çağrıp robotun hedefe ulaşmasını simüle et
		public void SimulateArrival() {
			transform.position = _target;
			IsMoving = false;
		}
	}

	[Test]
	public void Robot_Goes_Waits_Then_GoesAgain_UsingPrefabIfAvailable() {
		GameObject go = null;
		bool createdTemp = false;

#if UNITY_EDITOR
		// Projedeki 'Robot' adlı prefab'ı ara (case-insensitive)
		string[] guids = AssetDatabase.FindAssets("Robot t:Prefab");
		if (guids.Length == 0) guids = AssetDatabase.FindAssets("robot t:Prefab");

		if (guids.Length > 0) {
			string path = AssetDatabase.GUIDToAssetPath(guids[0]);
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab != null) {
				go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			}
		}
#endif
		// Prefab yoksa geçici GameObject oluştur
		if (go == null) {
			go = new GameObject("TestRobotGO");
			createdTemp = true;
		}

		// Test için deterministik kontrol sağlayacak FakeRobotMono ekle (varsa zaten eklenmemiştir)
		var fake = go.GetComponent<FakeRobotMono>();
		if (fake == null) fake = go.AddComponent<FakeRobotMono>();

		// Execution context'e atıyoruz
		CommandExecutionContext.CurrentRobot = fake;

		// 1) İlk hareket: move_to -> başlamalı (false) ve robot.StartMoveTo çağrılmalı
		var moveCmd1 = new MoveToCommand();
		moveCmd1.Reset();

		var target1 = new Vector3(1f, 0f, 2f);
		bool done1 = moveCmd1.Tick(target1);
		Assert.IsFalse(done1, "MoveTo should not complete immediately on first tick");
		Assert.AreEqual(target1, fake.LastStartedTarget);

		// Robot hareket ediyor gibi davran
		Assert.IsTrue(fake.IsMoving, "Robot should be moving after StartMoveTo");
		bool stillRunning = moveCmd1.Tick(target1);
		Assert.IsFalse(stillRunning, "MoveTo should return false while robot.IsMoving is true");

		// Varış simülasyonu ve tekrar tick
		fake.SimulateArrival();
		bool finished = moveCmd1.Tick(target1);
		Assert.IsTrue(finished, "MoveTo should complete after robot stops moving");

		// 2) Bekleme: WaitCommand ile kısa süre bekle (deterministik)
		var waitCmd = new WaitCommand();
		waitCmd.Reset();

		const float delta = 0.02f;
		WaitCommand.DeltaProvider = () => delta;

		// Başlat (başlangıç argümanı gerekli)
		waitCmd.Tick(0.1f);

		bool waitDone = false;
		int safety = 0;
		const int maxTicks = 100;
		while (!waitDone && safety < maxTicks) {
			waitDone = waitCmd.Tick(); // artık DeltaProvider kullanılıyor, argüman gerekli değil
			safety++;
		}

		WaitCommand.DeltaProvider = null;
		Assert.IsTrue(waitDone, "WaitCommand should complete within expected ticks");

		// 3) İkinci hareket: başka bir hedefe git
		var moveCmd2 = new MoveToCommand();
		moveCmd2.Reset();

		var target2 = new Vector3(-2f, 0f, 0.5f);
		bool started2 = moveCmd2.Tick(target2);
		Assert.IsFalse(started2, "Second MoveTo should not complete immediately");
		Assert.AreEqual(target2, fake.LastStartedTarget, "Robot should be commanded to second target");

		Assert.IsTrue(fake.IsMoving, "Robot should be moving toward second target");
		Assert.IsFalse(moveCmd2.Tick(target2));

		// Varışı simüle et
		fake.SimulateArrival();
		Assert.IsTrue(moveCmd2.Tick(target2), "Second MoveTo should complete after arrival");

		// Temizlik
		CommandExecutionContext.CurrentRobot = null;
#if UNITY_EDITOR
		if (go != null) Object.DestroyImmediate(go);
#else
		if (go != null) Object.DestroyImmediate(go);
#endif
	}
}