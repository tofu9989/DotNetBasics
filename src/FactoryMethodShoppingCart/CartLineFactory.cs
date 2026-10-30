namespace FactoryMethodShoppingCart;

public abstract class CartLineFactory
{
    public CartLine Create(string sku, string name, decimal unitPrice, int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitPrice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        return CreateLine(sku, name, unitPrice, quantity);
    }

    protected abstract CartLine CreateLine(string sku, string name, decimal unitPrice, int quantity);
}

public sealed class PhysicalCartLineFactory : CartLineFactory
{
    protected override CartLine CreateLine(string sku, string name, decimal unitPrice, int quantity) =>
        new PhysicalCartLine(sku, name, unitPrice, quantity);
}

public sealed class DigitalCartLineFactory : CartLineFactory
{
    protected override CartLine CreateLine(string sku, string name, decimal unitPrice, int quantity) =>
        new DigitalCartLine(sku, name, unitPrice, quantity);
}

public sealed class GiftCardCartLineFactory : CartLineFactory
{
    protected override CartLine CreateLine(string sku, string name, decimal unitPrice, int quantity)
    {
        if (quantity != 1)
        {
            throw new ArgumentException("Gift cards are limited to one per line.", nameof(quantity));
        }

        return new GiftCardCartLine(sku, name, unitPrice, quantity);
    }
}
