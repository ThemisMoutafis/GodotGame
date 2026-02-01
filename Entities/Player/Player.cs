using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [ExportGroup("Movement")]
    [Export] public float WalkSpeed = 300.0f;
    [Export] public float RunSpeed = 650.0f;
    [Export] public float Acceleration = 25.0f; 
    [Export] public float JumpVelocity = -600.0f;
    [Export] public float JumpCutValue = 0.5f;
    [Export] public float FallGravityMultiplier = 2.0f;
    [Export] public float LandingThreshold = 150.0f;

    [ExportGroup("Health System")]
    [Export] public int MaxHealth = 100;
    [Export] public float IFrameDuration = 0.8f; 
    [Export] public Vector2 KnockbackForce = new Vector2(350, -300);
    private int _currentHealth;
    private bool _isInvincible = false;
    private bool _isHurt = false;

    [ExportGroup("Double Jump Settings")]
    [Export] public int MaxJumps = 2; 
    [Export] public float MaxFloatTime = 0.15f; 
    [Export] public float ApexTriggerRange = 40.0f;

    [ExportGroup("Camera Juice")]
    [Export] public float DeathShakeIntensity = 8.0f;
    [Export] public float DeathShakeDuration = 0.15f;
    [Export] public float RunZoomAmount = 0.9f; 

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
    private float _previousYVelocity = 0f;
    private bool _isDead = false;

    private bool _usingDoubleJumpSet = false;
    private bool _isDoubleJumpStarting = false;
    private bool _isApexLocked = false;
    private bool _hasFloatedThisJump = false; 
    private float _floatTimer = 0f;
    private Camera2D _childCamera;
    private bool _isRunning = false;

    [Signal] public delegate void HealthChangedEventHandler(int newHealth);

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _childCamera = GetNodeOrNull<Camera2D>("Camera2D");
        PlayerSprite.AnimationFinished += OnAnimationFinished;
    }

    public async void TakeDamage(int amount)
    {
        if (_isDead || _isInvincible) return;
        _currentHealth -= amount;
        EmitSignal(SignalName.HealthChanged, _currentHealth);
        _isInvincible = true;
        if (_currentHealth <= 0) { _currentHealth = 0; TriggerDeath(); return; }
        
        _isHurt = true;
        PlayerSprite.Modulate = new Color(10, 1, 1, 1); 
        float knockbackDir = PlayerSprite.FlipH ? 1 : -1;
        Velocity = new Vector2(KnockbackForce.X * knockbackDir, KnockbackForce.Y);
        
        if (PlayerSprite.SpriteFrames.HasAnimation("Hurt")) 
            PlayerSprite.Play("Hurt");
        
        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        _isHurt = false;
        ApplyHurtFlash();
        
        await ToSignal(GetTree().CreateTimer(IFrameDuration - 0.3f), "timeout");
        _isInvincible = false;
        PlayerSprite.Modulate = new Color(1, 1, 1, 1);
    }

    private void ApplyHurtFlash()
    {
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(PlayerSprite, "modulate", new Color(1, 1, 1, 1), 0.2f).SetTrans(Tween.TransitionType.Quad);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsKeyPressed(Key.Key1)) TakeDamage(20);
        if (Input.IsKeyPressed(Key.Key2)) TriggerRevive();

        Vector2 velocity = Velocity;
        float currentGravity = GetGravity().Y;

        if (_isDead) { HandleDeathState(delta, currentGravity); return; }

        if (IsOnFloor())
        {
            if (_wasInAir && _previousYVelocity > LandingThreshold)
            {
                _isLanding = true;
                string landAnim = _usingDoubleJumpSet ? "DoubleJump_Land" : "Land";
                if (PlayerSprite.SpriteFrames.HasAnimation(landAnim)) PlayerSprite.Play(landAnim);
            }
            _wasInAir = false;
            _coyoteCounter = CoyoteTime;
            _jumpCount = 0;
            _isDoubleJumpStarting = false;
            _isApexLocked = false;
            _hasFloatedThisJump = false;
            _usingDoubleJumpSet = false; 
            _floatTimer = 0;
        }
        else
        {
            _coyoteCounter -= (float)delta;
            HandleAirPhysics(ref velocity, currentGravity, (float)delta);
        }

        if (!_isHurt)
        {
            HandleJumpInput(ref velocity);
            HandleHorizontalMovement(ref velocity, (float)delta);
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Acceleration * 0.5f);
            float finalGravity = (velocity.Y > 0) ? currentGravity * FallGravityMultiplier : currentGravity;
            velocity.Y += finalGravity * (float)delta;
        }

        HandleAnimations(velocity);
        Velocity = velocity;
        MoveAndSlide();
        _previousYVelocity = velocity.Y;
    }

    private void HandleAirPhysics(ref Vector2 velocity, float gravity, float delta)
    {
        if (_jumpCount == MaxJumps && !_hasFloatedThisJump && Mathf.Abs(velocity.Y) < ApexTriggerRange)
        {
            if (!IsOnCeiling() && !IsOnWall())
            {
                _isApexLocked = true;
                _isDoubleJumpStarting = false; 
                _floatTimer = MaxFloatTime;
                _hasFloatedThisJump = true;
                velocity.Y = 0; 
                PlayerSprite.Play("DoubleJump_Apex"); 
            }
        }

        if (_isApexLocked)
        {
            velocity.Y = 0; 
            _floatTimer -= delta;
            if (_floatTimer <= 0 || IsOnCeiling()) _isApexLocked = false;
        }
        else
        {
            float finalGravity = (velocity.Y > 0) ? gravity * FallGravityMultiplier : gravity;
            velocity.Y += finalGravity * delta;
        }
        
        if (velocity.Y > LandingThreshold) _wasInAir = true;
    }

    private void HandleHorizontalMovement(ref Vector2 velocity, float delta)
    {
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        
        // Fix: Sprinting requires directional input
        _isRunning = Input.IsActionPressed("run") && Mathf.Abs(direction.X) > 0.1f;
        
        float currentMaxSpeed = _isRunning ? RunSpeed : WalkSpeed;
        float moveSpeed = _isLanding ? currentMaxSpeed * 0.7f : currentMaxSpeed;
        
        if (Mathf.Abs(direction.X) > 0.1f) 
        {
            PlayerSprite.FlipH = direction.X < 0;
            velocity.X = Mathf.MoveToward(velocity.X, direction.X * moveSpeed, Acceleration);
        }
        else 
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Acceleration);
            // Fix: Snap to zero to prevent animation loops
            if (Mathf.Abs(velocity.X) < 1.0f) velocity.X = 0;
        }

        if (_childCamera != null)
        {
            float targetZoom = (_isRunning && Mathf.Abs(velocity.X) > WalkSpeed) ? RunZoomAmount : 1.0f;
            _childCamera.Zoom = _childCamera.Zoom.Lerp(new Vector2(targetZoom, targetZoom), 0.1f);
        }
    }

    private void HandleJumpInput(ref Vector2 velocity)
    {
        if (Input.IsActionJustPressed("ui_accept")) _jumpBufferCounter = JumpBufferTime;
        else _jumpBufferCounter -= (float)GetProcessDeltaTime();

        if (_jumpBufferCounter > 0 && _coyoteCounter > 0)
        {
            velocity.Y = JumpVelocity;
            _jumpCount = 1; _jumpBufferCounter = 0; _coyoteCounter = 0;
            _usingDoubleJumpSet = false; _isLanding = false;
        }
        else if (Input.IsActionJustPressed("ui_accept") && _jumpCount > 0 && _jumpCount < MaxJumps)
        {
            velocity.Y = JumpVelocity;
            _jumpCount++;
            _usingDoubleJumpSet = true; 
            _isDoubleJumpStarting = true;
            _isApexLocked = false; 
            PlayerSprite.Play("DoubleJump_Rise");
        }
        if (Input.IsActionJustReleased("ui_accept") && velocity.Y < 0) velocity.Y *= JumpCutValue;
    }

    private void HandleAnimations(Vector2 velocity)
    {
        if (PlayerSprite == null || _isDead || _isHurt) return; 

        if (IsOnFloor() || IsOnWall())
        {
            _isDoubleJumpStarting = false;
            _isApexLocked = false;
        }

        if (_isApexLocked) return;

        if (!IsOnFloor())
        {
            if (_isDoubleJumpStarting) return;

            string prefix = _usingDoubleJumpSet ? "DoubleJump_" : "Jump_";
            
            if (velocity.Y < -5.0f) 
                PlayerSprite.Play(prefix + "Rise");
            else 
                PlayerSprite.Play(prefix + "Fall");
        }
        else
        {
            // Fix: Strict Idle priority with 5.0f threshold
            if (Mathf.Abs(velocity.X) < 5.0f && !_isLanding)
            {
                if (PlayerSprite.Animation != "Idle_Animation")
                {
                    PlayerSprite.Play("Idle_Animation");
                    PlayerSprite.SpeedScale = 1.0f;
                }
            }
            else if (!_isLanding)
            {
                // Decide between Sprint and Run based on speed
                string nextAnim = (Mathf.Abs(velocity.X) > WalkSpeed + 50.0f) ? "Sprint" : "Run";
                if (PlayerSprite.Animation != nextAnim) PlayerSprite.Play(nextAnim);
            }
        }
    }

    private void HandleDeathState(double delta, float gravity)
    {
        Vector2 velocity = Velocity; velocity.X = 0;
        if (!IsOnFloor()) velocity.Y += gravity * (float)delta;
        else { if (_previousYVelocity > LandingThreshold) { ApplyDeathShake(); _previousYVelocity = 0; } velocity.Y = 0; }
        Velocity = velocity; MoveAndSlide(); _previousYVelocity = velocity.Y; 
    }

    private void OnAnimationFinished()
    {
        if (PlayerSprite.Animation == "Land" || PlayerSprite.Animation == "DoubleJump_Land") _isLanding = false;
        if (PlayerSprite.Animation == "DoubleJump_Rise") _isDoubleJumpStarting = false;
    }

    private void ApplyDeathShake()
    {
        if (_childCamera == null) return;
        Tween tween = GetTree().CreateTween();
        for (int i = 0; i < 6; i++) {
            Vector2 shakeOffset = new Vector2((float)GD.RandRange(-DeathShakeIntensity, DeathShakeIntensity), (float)GD.RandRange(-DeathShakeIntensity, DeathShakeIntensity));
            tween.TweenProperty(_childCamera, "offset", shakeOffset, DeathShakeDuration / 6);
        }
        tween.TweenProperty(_childCamera, "offset", Vector2.Zero, 0.05f);
    }

    public void TriggerDeath()
    {
        if (_isDead) return;
        _isDead = true; _currentHealth = 0; EmitSignal(SignalName.HealthChanged, _currentHealth);
        PlayerSprite.Play("Death_Animation");
    }

    public void TriggerRevive()
    {
        _isDead = false; _isLanding = false; _isHurt = false; _currentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, _currentHealth); _isInvincible = false;
        PlayerSprite.Modulate = new Color(1, 1, 1, 1); PlayerSprite.Play("Idle_Animation");
    }
}