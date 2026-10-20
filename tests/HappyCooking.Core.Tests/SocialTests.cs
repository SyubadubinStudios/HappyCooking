using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.Systems;

namespace HappyCooking.Core.Tests;

public class SocialTests
{
    private static (GameSession Alice, GameSession Bob) TwoPlayers()
    {
        var alice = TestGame.Create(seed: 1, name: "Alice");
        var bob = TestGame.Create(seed: 2, name: "Bob");
        Assert.NotEqual(alice.Profile.PlayerCode, bob.Profile.PlayerCode);
        return (alice, bob);
    }

    [Fact]
    public void PlayerCodes_AreValid_AndDeterministicPerSeed()
    {
        for (var seed = 0; seed < 200; seed++)
        {
            var code = SocialSystem.NewPlayerCode(new GameRandom(seed));
            Assert.True(SocialSystem.IsValidPlayerCode(code), code);
            Assert.Equal(code, SocialSystem.NewPlayerCode(new GameRandom(seed)));
            Assert.DoesNotContain('I', code);
            Assert.DoesNotContain('L', code);
            Assert.DoesNotContain('O', code);
            Assert.DoesNotContain('U', code);
        }

        var g = TestGame.Create();
        Assert.True(SocialSystem.IsValidPlayerCode(g.Social.PlayerCode));
    }

    [Fact]
    public void ExistingPlayerCode_IsKept()
    {
        var clock = new ManualClock(TestGame.Noon);
        var profile = GameSession.NewProfile(TestGame.Content, "Kept", clock);
        profile.PlayerCode = "ABCD2345";
        var g = new GameSession(TestGame.Content, profile, clock, 3);
        Assert.Equal("ABCD2345", g.Social.PlayerCode);
    }

    [Theory]
    [InlineData("abcd", false)]
    [InlineData("ABCD234", false)]
    [InlineData("ABCD23456", false)]
    [InlineData("ABCD234U", false)]
    [InlineData("abcd2345", false)] // not normalized
    [InlineData("ABCD2345", true)]
    public void IsValidPlayerCode_Cases(string code, bool valid) => Assert.Equal(valid, SocialSystem.IsValidPlayerCode(code));

    [Theory]
    [InlineData("o1l-i", "011-1")]
    [InlineData("  hc1-abcd ", "HC1-ABCD")]
    [InlineData("OIL", "011")]
    [InlineData("a b*c!u", "ABC")] // spaces, symbols and U are dropped
    public void NormalizeCode_MapsLookAlikes(string input, string expected) =>
        Assert.Equal(expected, SocialSystem.NormalizeCode(input));

    [Fact]
    public void AddFriend_Validation()
    {
        var (alice, bob) = TwoPlayers();
        var bobCode = bob.Profile.PlayerCode;

        Assert.False(alice.Social.TryAddFriend("XYZ", "Bob")); // too short
        Assert.False(alice.Social.TryAddFriend(alice.Profile.PlayerCode, "Me")); // self
        Assert.False(alice.Social.TryAddFriend(bobCode, "   ")); // empty name
        Assert.False(alice.Social.TryAddFriend(bobCode, new string('x', 21))); // name too long

        Assert.True(alice.Social.TryAddFriend(bobCode.ToLowerInvariant(), "  Bob  ")); // normalized
        var friend = Assert.Single(alice.Social.Friends);
        Assert.Equal(bobCode, friend.Code);
        Assert.Equal("Bob", friend.Name);
        Assert.Equal(TestGame.Noon, friend.AddedUtc);

        Assert.False(alice.Social.TryAddFriend(bobCode, "Bob again")); // duplicate
        Assert.True(alice.Social.RemoveFriend(bobCode));
        Assert.False(alice.Social.RemoveFriend(bobCode));
        Assert.Empty(alice.Social.Friends);
    }

    [Fact]
    public void Gift_EncodeDecode_RoundTrips()
    {
        var g = TestGame.Create();
        var gift = new Gift("ABCD2345", "WXYZ6789", GiftKind.Ingredient, "rice", 7, "N0NCE");
        var code = SocialSystem.Encode(gift);
        Assert.StartsWith("HC1-", code);
        Assert.Equal(gift, g.Social.Decode(code));
        Assert.Equal(gift, g.Social.Decode(code.ToLowerInvariant())); // typed in lower case
        Assert.Equal(gift, g.Social.Decode(code.Replace("-", " ", StringComparison.Ordinal))); // dashes optional
    }

