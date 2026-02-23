using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BuiltinCommandTests {
	[Test]
	public void PrintCommand_PrintsVariousValues() {
		var cmd = new PrintCommand();
		CommandExecutionContext.CurrentLine = 42;
		// Try string
		Assert.IsTrue(cmd.Tick("hello"));
		// Try number
		Assert.IsTrue(cmd.Tick(3f));
		// Try list
		var list = new List<object> { 1f, 2f, 3f };
		Assert.IsTrue(cmd.Tick(list));
		// Try nested list
		var nested = new List<object> { new List<object> { 1f, 2f }, 3f };
		Assert.IsTrue(cmd.Tick(nested));
	}

	[Test]
	public void WaitCommand_WaitsAndCompletes() {
		var cmd = new WaitCommand();
		cmd.Reset();

		// Başlatmak için bir kez çağırmak zorunlu değil (kod içinde ilk tick başlangıcı ayarlar),
		// bu yüzden ilk çağrının dönüş değerine güvenmiyoruz ve deterministik bir loop ile tamamlanmayı bekliyoruz.
		bool done = false;
		int safety = 0;
		const float delta = 0.02f;
		const int maxTicks = 200; // yeterli büyük bir güvenlik sınırı

		// Simüle edilecek toplam bekleme süresi: 0.2s
		// İlk olarak komutu başlatıyoruz (set timeRemaining)
		cmd.Tick(0.2f);

		while (!done && safety < maxTicks) {
			done = cmd.Tick(delta);
			safety++;
		}

		Assert.IsTrue(done, "WaitCommand did not complete within expected ticks");
		// reset sonrası başlangıç durumuna dönsün
		cmd.Reset();
	}

	[Test]
	public void DropAndCleanServe_Patterns() {
		var drop = new DropCommand();
		drop.Reset();
		Assert.IsFalse(drop.Tick("item"));
		// second call should return true
		Assert.IsTrue(drop.Tick("item"));

		var clean = new CleanTableCommand();
		clean.Reset();
		Assert.IsFalse(clean.Tick("t1"));
		Assert.IsTrue(clean.Tick("t1"));

		var serve = new ServeOrderCommand();
		serve.Reset();
		Assert.IsFalse(serve.Tick("o1"));
		Assert.IsTrue(serve.Tick("o1"));
	}
}
