using System.Globalization;
using FactoryMethodShoppingCart;

static void Check(bool ok, string message)
{
    if (!ok)
    {
        throw new InvalidOperationException(message);
    }

    Console.WriteLine($"PASS: {message}");
}

static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

try
{
    Cart cart = new();

    CartLineFactory physical = new PhysicalCartLineFactory();
    CartLineFactory digital = new DigitalCartLineFactory();
    CartLineFactory giftCard = new GiftCardCartLineFactory();

    CartLine keyboard = cart.Add(physical, "KB-01", "Mechanical keyboard", 2490m, 1);
    cart.Add(physical, "MS-01", "Wireless mouse", 890m, 2);
    CartLine cable = cart.Add(physical, "CB-01", "USB-C cable", 350m, 1);
    CartLine license = cart.Add(digital, "DL-01", "IDE license", 1200m, 1);
    CartLine card = cart.Add(giftCard, "GC-01", "Store gift card", 500m, 1);

    foreach (CartLine line in cart.Lines)
    {
        Console.WriteLine(
            $"{line.Sku} {line.Name} x{line.Quantity} @ {Money(line.UnitPrice)} shipping {Money(line.Shipping)} = {Money(line.LineTotal + line.Shipping)}");
    }

    Console.WriteLine($"Total: {Money(cart.Total)}");

    Check(keyboard is PhysicalCartLine, "factory typed as CartLineFactory produced a PhysicalCartLine");
    Check(keyboard.Shipping == 0m, "keyboard ships free above the threshold");
    Check(physical.Create("BD-01", "Boundary item", 1_000m, 1).Shipping == 0m, "physical line ships free at exactly 1000");
    Check(cable.Shipping == 60m, "cable shipping is 60.00 below the free-shipping threshold");
    Check(license.Shipping == 0m, "digital license ships free");
    Check(card.Shipping == 0m, "gift card ships free");
    Check(cart.Total == 6380.00m, "total is 6380.00");
    Check(cart.Lines.Count == 5, "cart holds 5 lines");

    bool giftCardRejected = false;
    try
    {
        giftCard.Create("GC-02", "Bonus gift card", 500m, 2);
    }
    catch (ArgumentException)
    {
        giftCardRejected = true;
    }

    Check(giftCardRejected, "gift card factory rejects quantity 2");

    bool physicalRejected = false;
    try
    {
        physical.Create("KB-02", "Second keyboard", 2490m, 0);
    }
    catch (ArgumentOutOfRangeException)
    {
        physicalRejected = true;
    }

    Check(physicalRejected, "physical factory rejects quantity 0");

    Console.WriteLine("PASS: all checks passed");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"FAIL: {ex.Message}");
    return 1;
}
