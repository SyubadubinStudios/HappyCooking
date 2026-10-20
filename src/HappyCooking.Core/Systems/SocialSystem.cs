using System.Text;
using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Systems;

public enum GiftKind
{
    Coins,
    Ingredient,
}

public sealed record Gift(string From, string To, GiftKind Kind, string Item, int Amount, string Nonce);

public enum RedeemError
{
    None,
    Invalid,
    NotForYou,
    FromYourself,
    AlreadyRedeemed,
}

/// <summary>
/// Offline-first social features for children: friends are added by their player
/// code and gifts travel as short codes that can be read aloud, typed or sent in
/// a chat. No accounts, no chat, no personal data leaves the device.
/// </summary>
public sealed class SocialSystem(
    GameContent content,
    PlayerProfile profile,
    EconomySystem economy,
    InventorySystem inventory,
    GameEvents events,
    IClock clock,
    GameRandom random)
{
    // Crockford base32: no I, L, O or U, so codes are easy to read aloud.
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const string GiftPrefix = "HC1";

    public const int MaxGiftCoins = 100;
    public const int MaxGiftIngredients = 10;

    public string PlayerCode => profile.PlayerCode;

    public IReadOnlyList<Friend> Friends => profile.Friends;

    public static string NewPlayerCode(GameRandom random)
    {
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[random.Range(0, Alphabet.Length)];
        }

        return new string(chars);
    }

    /// <summary>Upper-cases and maps look-alike characters (O→0, I/L→1) so typed codes still match.</summary>
    public static string NormalizeCode(string code)
    {
        var sb = new StringBuilder(code.Length);
        foreach (var raw in code.Trim().ToUpperInvariant())
        {
            var c = raw switch { 'O' => '0', 'I' or 'L' => '1', _ => raw };
            if (Alphabet.Contains(c) || c == '-')
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public static bool IsValidPlayerCode(string code) =>
        code.Length == 8 && code.All(c => Alphabet.Contains(c));

    public bool TryAddFriend(string code, string name)
    {
        code = NormalizeCode(code);
        name = name.Trim();
        if (!IsValidPlayerCode(code) || code == profile.PlayerCode || name.Length is 0 or > 20 ||
            profile.Friends.Exists(f => f.Code == code))
        {
            return false;
        }

        profile.Friends.Add(new Friend { Code = code, Name = name, AddedUtc = clock.UtcNow });
        return true;
    }

    public bool RemoveFriend(string code) => profile.Friends.RemoveAll(f => f.Code == code) > 0;

    /// <summary>
    /// Takes the gift out of this player's pantry or purse and returns the code to
    /// give to the friend, or null when it cannot be afforded.
    /// </summary>
    public string? TryCreateGift(string friendCode, GiftKind kind, string item, int amount)
    {
        friendCode = NormalizeCode(friendCode);
        if (!IsValidPlayerCode(friendCode) || friendCode == profile.PlayerCode || amount <= 0)
        {
            return null;
        }

        switch (kind)
        {
            case GiftKind.Coins when amount <= MaxGiftCoins && economy.TrySpendCoins(amount):
                item = "coins";
                break;
            case GiftKind.Ingredient when amount <= MaxGiftIngredients && content.IngredientById.ContainsKey(item) &&
                                          inventory.TryRemove(item, amount):
                break;
            default:
                return null;
        }

        var nonce = NewPlayerCode(random)[..5];
        profile.Stats.GiftsSent++;
        events.Raise(new GameEvent(GameEventKind.GiftSent, friendCode, amount));
        return Encode(new Gift(profile.PlayerCode, friendCode, kind, item, amount, nonce));
    }

    public RedeemError TryRedeem(string code, out Gift? gift)
    {
        gift = Decode(code);
        if (gift is null)
        {
            return RedeemError.Invalid;
        }

        if (gift.To != profile.PlayerCode)
        {
            return RedeemError.NotForYou;
        }

        if (gift.From == profile.PlayerCode)
        {
            return RedeemError.FromYourself;
        }

        var key = gift.From + gift.Nonce;
        if (profile.RedeemedGifts.Contains(key))
        {
            return RedeemError.AlreadyRedeemed;
        }

        profile.RedeemedGifts.Add(key);
        if (gift.Kind == GiftKind.Coins)
        {
            economy.AddCoins(gift.Amount);
        }
        else
        {
            inventory.Add(gift.Item, gift.Amount);
        }

        return RedeemError.None;
    }

    /// <summary>
    /// Gift codes look like <c>HC1-XXXX-XXXX-...</c>: the gift fields in base32 plus a
    /// checksum, so typos are caught instead of giving the wrong present.
    /// This is a friendly code, not security; it only stops accidents.
    /// </summary>
    public static string Encode(Gift gift)
    {
        var payload = $"{gift.From}|{gift.To}|{(int)gift.Kind}|{gift.Item}|{gift.Amount}|{gift.Nonce}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var body = ToBase32(bytes) + ToBase32(BitConverter.GetBytes(Checksum(bytes)))[..4];
        var groups = Enumerable.Range(0, (body.Length + 3) / 4)
            .Select(i => body.Substring(i * 4, Math.Min(4, body.Length - (i * 4))));
        return GiftPrefix + "-" + string.Join('-', groups);
    }

    public Gift? Decode(string code)
    {
        var normalized = NormalizeCode(code).Replace("-", string.Empty, StringComparison.Ordinal);
        if (!normalized.StartsWith(GiftPrefix, StringComparison.Ordinal) || normalized.Length < GiftPrefix.Length + 8)
        {
            return null;
        }

        normalized = normalized[GiftPrefix.Length..];
        var body = normalized[..^4];
        var check = normalized[^4..];
        var bytes = FromBase32(body);
        if (bytes is null || ToBase32(BitConverter.GetBytes(Checksum(bytes)))[..4] != check)
        {
            return null;
        }

        var parts = Encoding.UTF8.GetString(bytes).Split('|');
        if (parts.Length != 6 || !int.TryParse(parts[2], out var kind) || !int.TryParse(parts[4], out var amount) ||
            !Enum.IsDefined((GiftKind)kind) || amount <= 0)
        {
            return null;
        }

        var giftKind = (GiftKind)kind;
        if ((giftKind == GiftKind.Coins && amount > MaxGiftCoins) ||
            (giftKind == GiftKind.Ingredient && (amount > MaxGiftIngredients || !content.IngredientById.ContainsKey(parts[3]))))
        {
            return null;
        }

        return new Gift(parts[0], parts[1], giftKind, parts[3], amount, parts[5]);
    }

    private static uint Checksum(ReadOnlySpan<byte> data)
    {
        var h = 2166136261u;
        foreach (var b in data)
        {
            h = (h ^ b) * 16777619u;
        }

        return h;
    }

    private static string ToBase32(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder(((data.Length * 8) + 4) / 5);
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            sb.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        }

        return sb.ToString();
    }

    private static byte[]? FromBase32(string text)
    {
        var bytes = new List<byte>((text.Length * 5) / 8);
        int buffer = 0, bits = 0;
        foreach (var c in text)
        {
            var v = Alphabet.IndexOf(c, StringComparison.Ordinal);
            if (v < 0)
            {
                return null;
            }

            buffer = (buffer << 5) | v;
            bits += 5;
            if (bits >= 8)
            {
                bytes.Add((byte)(buffer >> (bits - 8)));
                bits -= 8;
            }
        }

        // The encoder pads the final character with zero bits; anything else is a typo.
        if (bits > 0 && (buffer & ((1 << bits) - 1)) != 0)
        {
            return null;
        }

        return [.. bytes];
    }
}