    private static readonly Gift s_coinGift = new("ABCD2345", "WXYZ6789", GiftKind.Coins, "coins", 50, "N0NCE");

    private static IEnumerable<(int Index, string Code)> SingleCharTampers(string code)
    {
        for (var i = 4; i < code.Length; i++)
        {
            if (code[i] != '-')
            {
                var replacement = code[i] == 'A' ? 'B' : 'A';
                yield return (i, string.Concat(code.AsSpan(0, i), replacement.ToString(), code.AsSpan(i + 1)));
            }
        }
    }

    [Fact]
    public void Gift_TamperedOrGarbage_NeverDecodesToADifferentGift()
    {
        var g = TestGame.Create();
        var code = SocialSystem.Encode(s_coinGift);

        // Change every body character in turn to a different alphabet character. The checksum must
        // reject it, or (see the skipped padding-bit test below) it may only decode to the very same gift.
        var rejected = 0;
        var total = 0;
        foreach (var (i, tampered) in SingleCharTampers(code))
        {
            total++;
            var decoded = g.Social.Decode(tampered);
            Assert.True(decoded is null || decoded == s_coinGift, $"tampered at {i} gave a different gift: {tampered}");
            rejected += decoded is null ? 1 : 0;
        }

        Assert.True(rejected >= total - 1, $"only {rejected} of {total} tampered codes were rejected");

        Assert.Null(g.Social.Decode(""));
        Assert.Null(g.Social.Decode("HC1-"));
        Assert.Null(g.Social.Decode("XYZ-ABCD-EFGH-JKMN"));
        Assert.Null(g.Social.Decode(code[..^2]));
    }

    [Fact]
    public void Gift_TamperedAnywhere_IsInvalid()
    {
        var g = TestGame.Create();
        foreach (var (i, tampered) in SingleCharTampers(SocialSystem.Encode(s_coinGift)))
        {
            Assert.True(g.Social.Decode(tampered) is null, $"tampered at {i}: {tampered}");
        }
    }

    [Fact]
    public void Gift_OverLimitsOrUnknownIngredient_IsInvalid()
    {
        var g = TestGame.Create();
        Assert.Null(g.Social.Decode(SocialSystem.Encode(new Gift("ABCD2345", "WXYZ6789", GiftKind.Coins, "coins", SocialSystem.MaxGiftCoins + 1, "N0NCE"))));
        Assert.Null(g.Social.Decode(SocialSystem.Encode(new Gift("ABCD2345", "WXYZ6789", GiftKind.Ingredient, "rice", SocialSystem.MaxGiftIngredients + 1, "N0NCE"))));
        Assert.Null(g.Social.Decode(SocialSystem.Encode(new Gift("ABCD2345", "WXYZ6789", GiftKind.Ingredient, "gold_bar", 1, "N0NCE"))));
        Assert.Null(g.Social.Decode(SocialSystem.Encode(new Gift("ABCD2345", "WXYZ6789", GiftKind.Coins, "coins", 0, "N0NCE"))));
    }

    [Fact]
    public void CoinGift_FullFlow_RedeemOnceOnly()
    {
        var (alice, bob) = TwoPlayers();
        var aliceCoins = alice.Economy.Coins;
        var bobCoins = bob.Economy.Coins;
        var sent = 0;
        alice.Events.Subscribe(e => sent += e.Kind == GameEventKind.GiftSent ? 1 : 0);

        var code = alice.Social.TryCreateGift(bob.Profile.PlayerCode, GiftKind.Coins, "whatever", 50);

        Assert.NotNull(code);
        Assert.Equal(aliceCoins - 50, alice.Economy.Coins);
        Assert.Equal(1, alice.Profile.Stats.GiftsSent);
        Assert.Equal(1, sent);
        Assert.True(alice.Achievements.IsUnlocked("a_gift_1"));

        Assert.Equal(RedeemError.None, bob.Social.TryRedeem(code!, out var gift));
        Assert.Equal(GiftKind.Coins, gift!.Kind);
        Assert.Equal("coins", gift.Item);
        Assert.Equal(alice.Profile.PlayerCode, gift.From);
        Assert.Equal(bobCoins + 50, bob.Economy.Coins);

        Assert.Equal(RedeemError.AlreadyRedeemed, bob.Social.TryRedeem(code!, out _));
        Assert.Equal(bobCoins + 50, bob.Economy.Coins);
    }

