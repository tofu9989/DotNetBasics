namespace SingletonShoppingCart;

public sealed record CartItem(string Sku, string Name, decimal UnitPrice, int Quantity);
