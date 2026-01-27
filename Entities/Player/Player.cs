using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [ExportGroup("Movement")]
    [Export] public float Speed = 300.0f;
    [Export] public float JumpVelocity = -400.0f;
    [Export] public float JumpCutValue = 0.5f;
    [Export] public float FallGravityMultiplier = 2.0f; // Snappier falling

    [ExportGroup("Forgiveness")]
    [Export] public float JumpBufferTime = 0.15f;
    [Export] public float CoyoteTime = 0.15f; // "Mercy" window

    [ExportGroup("Nodes")]
    [Export] public AnimatedSprite2D PlayerSprite;

    private float _jumpBufferCounter = 0;
    private float _coyoteCounter = 0; // Tracks time since leaving floor
    private bool _wasInAir = false;
    private bool _isLanding = false;

    public override void _Ready()
    {
        if (PlayerSprite == null)
        {
            GD.PrintErr("CRITICAL: Sprite node not found!");
        }
        PlayerSprite.AnimationFinished += OnAnimationFinished;
    }

    private void OnAnimationFinished()
    {
        if (PlayerSprite.Animation == "Land")
        {
            _isLanding = false;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        float currentGravity = GetGravity().Y;

        // 1. Gravity & Coyote Time Logic
        if (IsOnFloor())
        {
            _coyoteCounter = CoyoteTime; // Reset timer on ground
        }
        else
        {
            _coyoteCounter -= (float)delta; // Count down mercy window
            
            // Apply heavier gravity when falling
            float finalGravity = (velocity.Y > 0) ? currentGravity * FallGravityMultiplier : currentGravity;
            velocity.Y += finalGravity * (float)delta;
            
            _wasInAir = true;
        }

        // 2. Jump Input & Buffering
        if (Input.IsActionJustPressed("ui_accept")) _jumpBufferCounter = JumpBufferTime;
        else _jumpBufferCounter -= (float)delta;

        // Jump if buffer active AND we were recently on the floor (Coyote Time)
        if (_jumpBufferCounter > 0 && _coyoteCounter > 0)
        {
            velocity.Y = JumpVelocity;
            _jumpBufferCounter = 0;
            _coyoteCounter = 0; // Prevent infinite mid-air jumps
            _isLanding = false; // Cancel landing lock if we jump immediately
        }

        if (Input.IsActionJustReleased("ui_accept") && velocity.Y < 0)
        {
            velocity.Y *= JumpCutValue; 
        }

        // 3. Movement & Flipping (Slightly slowed during landing)
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        float moveSpeed = _isLanding ? Speed * 0.7f : Speed;

        if (direction.X != 0) PlayerSprite.FlipH = direction.X < 0;

        if (direction != Vector2.Zero) velocity.X = direction.X * moveSpeed;
        else velocity.X = Mathf.MoveToward(Velocity.X, 0, moveSpeed);

        // 4. Animation Priority System
        HandleAnimations(velocity);

        Velocity = velocity;
        MoveAndSlide();
    }

    private void HandleAnimations(Vector2 velocity)
    {
        if (PlayerSprite == null) return;

        if (IsOnFloor() && _wasInAir)
        {
            if (PlayerSprite.SpriteFrames.HasAnimation("Land"))
            {
                _isLanding = true;
                PlayerSprite.Play("Land");
            }
            _wasInAir = false;
        }

        if (!_isLanding)
        {
            string nextAnim = "Idle_Animation";

            if (!IsOnFloor())
            {
                if (velocity.Y < -50) nextAnim = "Jump_Rise";
                else if (velocity.Y > 50) nextAnim = "Jump_Fall";
                else nextAnim = "Jump_Apex";
            }
            else if (Mathf.Abs(velocity.X) > 1.0f)
            {
                nextAnim = PlayerSprite.SpriteFrames.HasAnimation("Run") ? "Run" : "Idle_Animation";
            }

            if (PlayerSprite.SpriteFrames.HasAnimation(nextAnim) && PlayerSprite.Animation != nextAnim)
            {
                PlayerSprite.Play(nextAnim);
            }
        }
    }
}