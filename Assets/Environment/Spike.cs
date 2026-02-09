using Godot;

public partial class Spike : Area2D
{
    public override void _Ready()
    {
        // Connect the signal via C# delegate
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        // Pattern matching: Check if the body is our Player class
        if (body is Player player)
        {
            GD.Print("Dimi hit a spike! 💀");
            player.TriggerDeath();
        }
    }
}
