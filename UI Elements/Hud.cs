using Godot;
using System;

public partial class Hud : CanvasLayer
{
    private TextureProgressBar _healthBar;

    // This runs the moment the node enters the scene tree
    public override void _EnterTree()
    {
        GD.Print(">>> HUD SCRIPT HAS ENTERED THE TREE <<<");
    }

    public override void _Ready()
    {
        GD.Print("HUD: _Ready is starting...");
        
        _healthBar = GetNodeOrNull<TextureProgressBar>("TextureProgressBar");
        
        if (_healthBar != null)
        {
            _healthBar.Value = 100; // Force it to show as full
            GD.Print("HUD: TextureProgressBar found and initialized.");
        }
        else
        {
            GD.PrintErr("HUD: CRITICAL ERROR - TextureProgressBar node not found!");
        }

        // Search for Dimi
        var player = GetTree().Root.FindChild("Player", true, false) as Player;
        if (player != null)
        {
            player.HealthChanged += OnPlayerHealthChanged;
            GD.Print("HUD: Successfully wired to Player.");
        }
    }

    public void OnPlayerHealthChanged(int newHealth)
    {
        GD.Print($"HUD: Received Damage Update! Current Health: {newHealth}");
        _healthBar.Value = (double)newHealth;
    }
}