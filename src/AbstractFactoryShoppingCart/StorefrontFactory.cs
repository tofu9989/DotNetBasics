using System.Globalization;

namespace AbstractFactoryShoppingCart;

public interface IShippingPolicy
{
    decimal Calculate(decimal subtotal);
}

public interface ITaxPolicy
{
    string Label { get; }

    decimal Calculate(decimal subtotal, decimal shipping);
}

public interface IMoneyFormatter
{
    string Format(decimal amount);
}

public interface IStorefrontFactory
{
    string Market { get; }

    IShippingPolicy CreateShippingPolicy();

    ITaxPolicy CreateTaxPolicy();

    IMoneyFormatter CreateMoneyFormatter();
}

public sealed class ThaiStorefrontFactory : IStorefrontFactory
{
    public string Market => "Thailand";

    public IShippingPolicy CreateShippingPolicy() => new ThaiShippingPolicy();

    public ITaxPolicy CreateTaxPolicy() => new ThaiTaxPolicy();

    public IMoneyFormatter CreateMoneyFormatter() => new ThaiMoneyFormatter();

    private sealed class ThaiShippingPolicy : IShippingPolicy
    {
        public decimal Calculate(decimal subtotal) => subtotal >= 1_500m ? 0m : 60m;
    }

    private sealed class ThaiTaxPolicy : ITaxPolicy
    {
        public string Label => "VAT 7%";

        public decimal Calculate(decimal subtotal, decimal shipping) => subtotal * 0.07m;
    }

    private sealed class ThaiMoneyFormatter : IMoneyFormatter
    {
        public string Format(decimal amount) => "THB " + amount.ToString("#,##0.00", CultureInfo.InvariantCulture);
    }
}

public sealed class UsStorefrontFactory : IStorefrontFactory
{
    public string Market => "United States";

    public IShippingPolicy CreateShippingPolicy() => new UsShippingPolicy();

    public ITaxPolicy CreateTaxPolicy() => new UsTaxPolicy();

    public IMoneyFormatter CreateMoneyFormatter() => new UsMoneyFormatter();

    private sealed class UsShippingPolicy : IShippingPolicy
    {
        public decimal Calculate(decimal subtotal) => subtotal >= 75m ? 0m : 8m;
    }

    private sealed class UsTaxPolicy : ITaxPolicy
    {
        public string Label => "Sales tax 8.25%";

        public decimal Calculate(decimal subtotal, decimal shipping) => (subtotal + shipping) * 0.0825m;
    }

    private sealed class UsMoneyFormatter : IMoneyFormatter
    {
        public string Format(decimal amount) => "USD " + amount.ToString("#,##0.00", CultureInfo.InvariantCulture);
    }
}
