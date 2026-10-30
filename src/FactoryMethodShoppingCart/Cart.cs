namespace FactoryMethodShoppingCart;

public sealed class Cart
{
    private readonly List<CartLine> _lines = new();

    public IReadOnlyList<CartLine> Lines => _lines;

    public CartLine Add(CartLineFactory factory, string sku, string name, decimal unitPrice, int quantity)
    {
        CartLine line = factory.Create(sku, name, unitPrice, quantity);
        _lines.Add(line);
        return line;
    }

    public decimal Total => _lines.Sum(line => line.LineTotal + line.Shipping);
}
