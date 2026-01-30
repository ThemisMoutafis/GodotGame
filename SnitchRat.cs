using Godot;
using System;

public partial class SnitchRat : CharacterBody2D
{
    [Export] public float MinDelay = 1.5f;
    [Export] public float MaxDelay = 4.0f;
    [Export] public float AttackOffset = -50.0f; 

    private AnimatedSprite2D _sprite;
    private CollisionShape2D _hitboxShape;
    private Timer _timer;
    private bool _playerInRange = false;
    private bool _isFirstAttack = true; // Gatekeeper for the instant bite

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _hitboxShape = GetNode<CollisionShape2D>("Hitbox/CollisionShape2D");
        
        _timer = new Timer();
        AddChild(_timer);
        _timer.OneShot = true;
        _timer.Timeout += OnAttackTimerTimeout;

        // Kill Logic
        GetNode<Area2D>("Hitbox").BodyEntered += (body) => {
    if (body is Player dimi) 
    {
        // Calculate the difference between Dimi's feet and the Rat's top
        float verticalDiff = dimi.GlobalPosition.Y - GlobalPosition.Y;

        // If Dimi is significantly higher than the rat's center, he's 'on top'
        if (verticalDiff > -10) // Adjust this number based on Rat height
        {
            dimi.TriggerDeath();
        }
    }
};

        // Range Logic
        var trigger = GetNode<Area2D>("AttackTrigger");
        trigger.BodyEntered += (body) => {
            if (body is Player) {
                _playerInRange = true;
                DetermineNextAction(); // Decide: bite now or wait?
            }
        };

        trigger.BodyExited += (body) => {
            if (body is Player) {
                _playerInRange = false;
                _timer.Stop();
                _isFirstAttack = true; // Reset so the next approach is also instant
                _sprite.Play("IdleRat");
            }
        };
    }

    private void DetermineNextAction()
    {
        if (!_playerInRange) return;

        if (_isFirstAttack)
        {
            _isFirstAttack = false; // Close the gate
            PerformLounge();        // Strike immediately
        }
        else
        {
            // Wait for a random interval before the next bite
            _timer.Start(GD.RandRange(MinDelay, MaxDelay));
        }
    }

    private void OnAttackTimerTimeout()
    {
        if (_playerInRange) PerformLounge();
    }

    private async void PerformLounge()
    {
        // 0% - Start Visuals
        _sprite.Play("AttackRat");

        // Wind-up Delay (Telegraph)
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        if (!IsInstanceValid(this)) return;

        // Active Phase (Lethal)
        _hitboxShape.Position = new Vector2(AttackOffset, _hitboxShape.Position.Y);
        
        // Active Duration (The 70% mark)
        await ToSignal(GetTree().CreateTimer(0.4f), "timeout");
        if (!IsInstanceValid(this)) return;

        // Retract Phase (Safe / Recovery)
        _hitboxShape.Position = Vector2.Zero; 

        // Finish remaining 30% of animation
        if (_sprite.IsPlaying() && _sprite.Animation == "AttackRat")
            await ToSignal(_sprite, "animation_finished");

        _sprite.Play("IdleRat");

        // Loop: Check if we should queue the next random attack
        if (_playerInRange) DetermineNextAction();
    }
}