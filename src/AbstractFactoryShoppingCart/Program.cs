using AbstractFactoryShoppingCart;

static void Check(bool ok, string message)
{
    if (!ok)
    {
        throw new InvalidOperationException(message);
    }

    Console.WriteLine($"PASS: {message}");
}

static Checkout Build(IStorefrontFactory factory)
{
    Checkout checkout = new(factory);
    checkout.Add("KB-01", "Mechanical keyboard", 2490m, 1);
    checkout.Add("MS-01", "Wireless mouse", 890m, 1);
    checkout.Add("CB-01", "USB-C cable", 350m, 2);
    return checkout;
}

try
{
    Checkout thai = Build(new ThaiStorefrontFactory());
    Checkout us = Build(new UsStorefrontFactory());

    foreach (Checkout checkout in new[] { thai, us })
    {
        Console.WriteLine($"== {checkout.Market} ==");
        foreach (string line in checkout.RenderReceipt())
        {
            Console.WriteLine(line);
        }
    }

    Check(thai.Subtotal == 4080m, "Thai subtotal is 4080.00");
    Check(us.Subtotal == 4080m, "US subtotal is 4080.00");
    Check(thai.Shipping == 0m, "Thai shipping is free on a 4080.00 cart");
    Check(us.Shipping == 0m, "US shipping is free on a 4080.00 cart");
    Check(thai.Tax == 285.60m, "Thai tax is 285.60");
    Check(us.Tax == 336.60m, "US tax is 336.60");
    Check(thai.Total == 4365.60m, "Thai total is 4365.60");
    Check(us.Total == 4416.60m, "US total is 4416.60");

    string thaiReceipt = string.Join("\n", thai.RenderReceipt());
    string usReceipt = string.Join("\n", us.RenderReceipt());
    Check(thaiReceipt.Contains("THB") && !thaiReceipt.Contains("USD"), "Thai receipt formats money as THB only");
    Check(usReceipt.Contains("USD") && !usReceipt.Contains("THB"), "US receipt formats money as USD only");
    Check(thaiReceipt.Contains("VAT 7%"), "Thai receipt labels VAT 7%");
    Check(usReceipt.Contains("Sales tax 8.25%"), "US receipt labels Sales tax 8.25%");

    Checkout thaiSingle = new(new ThaiStorefrontFactory());
    thaiSingle.Add("CB-01", "USB-C cable", 350m, 1);
    Checkout usSmall = new(new UsStorefrontFactory());
    usSmall.Add("ST-01", "Sticker pack", 50m, 1);

    Check(thaiSingle.Shipping == 60m, "Thai single-cable shipping is 60.00");
    Check(thaiSingle.Tax == 24.50m, "Thai single-cable tax is 24.50");
    Check(thaiSingle.Total == 434.50m, "Thai single-cable total is 434.50");
    Check(usSmall.Shipping == 8m, "US below-threshold shipping is 8.00");
    Check(usSmall.Tax == 4.79m, "US below-threshold tax rounds 4.785 away from zero to 4.79");
    Check(usSmall.Total == 62.79m, "US below-threshold total is 62.79");

    IShippingPolicy thaiShipping = new ThaiStorefrontFactory().CreateShippingPolicy();
    IShippingPolicy usShipping = new UsStorefrontFactory().CreateShippingPolicy();
    Check(thaiShipping.Calculate(1499.99m) == 60m, "Thai shipping is 60.00 at 1499.99");
    Check(thaiShipping.Calculate(1500m) == 0m, "Thai shipping is free at exactly 1500");
    Check(usShipping.Calculate(74.99m) == 8m, "US shipping is 8.00 at 74.99");
    Check(usShipping.Calculate(75m) == 0m, "US shipping is free at exactly 75");

    bool quantityRejected = false;
    try
    {
        thai.Add("KB-02", "Second keyboard", 2490m, 0);
    }
    catch (ArgumentOutOfRangeException)
    {
        quantityRejected = true;
    }

    Check(quantityRejected, "Checkout.Add rejects quantity 0");

    bool blankNameRejected = false;
    try
    {
        thai.Add("KB-03", "   ", 2490m, 1);
    }
    catch (ArgumentException)
    {
        blankNameRejected = true;
    }

    Check(blankNameRejected, "Checkout.Add rejects a blank name");

    Console.WriteLine("PASS: all checks passed");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"FAIL: {ex.Message}");
    return 1;
}
