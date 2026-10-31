namespace AbstractFactoryShoppingCart;

public sealed class Checkout
{
    private readonly IStorefrontFactory _storefront;
    private readonly IShippingPolicy _shipping;
    private readonly ITaxPolicy _tax;
    private readonly IMoneyFormatter _money;
    private readonly List<CartLine> _lines = new();

    public Checkout(IStorefrontFactory storefront)
    {
        ArgumentNullException.ThrowIfNull(storefront);

        _storefront = storefront;
        _shipping = storefront.CreateShippingPolicy();
        _tax = storefront.CreateTaxPolicy();
        _money = storefront.CreateMoneyFormatter();
    }

    public string Market => _storefront.Market;

    public IReadOnlyList<CartLine> Lines => _lines;

    public decimal Subtotal => _lines.Sum(line => line.LineTotal);

    public decimal Shipping => _shipping.Calculate(Subtotal);

    public decimal Tax => decimal.Round(_tax.Calculate(Subtotal, Shipping), 2, MidpointRounding.AwayFromZero);

    public decimal Total => Subtotal + Shipping + Tax;

    public void Add(string sku, string name, decimal unitPrice, int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitPrice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        _lines.Add(new CartLine(sku, name, unitPrice, quantity));
    }

    public IReadOnlyList<string> RenderReceipt()
    {
        List<string> receipt = new();
        foreach (CartLine line in _lines)
        {
            receipt.Add($"{_money.Format(line.LineTotal)}  {line.Sku} {line.Name} x{line.Quantity}");
        }

        receipt.Add($"Subtotal: {_money.Format(Subtotal)}");
        receipt.Add($"Shipping: {_money.Format(Shipping)}");
        receipt.Add($"{_tax.Label}: {_money.Format(Tax)}");
        receipt.Add($"Total: {_money.Format(Total)}");
        return receipt;
    }
}
