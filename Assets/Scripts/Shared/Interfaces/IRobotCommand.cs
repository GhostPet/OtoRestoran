public interface IRobotCommand {
	/// <summary>
	/// Komutun beklediği argüman sayısı
	/// </summary>
	int ExpectedArgumentCount { get; }

	/// <summary>
	/// Komutun Tick tabanlı çalıştırılması
	/// true → komut tamamlandı
	/// false → komut hala devam ediyor
	/// </summary>
	bool Tick(params object[] args);

	/// <summary>
	/// Komutu resetler, tekrar kullanılabilir hale getirir
	/// </summary>
	void Reset();
}
