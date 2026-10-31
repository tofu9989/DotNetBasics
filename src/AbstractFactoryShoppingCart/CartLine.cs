namespace AbstractFactoryShoppingCart;

public sealed record CartLine(string Sku, string Name, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