    [Fact]
    public void TwoGiftsOfSameAmount_AreBothRedeemable()
    {
        var (alice, bob) = TwoPlayers();
        var c1 = alice.Social.TryCreateGift(bob.Profile.PlayerCode, GiftKind.Coins, "", 10)!;
        var c2 = alice.Social.TryCreateGift(bob.Profile.PlayerCode, GiftKind.Coins, "", 10)!;
        Assert.NotEqual(c1, c2);
        Assert.Equal(RedeemError.None, bob.Social.TryRedeem(c1, out _));
        Assert.Equal(RedeemError.None, bob.Social.TryRedeem(c2, out _));
    }

    [Fact]
    public void IngredientGift_MovesIngredients()
    {
        var (alice, bob) = TwoPlayers();
        var aliceRice = alice.Inventory.Count("rice");
        var bobRice = bob.Inventory.Count("rice");
        var code = alice.Social.TryCreateGift(bob.Profile.PlayerCode, GiftKind.Ingredient, "rice", 2);
        Assert.NotNull(code);
        Assert.Equal(aliceRice - 2, alice.Inventory.Count("rice"));
        Assert.Equal(RedeemError.None, bob.Social.TryRedeem(code!, out _));
        Assert.Equal(bobRice + 2, bob.Inventory.Count("rice"));
    }

    [Fact]
    public void CreateGift_Rejections_TakeNothing()
    {
        var (alice, bob) = TwoPlayers();
        var bobCode = bob.Profile.PlayerCode;
        var rice = alice.Inventory.Count("rice");

        Assert.Null(alice.Social.TryCreateGift(alice.Profile.PlayerCode, GiftKind.Coins, "", 10)); // to self
        Assert.Null(alice.Social.TryCreateGift("BAD", GiftKind.Coins, "", 10));
        Assert.Null(alice.Social.TryCreateGift(bobCode, GiftKind.Coins, "", 0));
        Assert.Null(alice.Social.TryCreateGift(bobCode, GiftKind.Coins, "", SocialSystem.MaxGiftCoins + 1));
        Assert.Null(alice.Social.TryCreateGift(bobCode, GiftKind.Ingredient, "rice", SocialSystem.MaxGiftIngredients + 1));
        Assert.Null(alice.Social.TryCreateGift(bobCode, GiftKind.Ingredient, "rice", rice + 1));
        Assert.Null(alice.Social.TryCreateGift(bobCode, GiftKind.Ingredient, "gold_bar", 1));
        alice.Profile.Coins = 5;
        Assert.Null(alice.Social.TryCreateGift(bobCode, GiftKind.Coins, "", 10));

        Assert.Equal(5, alice.Economy.Coins);
        Assert.Equal(rice, alice.Inventory.Count("rice"));
        Assert.Equal(0, alice.Profile.Stats.GiftsSent);
    }

    [Fact]
    public void Redeem_NotForYou()
    {
        var (alice, bob) = TwoPlayers();
        var carol = TestGame.Create(seed: 3, name: "Carol");
        var code = alice.Social.TryCreateGift(bob.Profile.PlayerCode, GiftKind.Coins, "", 10)!;
        var coins = carol.Economy.Coins;
        Assert.Equal(RedeemError.NotForYou, carol.Social.TryRedeem(code, out _));
        Assert.Equal(RedeemError.NotForYou, alice.Social.TryRedeem(code, out _));
        Assert.Equal(coins, carol.Economy.Coins);
    }

    [Fact]
    public void Redeem_FromYourself()
    {
        var g = TestGame.Create();
        var me = g.Profile.PlayerCode;
        var code = SocialSystem.Encode(new Gift(me, me, GiftKind.Coins, "coins", 10, "ABCDE"));
        Assert.Equal(RedeemError.FromYourself, g.Social.TryRedeem(code, out _));
    }

    [Fact]
    public void Redeem_Invalid()
    {
        var g = TestGame.Create();
        Assert.Equal(RedeemError.Invalid, g.Social.TryRedeem("HC1-NOPE-NOPE", out var gift));
        Assert.Null(gift);
    }
}
