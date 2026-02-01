using Godot;
using System;

public partial class SnitchRat : CharacterBody2D
{
    [ExportGroup("Attack Settings")]
    [Export] public float MinDelay = 1.5f;
    [Export] public float MaxDelay = 4.0f;
    [Export] public float AttackOffset = 70.0f; 
    [Export] public int BiteDamage = 20;

    [ExportGroup("Patrol Settings")]
    [Export] public bool IsPatrolling = true;
    [Export] public float PatrolDistance = 200.0f;
    [Export] public float Speed = 100.0f;

    private AnimatedSprite2D _sprite;
    private CollisionShape2D _hitboxShape;
    private Area2D _hitboxArea;
    private Timer _timer;
    private bool _playerInRange = false;
    private bool _isAttacking = false;
    private bool _isDead = false;
    private bool _hasDealtDamageThisAttack = false;

    private Vector2 _startPosition;
    private int _direction = -1; 
    private float _flipCooldown = 0.0f;

    public override void _Ready()
    {
        _startPosition = GlobalPosition;
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _hitboxShape = GetNode<CollisionShape2D>("Hitbox/CollisionShape2D");
        _hitboxArea = GetNode<Area2D>("Hitbox");
        
        _hitboxArea.Monitoring = false;

        _timer = new Timer();
        AddChild(_timer);
        _timer.OneShot = true;
        _timer.Timeout += () => { if (_playerInRange && !_isDead) PerformLounge(); };

        _sprite.Play("RunRat");

        _sprite.AnimationFinished += () => {
            if (_sprite.Animation == "DeathRat") QueueFree();
        };

        _hitboxArea.BodyEntered += (body) => {
            if (_isDead || !_isAttacking || _hasDealtDamageThisAttack) return;
            if (body is Player dimi) {
                dimi.TakeDamage(BiteDamage);
                _hasDealtDamageThisAttack = true; 
                GD.Print("Bitten!");
            }
        };

        var trigger = GetNode<Area2D>("AttackTrigger");
        trigger.BodyEntered += (body) => {
            if (body is Player && !_isDead) {
                _playerInRange = true;
                if (!_isAttacking) PerformLounge(); 
            }
        };
        trigger.BodyExited += (body) => { if (body is Player) _playerInRange = false; };
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead) return;

        // --- 💀 IMPROVED STOMP DETECTION ---
        // 1. Check for "Resting" Pressure (Dimi standing still on head)
        // We test a move 2 pixels UP. If we hit Dimi, he's on top.
        var stompTest = MoveAndCollide(new Vector2(0, -2), true); 
        if (stompTest != null && stompTest.GetCollider() is Player standingDimi)
        {
            if (standingDimi.Velocity.Y >= 0)
            {
                Die(standingDimi);
                return;
            }
        }

        if (_flipCooldown > 0) _flipCooldown -= (float)delta;

        Vector2 velocity = Velocity;
        if (!IsOnFloor()) velocity.Y += GetGravity().Y * (float)delta;
        else velocity.Y = 0;

        if (_playerInRange || _isAttacking) {
            velocity.X = 0;
            if (!_isAttacking) FacePlayer(); 
        } else {
            velocity.X = _direction * Speed;
            if (_flipCooldown <= 0) {
                if (IsOnWall()) FlipDirection();
                else if (Mathf.Abs(GlobalPosition.X - _startPosition.X) >= PatrolDistance) FlipDirection();
            }

            if (!_isAttacking && _sprite.Animation != "RunRat") {
                _sprite.Play("RunRat");
            }
        }

        Velocity = velocity;
        MoveAndSlide();

        // 2. Check for "Impact" Pressure (Dimi landing this frame)
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = GetSlideCollision(i);
            if (collision.GetCollider() is Player landingDimi)
            {
                if (collision.GetNormal().Y > 0.6f && landingDimi.Velocity.Y >= 0)
                {
                    Die(landingDimi);
                    return;
                }
            }
        }
    }

    private void Die(Player dimi)
    {
        if (_isDead) return;
        _isDead = true;

        GD.Print("Stomped!");
        _timer.Stop();
        _sprite.Stop();
        _sprite.Play("DeathRat");

        CollisionLayer = 0;
        CollisionMask = 0;
        _hitboxArea.Monitoring = false;

        // Force the bounce on Dimi
        dimi.Velocity = new Vector2(dimi.Velocity.X, -400);
    }

    private void FacePlayer() {
        var dimi = GetTree().GetFirstNodeInGroup("Player") as Node2D;
        if (dimi != null) {
            float diffX = dimi.GlobalPosition.X - GlobalPosition.X;
            if (Mathf.Abs(diffX) > 5.0f) {
                _direction = diffX > 0 ? 1 : -1;
                _sprite.FlipH = _direction > 0;
            }
        }
    }

    private void FlipDirection() {
        _direction *= -1;
        _sprite.FlipH = _direction > 0;
        _startPosition = GlobalPosition;
        _flipCooldown = 0.25f; 
    }

    private async void PerformLounge() {
        if (_isAttacking || _isDead || !IsInstanceValid(this)) return;
        
        _isAttacking = true;
        _hasDealtDamageThisAttack = false;
        FacePlayer(); 

        string attackAnim = (GD.Randi() % 2 == 0) ? "AttackRat" : "AttackRat2";
        _sprite.Stop(); 
        _sprite.Play(attackAnim);

        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        if (!IsInstanceValid(this) || _isDead) return;

        _hitboxShape.Position = new Vector2(AttackOffset * _direction, 0);
        _hitboxArea.Monitoring = true;
        
        await ToSignal(GetTree().CreateTimer(0.4f), "timeout");
        if (!IsInstanceValid(this) || _isDead) return;

        _hitboxArea.Monitoring = false;
        _hitboxShape.Position = Vector2.Zero;

        if (IsInstanceValid(_sprite) && !_isDead) {
            if (_sprite.Animation == "AttackRat" || _sprite.Animation == "AttackRat2")
                await ToSignal(_sprite, "animation_finished");
        }
        
        _isAttacking = false;
        if (_playerInRange && !_isDead) _timer.Start(GD.RandRange(MinDelay, MaxDelay));
    }
}