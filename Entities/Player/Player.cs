using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [ExportGroup("Animation Buffers")]
    [Export] public float FallAnimationDelay = 0.4f; 
    private float _fallCounter = 0;

    [ExportGroup("Fall Damage")]
    [Export] public float LethalFallDistance = 2160.0f; // 2 screens (1080 * 2)
    private float _fallStartY = 0f;
    private bool _isTrackingFall = false;

    [ExportGroup("Movement")]
    [Export] public float EnemyBounceForce = -600.0f;
    [Export] public float MaxLandingStun = 0.2f; 
    [Export] public float WalkSpeed = 300.0f;
    [Export] public float RunSpeed = 650.0f;
    [Export] public float Acceleration = 25.0f; 
    [Export] public float JumpVelocity = -550.0f;
    [Export] public float JumpCutValue = 0.5f;
    [Export] public float FallGravityMultiplier = 2.5f;
    [Export] public float LandingThreshold = 500.0f;

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
    private float _landingTimer = 0f;
    private bool _justFinishedApex = false;
    private bool _usingDoubleJumpSet = false;
    private bool _isDoubleJumpStarting = false;
    private bool _isApexLocked = false;
    private bool _hasFloatedThisJump = false; 
    private float _floatTimer = 0f;
    private Camera2D _childCamera;
    private bool _isRunning = false;

    private Label _debugLabel;   
    [Signal] public delegate void HealthChangedEventHandler(int newHealth);

    public override void _Ready()
    {
        _debugLabel = GetNodeOrNull<Label>("%DebugStateLabel");
        _currentHealth = MaxHealth;
        _childCamera = GetNodeOrNull<Camera2D>("Camera2D");
        PlayerSprite.AnimationFinished += OnAnimationFinished;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsKeyPressed(Key.Key1)) TakeDamage(20);
        if (Input.IsKeyPressed(Key.Key2)) TriggerRevive();

        Vector2 velocity = Velocity;
        float currentGravity = GetGravity().Y;

        if (_isDead) { HandleDeathState(delta, currentGravity); return; }

        // --- 1. THE FLOOR CHECK (Strict Reset) ---
        if (IsOnFloor())
        {
            _jumpCount = 0; 
            _coyoteCounter = CoyoteTime;
            _isDoubleJumpStarting = false;
            _isApexLocked = false;
            _hasFloatedThisJump = false;
            _usingDoubleJumpSet = false; 
            _floatTimer = 0;
            
            if (_isTrackingFall)
            {
                float totalFallDistance = GlobalPosition.Y - _fallStartY;
                if (totalFallDistance > LethalFallDistance) TriggerDeath();
                else if (_wasInAir && _previousYVelocity > LandingThreshold)
                {
                    _isLanding = true;
                    _landingTimer = MaxLandingStun;
                    string landAnim = _usingDoubleJumpSet ? "DoubleJump_Land" : "Land";
                    if (PlayerSprite.SpriteFrames.HasAnimation(landAnim)) PlayerSprite.Play(landAnim);
                }
                _isTrackingFall = false;
            }

            _wasInAir = false;
            velocity.Y = 10.0f; // Ground pressure to keep IsOnFloor stable
        }
        // --- 2. THE AIRBORNE CHECK (Includes touching walls) ---
        else
        {
            _coyoteCounter -= (float)delta;
            HandleAirPhysics(ref velocity, currentGravity, (float)delta);

            // Track Lethal Fall Height
            if (velocity.Y > 0 && !_isTrackingFall)
            {
                _fallStartY = GlobalPosition.Y;
                _isTrackingFall = true;
            }

            if (_isTrackingFall)
            {
                float currentFallDistance = GlobalPosition.Y - _fallStartY;
                if (currentFallDistance > LethalFallDistance)
                {
                    TriggerDeath();
                    _isTrackingFall = false;
                }
            }
        }

        // --- 3. INPUT & MOVEMENT ---
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

        // --- 4. APPLY PHYSICS & COLLISION ---
        HandleAnimations(velocity);
        
        _previousYVelocity = velocity.Y; 
        Velocity = velocity;
        MoveAndSlide();
        
        // Final Sync for next frame and Bounce Check
        velocity = Velocity; 

       // --- ENEMY BOUNCE LOGIC ---
    for (int i = 0; i < GetSlideCollisionCount(); i++)
    {
        KinematicCollision2D collision = GetSlideCollision(i);
        Node collider = (Node)collision.GetCollider();

        if (collider.IsInGroup("Enemies"))
        {
            // NEW: Added 'velocity.Y >= 0' check. 
            // This prevents the loop by ignoring the rat while Dimi is already flying up.
            if (collision.GetNormal().Y < -0.5f && velocity.Y >= 0)
            {
                // 1. Apply the pop
                velocity.Y = EnemyBounceForce; 

                // 2. PHYSICAL SEPARATION:
                // Move Dimi slightly up so he isn't 'touching' the rat on the next frame.
                GlobalPosition = new Vector2(GlobalPosition.X, GlobalPosition.Y - 2.0f);

                // 3. State resets
                _isLanding = false;
                _wasInAir = true; 
                _isTrackingFall = false;
                _jumpCount = 1;              
                _hasFloatedThisJump = false; 

                // 4. Animation Feedback
                PlayerSprite.Play(_usingDoubleJumpSet ? "DoubleJump_Rise" : "Jump_Rise");
            }
        }
    }
        Velocity = velocity; 

        if (_debugLabel != null)
        {
            _debugLabel.Text = $"State: {GetCurrentState()}\nVel: {Velocity.X:F0}, {Velocity.Y:F0}";
        }
    }

    private void HandleAirPhysics(ref Vector2 velocity, float gravity, float delta)
    {
        if (_jumpCount == MaxJumps && !_hasFloatedThisJump && Mathf.Abs(velocity.Y) < ApexTriggerRange && !IsOnCeiling())
        {
            _isApexLocked = true;
            _isDoubleJumpStarting = false; 
            _floatTimer = MaxFloatTime;
            _hasFloatedThisJump = true;
            velocity.Y = 0; 
            _fallCounter = 0;
        }

        if (_isApexLocked)
        {
            velocity.Y = 0; 
            _floatTimer -= delta;
            if (_floatTimer <= 0 || IsOnCeiling()) 
            {
                _isApexLocked = false;
                _fallStartY = GlobalPosition.Y;
                _justFinishedApex = true; 
            }
        }
        else
        {
            float finalGravity = (velocity.Y > 0) ? gravity * FallGravityMultiplier : gravity;
            velocity.Y += finalGravity * delta;
        }
        
        if (velocity.Y > 5.0f) _wasInAir = true; 
    }

    private void HandleHorizontalMovement(ref Vector2 velocity, float delta)
    {
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        if (_isLanding)
        {
            if (Mathf.Abs(direction.X) > 0.1f) _isLanding = false;
            _landingTimer -= delta;
            if (_landingTimer <= 0) _isLanding = false;
        }
        
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

        if (IsOnFloor())
        {
            _isDoubleJumpStarting = false;
            _isApexLocked = false;
            _fallCounter = 0f;
            
            if (Mathf.Abs(velocity.X) < 5.0f && !_isLanding)
                PlayerSprite.Play("Idle_Animation");
            else if (!_isLanding)
            {
                string nextAnim = (Mathf.Abs(velocity.X) > WalkSpeed + 50.0f) ? "Sprint" : "Run";
                PlayerSprite.Play(nextAnim);
            }
        }
        else // Airborne
        {
            if (_isApexLocked) { PlayerSprite.Play("DoubleJump_Apex"); return; }
            if (_isDoubleJumpStarting) return;

            string prefix = _usingDoubleJumpSet ? "DoubleJump_" : "Jump_";
            
            if (velocity.Y <= 5.0f) 
            {
                _fallCounter = 0f; 
                PlayerSprite.Play(prefix + "Rise");
            }
            else // Falling
            {
                _fallCounter += (float)GetProcessDeltaTime();
                if (_fallCounter > FallAnimationDelay || velocity.Y > 400.0f || _justFinishedApex)
                {
                    PlayerSprite.Play(prefix + "Fall");
                    _justFinishedApex = false; 
                }
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

    private string GetCurrentState()
    {
        if (_isDead) return "DEAD";
        if (_isHurt) return "HURT";
        if (_isLanding) return "LANDING";
        if (!IsOnFloor())
        {
            if (_isApexLocked) return "APEX_FLOAT";
            if (_isDoubleJumpStarting) return "DOUBLE_JUMP_RISE";
            return Velocity.Y < 0 ? "JUMPING" : "FALLING";
        }
        if (_isRunning && Mathf.Abs(Velocity.X) > WalkSpeed) return "SPRINTING";
        if (Mathf.Abs(Velocity.X) > 5.0f) return "RUNNING";
        return "IDLE";
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

    private void UpdateDebugUI()
    {
        if (_debugLabel != null)
        {
            _debugLabel.Text = $"State: {GetCurrentState()}\nVel: {Velocity.X:F0}, {Velocity.Y:F0}";
        }
    }
}