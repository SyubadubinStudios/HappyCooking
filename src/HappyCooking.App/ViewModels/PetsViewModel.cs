using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Services;
using HappyCooking.Core.Content;
using HappyCooking.Core.Systems;

namespace HappyCooking.App.ViewModels;

public sealed partial class PetCard : ObservableObject
{
    public PetCard(Pet pet) => Pet = pet;

    public Pet Pet { get; }

    public Bitmap? Image => Images.Pet(Pet.Id);

    public IBrush Backdrop => new SolidColorBrush(Color.Parse(Pet.Color), 0.35);

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _bonus = string.Empty;

    [ObservableProperty]
    private string _price = string.Empty;

    [ObservableProperty]
    private Bitmap? _priceIcon;

    [ObservableProperty]
    private bool _owned;

    [ObservableProperty]
    private bool _active;

    [ObservableProperty]
    private bool _locked;

    [ObservableProperty]
    private bool _canAdopt;

    [ObservableProperty]
    private string _lockText = string.Empty;

    [ObservableProperty]
    private double _happiness;

    [ObservableProperty]
    private string _mood = string.Empty;

    public bool ShowAdopt => !Owned && !Locked;

    public bool ShowCare => Owned;

    partial void OnOwnedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowAdopt));
        OnPropertyChanged(nameof(ShowCare));
    }

    partial void OnLockedChanged(bool value) => OnPropertyChanged(nameof(ShowAdopt));
}

/// <summary>The pet house: adopt, feed and choose the helper that travels with you.</summary>
public sealed partial class PetsViewModel : PageViewModel
{
    public PetsViewModel() => Refresh();

    public override string DockKey => "pets";

    public ObservableCollection<PetCard> Pets { get; } = [];

    public string FeedCost { get; } = PetSystem.FeedCost.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public Bitmap? CoinIcon { get; } = Images.Icon("coin");

    private void Refresh()
    {
        if (Pets.Count == 0)
        {
            foreach (var pet in Game.Content.Pets)
            {
                Pets.Add(new PetCard(pet));
            }
        }

        var pets = Game.Pets;
        foreach (var card in Pets)
        {
            var pet = card.Pet;
            card.Name = L.Get(pet.Name);
            card.Description = L.Get(pet.Description);
            card.Bonus = L.Format("pet.bonus." + pet.Bonus.ToString().ToLowerInvariant(), (int)MathF.Round(pet.Amount * 100));
            card.Price = pet.Price.ToString(System.Globalization.CultureInfo.CurrentCulture);
            card.PriceIcon = Images.Icon(pet.PriceInGems ? "gem" : "coin");
            card.Owned = pets.Owns(pet.Id);
            card.Active = Game.Profile.ActivePet == pet.Id;
            card.Locked = Game.Economy.Level < pet.UnlockLevel;
            card.LockText = L.Format("common.unlockAtLevel", pet.UnlockLevel);
            card.CanAdopt = pets.CanAdopt(pet);
            var h = pets.Happiness(pet.Id);
            card.Happiness = h;
            card.Mood = h >= 70 ? L["pet.mood.happy"] : h >= PetSystem.HappyThreshold ? L["pet.mood.ok"] : L["pet.mood.hungry"];
        }
    }

    [RelayCommand]
    private void Adopt(PetCard card)
    {
        if (Game.Pets.TryAdopt(card.Pet.Id))
        {
            Host.Sfx.Play(Sound.Perfect);
            Host.Toast(L["pet.welcome"], L.Get(card.Pet.Name), "pets");
            Host.Touch();
        }
        else
        {
            Host.Sfx.Play(Sound.Oops);
        }

        Refresh();
    }

    [RelayCommand]
    private void Feed(PetCard card)
    {
        Host.Sfx.Play(Game.Pets.TryFeed(card.Pet.Id) ? Sound.Pop : Sound.Oops);
        Host.Touch();
        Refresh();
    }

    [RelayCommand]
    private void SetActive(PetCard card)
    {
        if (Game.Pets.SetActive(card.Pet.Id))
        {
            Host.Sfx.Play(Sound.Good);
            Host.Touch();
        }

        Refresh();
    }

    public override void RefreshTexts() => Refresh();
}
