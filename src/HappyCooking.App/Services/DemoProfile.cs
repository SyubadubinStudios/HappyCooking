using HappyCooking.Core;
using HappyCooking.Core.Content;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;

namespace HappyCooking.App.Services;

/// <summary>
/// A mid-game profile: several islands open, a few pets, a planted farm and dishes
/// on the counter. Used by <c>--demo</c> and the screenshot tour so every screen
/// has something to show. Never saved to disk.
/// </summary>
public static class DemoProfile
{
    public static PlayerProfile Create(GameContent content, IClock clock)
    {
        var p = GameSession.NewProfile(content, "Ariana", clock);
        p.Level = 7;
        p.Xp = 180;
        p.Coins = 1240;
        p.Gems = 36;

        string[] cooked = ["nasi_goreng", "sate", "rendang", "soto", "pempek", "sushi", "ramen", "pizza", "gelato", "dim_sum", "taco", "rainbow_cupcake", "galaxy_donut"];
        for (var i = 0; i < cooked.Length; i++)
        {
            p.BestStars[cooked[i]] = (i % 3) + 1;
            p.TimesCooked[cooked[i]] = 1 + (i % 4);
        }

        p.Counter["rendang"] = 2;
        p.Counter["sushi"] = 1;
        p.Counter["pizza"] = 2;
        p.Counter["taco"] = 1;
        p.Counter["galaxy_donut"] = 1;

        var now = clock.UtcNow;
        p.Pets["kiwi"] = new PetState { Happiness = 95, LastFedUtc = now };
        p.Pets["sambal"] = new PetState { Happiness = 70, LastFedUtc = now };
        p.Pets["mango"] = new PetState { Happiness = 88, LastFedUtc = now };
        p.ActivePet = "sambal";

        p.Farm.Clear();
        string?[] crops = ["chili", "tomato", "strawberry", "corn", null, "basil"];
        int[] ages = [60, 20, 40, 75, 0, 10];
        for (var i = 0; i < crops.Length; i++)
        {
            p.Farm.Add(new FarmPlot { Crop = crops[i], PlantedUtc = now.AddSeconds(-ages[i]) });
        }

        p.FishLog["sunny_salmon"] = 3;
        p.FishLog["merry_mackerel"] = 4;
        p.FishLog["blue_crab"] = 1;
        p.Decorations.AddRange(["d_flower_pot", "d_lantern", "d_plant", "d_balloons"]);
        p.CompletedQuests.UnionWith(["q_first_dish", "q_first_customers", "q_little_farm", "q_gone_fishing", "q_rendang", "q_pet_friend"]);
        p.QuestProgress["q_nusantara_tour"] = 3;
        p.QuestProgress["q_feed_pet"] = 1;
        p.Friends.Add(new Friend { Code = "7K3M9PQR", Name = "Kenji", AddedUtc = now });
        p.Friends.Add(new Friend { Code = "R2D4X8ZT", Name = "Putri", AddedUtc = now });

        p.Stats.DishesCooked = 34;
        p.Stats.ThreeStarDishes = 12;
        p.Stats.TotalStars = 81;
        p.Stats.CustomersServed = 27;
        p.Stats.FishCaught = 8;
        p.Stats.CropsHarvested = 15;
        p.Stats.CreativeDishes = 1;
        return p;
    }
}
