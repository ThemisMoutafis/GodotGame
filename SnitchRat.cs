using Godot;
using System;

public partial class SnitchRat : CharacterBody2D
{
    [ExportGroup("Attack Settings")]
    [Export] public float MinDelay = 1.5f;
    [Export] public float MaxDelay = 4.0f;
    [Export] public float AttackOffset = -70.0f; 
    [Export] public int BiteDamage = 20; // How much health Dimi loses per bite

    private AnimatedSprite2D _sprite;
    private CollisionShape2D _hitboxShape;
    private Timer _timer;
    private bool _playerInRange = false;
    private bool _isFirstAttack = true;

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _hitboxShape = GetNode<CollisionShape2D>("Hitbox/CollisionShape2D");
        
        _timer = new Timer();
        AddChild(_timer);
        _timer.OneShot = true;
        _timer.Timeout += OnAttackTimerTimeout;

        // --- UPDATED DAMAGE LOGIC ---
        GetNode<Area2D>("Hitbox").BodyEntered += (body) => {
            if (body is Player dimi) 
            {
                // verticalDiff > -10 ensures Dimi is not landing on the rat's back
                float verticalDiff = dimi.GlobalPosition.Y - GlobalPosition.Y;

                if (verticalDiff > -10) 
                {
                    // Call the new Health API instead of instant death
                    dimi.TakeDamage(BiteDamage);
                }
            }
        };

        // Range Logic
        var trigger = GetNode<Area2D>("AttackTrigger");
        trigger.BodyEntered += (body) => {
            if (body is Player) {
                _playerInRange = true;
                DetermineNextAction();
            }
        };

        trigger.BodyExited += (body) => {
            if (body is Player) {
                _playerInRange = false;
                _timer.Stop();
                _isFirstAttack = true;
                _sprite.Play("IdleRat");
            }
        };
    }

    private void DetermineNextAction()
    {
        if (!_playerInRange) return;

        if (_isFirstAttack)
        {
            _isFirstAttack = false;
            PerformLounge();
        }
        else
        {
            _timer.Start(GD.RandRange(MinDelay, MaxDelay));
        }
    }

    private void OnAttackTimerTimeout()
    {
        if (_playerInRange) PerformLounge();
    }

    private async void PerformLounge()
    {
        _sprite.Play("AttackRat");

        // Wind-up (Telegraph)
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        if (!IsInstanceValid(this)) return;

        // Active Phase (Lethal)
        _hitboxShape.Position = new Vector2(AttackOffset, _hitboxShape.Position.Y);
        
        // Active Duration (70% mark)
        await ToSignal(GetTree().CreateTimer(0.4f), "timeout");
        if (!IsInstanceValid(this)) return;

        // Retract Phase (Safe / Recovery)
        _hitboxShape.Position = Vector2.Zero; 

        // Finish remaining 30% of animation
        if (_sprite.IsPlaying() && _sprite.Animation == "AttackRat")
            await ToSignal(_sprite, "animation_finished");

        _sprite.Play("IdleRat");

        if (_playerInRange) DetermineNextAction();
    }
}