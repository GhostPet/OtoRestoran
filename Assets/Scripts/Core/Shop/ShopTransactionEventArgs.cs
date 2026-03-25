using System;

/// <summary>
/// Başarılı shop işlemlerinde ilgili ürün ve işlem özeti bu sınıf ile yayınlanır.
/// Böylece inventory, placement veya UI sistemleri gereken aksiyonu alabilir.
/// </summary>
[Serializable]
public sealed class ShopTransactionEventArgs {
	public ShopTransactionEventArgs(
		ShopProductDefinitionSO product,
		int quantity,
		int totalPrice,
		string tabId,
		ShopTransactionType transactionType) {
		Product = product;
		Quantity = quantity;
		TotalPrice = totalPrice;
		TabId = tabId;
		TransactionType = transactionType;
	}

	public ShopProductDefinitionSO Product { get; }

	public int Quantity { get; }

	public int TotalPrice { get; }

	public string TabId { get; }

	public ShopTransactionType TransactionType { get; }
}
