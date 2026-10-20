using System.Collections.ObjectModel;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Services;
using HappyCooking.Core.Content;
using HappyCooking.Core.State;
using HappyCooking.Core.Systems;

namespace HappyCooking.App.ViewModels;

public sealed partial class FriendCard(Friend friend) : ObservableObject
{
    public Friend Friend { get; } = friend;

    public string Name { get; } = friend.Name;

    public string Code { get; } = friend.Code;

    public string Initial { get; } = friend.Name.Length > 0 ? friend.Name[..1].ToUpperInvariant() : "?";

    [ObservableProperty]
    private bool _isSelected;
}

public sealed class GiftOption(string id, string name, bool isCoins)
{
    public string Id { get; } = id;

    public string Name { get; } = name;

    public bool IsCoins { get; } = isCoins;

    public override string ToString() => Name;
}

/// <summary>
/// Friends and gifts without accounts: share your player code, add friends by
/// theirs, and send presents as short codes a grown-up can pass along.
/// </summary>
public sealed partial class FriendsViewModel : PageViewModel
{
    [ObservableProperty]
    private string _newFriendCode = string.Empty;

    [ObservableProperty]
    private string _newFriendName = string.Empty;

    [ObservableProperty]
    private string _friendMessage = string.Empty;

    [ObservableProperty]
    private FriendCard? _selectedFriend;

    [ObservableProperty]
    private GiftOption? _selectedGift;

    [ObservableProperty]
    private decimal? _giftAmount = 10;

    [ObservableProperty]
    private string _giftCode = string.Empty;

    [ObservableProperty]
    private string _giftMessage = string.Empty;

    [ObservableProperty]
    private string _redeemCode = string.Empty;

    [ObservableProperty]
    private string _redeemMessage = string.Empty;

    public FriendsViewModel() => Build();

    public override string DockKey => "friends";

    public string MyCode => Game.Social.PlayerCode;

    public ObservableCollection<FriendCard> Friends { get; } = [];

    public ObservableCollection<GiftOption> GiftOptions { get; } = [];

    public bool HasFriends => Friends.Count > 0;

    public bool HasGiftCode => GiftCode.Length > 0;

    partial void OnGiftCodeChanged(string value) => OnPropertyChanged(nameof(HasGiftCode));

    partial void OnSelectedFriendChanged(FriendCard? value)
    {
        foreach (var f in Friends)
        {
            f.IsSelected = f == value;
        }
    }

    private void Build()
    {
        Friends.Clear();
        foreach (var f in Game.Social.Friends)
        {
            Friends.Add(new FriendCard(f));
        }

        OnPropertyChanged(nameof(HasFriends));
        SelectedFriend ??= Friends.FirstOrDefault();

        GiftOptions.Clear();
        GiftOptions.Add(new GiftOption("coins", L["friends.giftCoins"], true));
        foreach (var (id, count) in Game.Profile.Inventory.OrderByDescending(kv => kv.Value))
        {
            if (count > 0 && Game.Content.IngredientById.TryGetValue(id, out Ingredient? ing))
            {
                GiftOptions.Add(new GiftOption(id, $"{L.Get(ing.Name)} (×{count})", false));
            }
        }

        SelectedGift = GiftOptions[0];
    }

    [RelayCommand]
    private void SelectFriend(FriendCard card) => SelectedFriend = card;

    [RelayCommand]
    private void AddFriend()
    {
        if (Game.Social.TryAddFriend(NewFriendCode, NewFriendName))
        {
            FriendMessage = L.Format("friends.added", NewFriendName.Trim());
            NewFriendCode = string.Empty;
            NewFriendName = string.Empty;
            Host.Sfx.Play(Sound.Good);
            Host.Touch();
            Build();
            SelectedFriend = Friends.LastOrDefault();
        }
        else
        {
            FriendMessage = L["friends.addFailed"];
            Host.Sfx.Play(Sound.Oops);
        }
    }

    [RelayCommand]
    private void RemoveFriend(FriendCard card)
    {
        Game.Social.RemoveFriend(card.Code);
        if (SelectedFriend == card)
        {
            SelectedFriend = null;
        }

        Host.Touch();
        Build();
    }

    [RelayCommand]
    private void CreateGift()
    {
        if (SelectedFriend is null || SelectedGift is null)
        {
            GiftMessage = L["friends.pickFriend"];
            return;
        }

        var amount = (int)(GiftAmount ?? 0);
        var code = Game.Social.TryCreateGift(
            SelectedFriend.Code,
            SelectedGift.IsCoins ? GiftKind.Coins : GiftKind.Ingredient,
            SelectedGift.Id,
            amount);
        if (code is null)
        {
            GiftCode = string.Empty;
            GiftMessage = L.Format("friends.giftFailed", SocialSystem.MaxGiftCoins, SocialSystem.MaxGiftIngredients);
            Host.Sfx.Play(Sound.Oops);
            return;
        }

        GiftCode = code;
        GiftMessage = L.Format("friends.giftReady", SelectedFriend.Name);
        Host.Sfx.Play(Sound.Perfect);
        Host.Touch();
        Build();
    }

    [RelayCommand]
    private async Task CopyGift()
    {
        var clipboard = Avalonia.Application.Current?.ApplicationLifetime switch
        {
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d => d.MainWindow?.Clipboard,
            Avalonia.Controls.ApplicationLifetimes.ISingleViewApplicationLifetime s => Avalonia.Controls.TopLevel.GetTopLevel(s.MainView)?.Clipboard,
            _ => null,
        };
        if (clipboard is not null && GiftCode.Length > 0)
        {
            await clipboard.SetTextAsync(GiftCode);
            GiftMessage = L["friends.copied"];
        }
    }

    [RelayCommand]
    private void Redeem()
    {
        var error = Game.Social.TryRedeem(RedeemCode, out var gift);
        if (error == RedeemError.None && gift is not null)
        {
            var what = gift.Kind == GiftKind.Coins
                ? L.Format("friends.coinsAmount", gift.Amount)
                : $"{gift.Amount} {L.Get(Game.Content.IngredientById[gift.Item].Name)}";
            RedeemMessage = L.Format("friends.redeemed", what);
            RedeemCode = string.Empty;
            Host.Sfx.Play(Sound.LevelUp);
            Host.Touch();
            Build();
        }
        else
        {
            RedeemMessage = L["friends.redeem." + error.ToString().ToLowerInvariant()];
            Host.Sfx.Play(Sound.Oops);
        }
    }

    public override void RefreshTexts() => Build();
}
