using System.Numerics;
using HappyCooking.Core.Content;
using ThreeNet;

namespace HappyCooking.App.Rendering;

/// <summary>A turntable for the recipe book: the selected dish slowly spinning on a pedestal.</summary>
public sealed class DishPreviewStage : Stage
{
    private Node? _dish;
    private readonly Node _turntable;

    public DishPreviewStage()
    {
        IndoorEnvironment("#FFF4D6");
        AddLights(new Vector3(0.6f, 1f, 0.8f), 3.2f);
        _turntable = Scene.CreateNode(null, "turntable");
        Props.Cylinder(new Vector3(0, -0.1f, 0), 0.55f, 0.2f, "#FF8FB1", _turntable);
        Props.Cylinder(new Vector3(0, 0.01f, 0), 0.5f, 0.03f, "#FFFFFF", _turntable);
        Props.Cylinder(new Vector3(0, -0.6f, 0), 0.25f, 0.8f, "#FF8FB1");
        Camera.Position = new Vector3(0, 0.72f, 1.3f);
        Camera.LookAt(new Vector3(0, 0.08f, 0));
    }

    public void Show(Recipe recipe)
    {
        _dish?.Remove();
        if (recipe.Model is { } model && ModelLibrary.Exists(model))
        {
            _dish = ModelLibrary.LoadStatic(Scene, model, _turntable);
            _dish.Scale = new Vector3(1.1f);
        }
        else
        {
            _dish = Props.GenericDish(recipe.Color, _turntable, recipe.Id.Length);
            _dish.Scale = new Vector3(1.4f);
        }

        _dish.Position = new Vector3(0, 0.03f, 0);
    }

    protected override void OnUpdate(float dt, double total) =>
        _turntable.EulerAngles = new Vector3(0, (float)total * 0.5f, 0);
}
