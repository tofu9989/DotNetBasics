namespace FactoryMethodShoppingCart;

public abstract record CartLine(string Sku, string Name, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;

    public abstract decimal Shipping { get; }
}

public sealed record PhysicalCartLine(string Sku, string Name, decimal UnitPrice, int Quantity)
    : CartLine(Sku, Name, UnitPrice, Quantity)
{
    public override decimal Shipping => LineTotal >= 1_000m ? 0m : 60m;
}

public sealed record DigitalCartLine(string Sku, string Name, decimal UnitPrice, int Quantity)
    : CartLine(Sku, Name, UnitPrice, Quantity)
{
    public override decimal Shipping => 0m;
}

public sealed record GiftCardCartLine(string Sku, string Name, decimal UnitPrice, int Quantity)
    : CartLine(Sku, Name, UnitPrice, Quantity)
{
    public override decimal Shipping => 0m;
}
