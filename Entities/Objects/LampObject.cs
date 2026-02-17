using Godot;
using System;

public partial class LampObject : Node2D
{
    private bool _playerInside = false;
    private bool _isTaken = false;

    public override void _Ready()
    {
        // Connect signals
        var area = GetNode<Area2D>("InteractionArea");
        area.BodyEntered += OnBodyEntered;
        area.BodyExited += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        // THE LOGIC: Player is here + Pressing "S" + Lamp isn't already gone
        if (!_isTaken && _playerInside && Input.IsActionJustPressed("interact")) //s button
        {
            CollectLamp();
        }
    }

    private void CollectLamp()
    {
        _isTaken = true;
        
        // Find Dimi and update his status
        var player = GetTree().GetFirstNodeInGroup("Player") as Player;
        if (player != null)
        {
            player.HasLamp = true;
            GD.Print("Dimi collected the lamp!");
        }

        // Hide visuals and light
        GetNode<AnimatedSprite2D>("LampSprite").Visible = false;
        var light = GetNodeOrNull<PointLight2D>("LampLight");
        if (light != null) light.Enabled = false;

        // Optionally queue the object for deletion if you don't need it anymore
        // QueueFree(); 
    }

    private void OnBodyEntered(Node2D body)
{
    if (body.IsInGroup("Player")) 
    {
        _playerInside = true;
        GD.Print("Dimi is on the lamp!"); // If this doesn't show up, detection is broken
    }
}

    private void OnBodyExited(Node2D body)
    {
        if (body.IsInGroup("Player")) _playerInside = false;
    }
}