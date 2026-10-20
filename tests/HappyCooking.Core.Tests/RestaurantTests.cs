using HappyCooking.Core.Events;
using HappyCooking.Core.Systems;

namespace HappyCooking.Core.Tests;

public class RestaurantTests
{
    private static GameSession WithDishOnMenu(int onCounter = 0)
    {
        var g = TestGame.Create();
        g.Profile.BestStars["nasi_goreng"] = 3;
        if (onCounter > 0)
        {
            g.Profile.Counter["nasi_goreng"] = onCounter;
        }

        return g;
    }

    [Fact]
    public void Spawn_RequiresSomethingOnTheMenu()
    {
        var g = TestGame.Create();
        Assert.Empty(g.Restaurant.Menu);
        Assert.Null(g.Restaurant.TrySpawn());
        g.Restaurant.Update(60f);
        Assert.Empty(g.Restaurant.Customers);
    }

    [Fact]
    public void Spawn_OrdersFromMenu_FillsSeatsUpToLimit()
    {
        var g = WithDishOnMenu();
        var arrived = 0;
        g.Restaurant.CustomerArrived += _ => arrived++;
        for (var i = 0; i < RestaurantSystem.Seats; i++)
        {
            var c = g.Restaurant.TrySpawn();
            Assert.NotNull(c);
            Assert.Equal("nasi_goreng", c!.Order.Id);
            Assert.Equal(i, c.Seat);
            Assert.Equal(1f, c.PatienceLeft);
        }

        Assert.Null(g.Restaurant.TrySpawn());
        Assert.Equal(RestaurantSystem.Seats, arrived);
        Assert.Equal(RestaurantSystem.Seats, g.Restaurant.Customers.Select(c => c.Id).Distinct().Count());
    }

    [Fact]
    public void Update_SpawnsCustomersOverTime()
    {
        var g = WithDishOnMenu();
        g.Restaurant.Update(2.1f); // first guest arrives after 2 s
        Assert.Single(g.Restaurant.Customers);
    }

    [Fact]
    public void Serve_ConsumesCounter_PaysPriceAndTip_RaisesEvent()
    {
        var g = WithDishOnMenu(onCounter: 2);
        var c = g.Restaurant.TrySpawn()!;
        var coins = g.Economy.Coins;
        var served = new List<GameEvent>();
        g.Events.Subscribe(e =>
        {
            if (e.Kind == GameEventKind.CustomerServed)
            {
                served.Add(e);
            }
        });

        Assert.True(g.Restaurant.CanServe(c));
        var result = g.Restaurant.TryServe(c);

        Assert.NotNull(result);
        var price = Math.Max(5, c.Order.Coins / 2);
        Assert.Equal(price, result!.Value.Coins);
        Assert.Equal((int)MathF.Round(price * 0.5f * 1f * 1.1f), result.Value.Tip); // full patience, rating 1
        Assert.Equal(coins + price + result.Value.Tip, g.Economy.Coins);
        Assert.Equal(1, g.Profile.Counter["nasi_goreng"]);
        Assert.Empty(g.Restaurant.Customers);
        Assert.Equal(1, g.Profile.Stats.CustomersServed);
        Assert.Single(served);
        Assert.Null(g.Restaurant.TryServe(c)); // already gone
    }

    [Fact]
    public void Serve_LastDishRemovesCounterEntry()
    {
        var g = WithDishOnMenu(onCounter: 1);
        var c = g.Restaurant.TrySpawn()!;
        Assert.NotNull(g.Restaurant.TryServe(c));
        Assert.False(g.Profile.Counter.ContainsKey("nasi_goreng"));
    }

    [Fact]
    public void Serve_FailsWhenDishNotOnCounter()
    {
        var g = WithDishOnMenu();
        var c = g.Restaurant.TrySpawn()!;
        var coins = g.Economy.Coins;
        Assert.False(g.Restaurant.CanServe(c));
        Assert.Null(g.Restaurant.TryServe(c));
        Assert.Equal(coins, g.Economy.Coins);
        Assert.Contains(c, g.Restaurant.Customers);
    }

    [Fact]
    public void SlowerService_SmallerTip()
    {
        var g = WithDishOnMenu(onCounter: 2);
        var c = g.Restaurant.TrySpawn()!;
        g.Restaurant.Update(c.Patience * 0.9f);
        Assert.Contains(c, g.Restaurant.Customers);
        Assert.InRange(c.PatienceLeft, 0.05f, 0.15f);
        var tip = g.Restaurant.TryServe(c)!.Value.Tip;
        var price = Math.Max(5, c.Order.Coins / 2);
        Assert.True(tip < (int)MathF.Round(price * 0.5f * 1.1f));
    }

    [Fact]
    public void PatienceTimeout_RemovesCustomer_AndRaisesCustomerLeft()
    {
        var g = WithDishOnMenu();
        var c = g.Restaurant.TrySpawn()!;
        Customer? left = null;
        g.Restaurant.CustomerLeft += x => left = x;

        g.Restaurant.Update(c.Patience - 0.5f);
        Assert.Contains(c, g.Restaurant.Customers);
        g.Restaurant.Update(1f);

        Assert.Same(c, left);
        Assert.DoesNotContain(c, g.Restaurant.Customers);
        Assert.Equal(0, g.Profile.Stats.CustomersServed);
    }

    [Fact]
    public void Decorations_RaiseRating_AndPatience()
    {
        var g = WithDishOnMenu();
        g.Profile.Coins = 10_000;
        Assert.Equal(1, g.Restaurant.Rating);
        var basePatience = g.Restaurant.TrySpawn()!.Patience;

        var placed = 0;
        g.Events.Subscribe(e => placed += e.Kind == GameEventKind.DecorationPlaced ? 1 : 0);
        Assert.True(g.Restaurant.TryBuyDecoration("d_piano")); // rating 5
        Assert.False(g.Restaurant.TryBuyDecoration("d_piano")); // only once
        Assert.False(g.Restaurant.TryBuyDecoration("d_nope"));
        Assert.True(g.Restaurant.Owns("d_piano"));
        Assert.Equal(1, placed);
        Assert.Equal(2, g.Restaurant.Rating);

        foreach (var d in g.Content.Decorations)
        {
            g.Restaurant.TryBuyDecoration(d.Id);
        }

        Assert.Equal(5, g.Restaurant.Rating); // capped
        Assert.True(g.Restaurant.TrySpawn()!.Patience > basePatience);
    }

    [Fact]
    public void Decoration_RequiresCoins()
    {
        var g = TestGame.Create();
        g.Profile.Coins = 0;
        Assert.False(g.Restaurant.TryBuyDecoration("d_flower_pot"));
        Assert.Empty(g.Profile.Decorations);
    }

    [Fact]
    public void PatiencePet_MakesGuestsWaitLonger()
    {
        var g = WithDishOnMenu();
        var plain = g.Restaurant.TrySpawn()!.Patience;
        g.Profile.Level = 4;
        g.Profile.Coins = 1000;
        Assert.True(g.Pets.TryAdopt("luna"));
        Assert.True(g.Restaurant.TrySpawn()!.Patience > plain);
    }
}
