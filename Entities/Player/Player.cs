using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [ExportGroup("Movement")]
    [Export] public float Speed = 300.0f;
    [Export] public float JumpVelocity = -400.0f;
    [Export] public float JumpCutValue = 0.5f;
    [Export] public float FallGravityMultiplier = 2.0f;

    [ExportGroup("Double Jump Settings")]
    [Export] public int MaxJumps = 2; 
    [Export] public float MaxFloatTime = 0.2f; // How long Dimi hangs in the air
    [Export] public float ApexCatchThreshold = -10.0f; // Sensitivity of the "Peak" detection

    [ExportGroup("Forgiveness")]
    [Export] public float JumpBufferTime = 0.15f;
    [Export] public float CoyoteTime = 0.15f;

    [ExportGroup("Nodes")]
    [Export] public AnimatedSprite2D PlayerSprite;

    private float _jumpBufferCounter = 0;
    private int _jumpCount = 0;
    private float _coyoteCounter = 0; 
    private bool _wasInAir = false;
    private bool _isLanding = false;

    private bool _usingDoubleJumpSet = false;
    private bool _isDoubleJumpStarting = false;
    private bool _isApexLocked = false;
    private bool _hasFloatedThisJump = false; 
    private float _floatTimer = 0f;
  

    public override void _Ready()
    {
        if (PlayerSprite == null) GD.PrintErr("CRITICAL: Sprite node not found!");
        PlayerSprite.AnimationFinished += OnAnimationFinished;
    }

    private void OnAnimationFinished()
    {
        // ONLY handle landing here.
        if (PlayerSprite.Animation == "Land" || PlayerSprite.Animation == "DoubleJump_Land")
            _isLanding = false;

        // We removed the Apex trigger from here to prevent the "Double Play" bug.
        if (PlayerSprite.Animation == "DoubleJump_Rise")
            _isDoubleJumpStarting = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        float currentGravity = GetGravity().Y;

        if (IsOnFloor())
        {
            _coyoteCounter = CoyoteTime;
            _jumpCount = 0; 
            _isDoubleJumpStarting = false;
            _isApexLocked = false;
            _hasFloatedThisJump = false;
            _floatTimer = 0;
        }
        else
        {
            _coyoteCounter -= (float)delta;

            // THE INSTANT CATCHER: Happens the MOMENT you stop moving up.
            // This bypasses animation lag entirely.
            if (_jumpCount == 2 && !_hasFloatedThisJump && velocity.Y >= -10.0f)
            {
                if (!IsOnCeiling() && !IsOnWall())
                {
                    _isApexLocked = true;
                    _isDoubleJumpStarting = false; // Kill the Rise lock immediately
                    _floatTimer = MaxFloatTime;
                    _hasFloatedThisJump = true;
                    velocity.Y = 0; 
                    PlayerSprite.Play("DoubleJump_Apex"); 
                }
            }

            if (_isApexLocked && _floatTimer > 0 && !IsOnCeiling() && !IsOnWall())
            {
                velocity.Y = 0; 
                _floatTimer -= (float)delta;
            }
            else
            {
                _isApexLocked = false;
                float finalGravity = (velocity.Y > 0) ? currentGravity * FallGravityMultiplier : currentGravity;
                velocity.Y += finalGravity * (float)delta;
            }
            _wasInAir = true;
        }

        HandleJumpInput(ref velocity);
        HandleHorizontalMovement(ref velocity);
        HandleAnimations(velocity);

        Velocity = velocity;
        MoveAndSlide();
    }

    private void HandleJumpInput(ref Vector2 velocity)
    {
        if (Input.IsActionJustPressed("ui_accept")) _jumpBufferCounter = JumpBufferTime;
        else _jumpBufferCounter -= (float)GetProcessDeltaTime();

        if (_jumpBufferCounter > 0 && _coyoteCounter > 0)
        {
            velocity.Y = JumpVelocity;
            _jumpCount = 1;
            _jumpBufferCounter = 0;
            _coyoteCounter = 0;
            _usingDoubleJumpSet = false;
            _isLanding = false;
            _hasFloatedThisJump = false;
        }
        else if (Input.IsActionJustPressed("ui_accept") && _jumpCount > 0 && _jumpCount < MaxJumps)
        {
            velocity.Y = JumpVelocity;
            _jumpCount++;
            _usingDoubleJumpSet = true;
            _isLanding = false;
            _isDoubleJumpStarting = true;
            _hasFloatedThisJump = false; 
            PlayerSprite.Play("DoubleJump_Rise");
        }

        if (Input.IsActionJustReleased("ui_accept") && velocity.Y < 0)
            velocity.Y *= JumpCutValue;
    }

    private void HandleHorizontalMovement(ref Vector2 velocity)
    {
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        float moveSpeed = _isLanding ? Speed * 0.7f : Speed;
        if (direction.X != 0) PlayerSprite.FlipH = direction.X < 0;
        if (direction != Vector2.Zero) velocity.X = direction.X * moveSpeed;
        else velocity.X = Mathf.MoveToward(Velocity.X, 0, moveSpeed);
    }

    private void HandleAnimations(Vector2 velocity)
    {
        if (PlayerSprite == null) return;

        if (IsOnFloor() || IsOnWall() || IsOnCeiling())
        {
            _isDoubleJumpStarting = false;
            _isApexLocked = false;
        }

        // Priority 1: Locks
        if (_isLanding || _isDoubleJumpStarting || _isApexLocked) return;

        // Priority 2: Landing
        if (IsOnFloor() && _wasInAir)
        {
            string landAnim = _usingDoubleJumpSet ? "DoubleJump_Land" : "Land";
            _isLanding = true;
            PlayerSprite.Play(landAnim);
            _wasInAir = false;
            return;
        }

        // Priority 3: Air/Ground
        if (!IsOnFloor())
        {
            string prefix = _usingDoubleJumpSet ? "DoubleJump_" : "Jump_";
            // Strict Toggle: No "Apex" state here.
            if (velocity.Y < 0) PlayerSprite.Play(prefix + "Rise");
            else PlayerSprite.Play(prefix + "Fall");
        }
        else
        {
            string nextAnim = Mathf.Abs(velocity.X) > 1.0f ? "Run" : "Idle_Animation";
            if (PlayerSprite.Animation != nextAnim) PlayerSprite.Play(nextAnim);
        }
    }
}