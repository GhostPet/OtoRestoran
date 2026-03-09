using System;

/// <summary>
/// Eski yazım hatalı sınıf adıyla eklenmiş component'lerin sahnede bozulmaması için tutuluyor.
/// Yeni kodda doğrudan InventoryManager kullanılmalıdır.
/// </summary>
[Obsolete("Bu sınıf geriye dönük uyumluluk içindir. Yeni kullanımda InventoryManager tercih edilmelidir.")]
public class InvenyoryManager : InventoryManager
{
}
