using System.Globalization;
using SingletonShoppingCart;

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
    ShoppingCart sessionA = ShoppingCart.Instance;
    ShoppingCart sessionB = ShoppingCart.Instance;
    Check(ReferenceEquals(sessionA, sessionB), "sessionA and sessionB reference the same instance");

    sessionA.Add(new CartItem("KB-01", "Mechanical keyboard", 2490m, 1));
    sessionA.Add(new CartItem("MS-01", "Wireless mouse", 890m, 2));
    sessionB.Add(new CartItem("KB-01", "Mechanical keyboard", 2490m, 1));
    sessionB.Add(new CartItem("CB-01", "USB-C cable", 350m, 3));

    foreach (CartItem item in sessionB.Items)
    {
        Console.WriteLine($"{item.Sku} {item.Name} x{item.Quantity} @ {Money(item.UnitPrice)} = {Money(item.UnitPrice * item.Quantity)}");
    }

    Console.WriteLine($"Total: {Money(sessionB.Total)}");

    CartItem keyboard = sessionB.Items.First(item => item.Sku == "KB-01");
    Check(keyboard.Quantity == 2, "KB-01 quantity is 2 after merging both sessions");
    Check(sessionB.Total == 7810.00m, "total is 7810.00");

    Parallel.For(0, 1000, i =>
    {
        ShoppingCart cart = i % 2 == 0 ? sessionA : sessionB;
        cart.Add(new CartItem("CP-01", "Concurrency probe", 1m, 1));
    });

    CartItem probe = sessionB.Items.First(item => item.Sku == "CP-01");
    Console.WriteLine($"Concurrency probe: CP-01 x{probe.Quantity}, total {Money(sessionB.Total)}");
    Check(probe.Quantity == 1000, "CP-01 quantity is 1000 after concurrent adds");
    Check(sessionB.Total == 8810.00m, "total is 8810.00 after concurrent adds");

    Check(sessionA.Remove("CP-01"), "CP-01 line removed");
    Check(sessionB.Total == 7810.00m, "total is 7810.00 after removal");
    Check(sessionB.Items.Count == 3, "cart holds 3 lines after removal");

    Console.WriteLine("PASS: all checks passed");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"FAIL: {ex.Message}");
    return 1;
}
