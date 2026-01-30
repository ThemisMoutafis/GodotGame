using Godot;

public partial class SnitchRat : CharacterBody2D // Or AnimatableBody2D
{
    public override void _Ready()
    {
        // Get the Hitbox Area2D we created in the editor
        var hitbox = GetNode<Area2D>("Hitbox");

        // Subscribe to the signal for when Dimi enters the zone
        hitbox.BodyEntered += OnHitboxBodyEntered;
    }

    private void OnHitboxBodyEntered(Node2D body)
    {
        // Use your existing Player class check
        if (body is Player dimi)
        {
            GD.Print("Rat kill triggered!");
            dimi.TriggerDeath();
        }
    }
}