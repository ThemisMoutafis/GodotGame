using Godot;
using System;

public partial class Player : CharacterBody2D
{
  [Export] public AnimatedSprite2D PlayerSprite;

    public override void _Ready()
    {
        GD.Print(">>> PLAYER SCRIPT IS RUNNING <<<");
      if (PlayerSprite == null)
    {
        GD.PrintErr("CRITICAL: Sprite node not found! Check the name in the Scene Tree.");
    }
    GD.Print("Available animations: ", string.Join(", ", PlayerSprite.SpriteFrames.GetAnimationNames()));
    }
    [Export] public float Speed = 300.0f;
    [Export] public float JumpVelocity = -400.0f;
    [Export] public float JumpCutValue = 0.5f;
    [Export] public float JumpBufferTime = 0.15f;
    private float _jumpBufferCounter = 0;
    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        // 1. Gravity: Use ProjectSettings so it matches Godot's defaults
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }

        if (Input.IsActionJustPressed("ui_accept"))
    {
        _jumpBufferCounter = JumpBufferTime; // Start the countdown
    }
    else
    {
        _jumpBufferCounter -= (float)delta; // Count down every frame
    }

    // 3. The Jump (Now checks the Buffer instead of JustPressed)
    if (_jumpBufferCounter > 0 && IsOnFloor())
    {
        velocity.Y = JumpVelocity;
        _jumpBufferCounter = 0; // Reset buffer so we don't double jump
    }

        if (Input.IsActionJustReleased("ui_accept") && velocity.Y < 0)
    {
        // Multiply velocity by 0.5 (or your JumpCutValue) to slow the ascent
        velocity.Y *= JumpCutValue; 
    }
        // 3. Horizontal Movement: Smoothly move toward the input direction
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if(direction.X <0 )
        {
            PlayerSprite.FlipH = true;
        }
        else if(direction.X >0)
        {
            PlayerSprite.FlipH = false;
        }
        if (direction != Vector2.Zero)
        {
            velocity.X = direction.X * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        }

        // 4. Animation Logic with Safety Checks
    // 4. Animation State Machine
    if (PlayerSprite != null)
    {
        string nextAnim;

        if (!IsOnFloor())
        {
            // State: AIR
            nextAnim = PlayerSprite.SpriteFrames.HasAnimation("Jump") ? "Jump" : "Idle_Animation";
        }
        else if (Mathf.Abs(velocity.X) > 1.0f) // Increased threshold to 1.0 to prevent jitter
        {
            // State: MOVING
            nextAnim = PlayerSprite.SpriteFrames.HasAnimation("Run") ? "Run" : "Idle_Animation";
        }
        else
        {
            // State: IDLE (The absolute fallback)
            nextAnim = "Idle_Animation";
        }

        // Only call Play if the animation isn't already playing (prevents restarting frame 1 every frame)
        if (PlayerSprite.Animation != nextAnim)
        {
            PlayerSprite.Play(nextAnim);
        }
    }

    Velocity = velocity;
    MoveAndSlide();
    }
}