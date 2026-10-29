namespace SingletonShoppingCart;

public sealed class ShoppingCart
{
    private static readonly Lazy<ShoppingCart> LazyInstance = new(() => new ShoppingCart(), isThreadSafe: true);

    private readonly object _gate = new();
    private readonly Dictionary<string, CartItem> _lines = new(StringComparer.OrdinalIgnoreCase);

    private ShoppingCart()
    {
    }

    public static ShoppingCart Instance => LazyInstance.Value;

    public void Add(CartItem item)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.Quantity);

        lock (_gate)
        {
            if (_lines.TryGetValue(item.Sku, out CartItem? existing))
            {
                _lines[item.Sku] = existing with { Quantity = checked(existing.Quantity + item.Quantity) };
            }
            else
            {
                _lines[item.Sku] = item;
            }
        }
    }

    public bool Remove(string sku)
    {
        lock (_gate)
        {
            return _lines.Remove(sku);
        }
    }

    public decimal Total
    {
        get
        {
            lock (_gate)
            {
                decimal total = 0m;
                foreach (CartItem line in _lines.Values)
                {
                    total += line.UnitPrice * line.Quantity;
                }

                return total;
            }
        }
    }

    public IReadOnlyList<CartItem> Items
    {
        get
        {
            lock (_gate)
            {
                return _lines.Values.ToArray();
            }
        }
    }
}
