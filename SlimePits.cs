using Godot; // This is the line you are missing
using System;
public partial class SlimePits : Area2D 
{
    private bool _isDimiInside = false;
    private Player _playerRef;
    private float _damageTimer = 0f;
    private float _damageInterval = 0.6f; // Slightly longer than the damage pulse

    // 1. Initial Contact
    private void _on_body_entered(Node2D body)
{
    if (body is Player player)
    {
        _isDimiInside = true;
        _playerRef = player;
        
        // Tell Dimi he is now green
        _playerRef.IsInSlime = true;
        
        _playerRef.TakeDamageOverTime(5); 
    }
}

    // 2. Continuous Burn
   public override void _PhysicsProcess(double delta)
    {
        if (_isDimiInside && _playerRef != null)
        {
            _damageTimer += (float)delta;
            if (_damageTimer >= _damageInterval)
            {
                _playerRef.TakeDamageOverTime(5); 
                _damageTimer = 0f;
            }
        }
    }

    private void _on_body_exited(Node2D body)
{
    if (body is Player player)
    {
        _isDimiInside = false;
        
        // Remove the green tint
        player.IsInSlime = false;
        
        _playerRef = null;
        _damageTimer = 0f;
    }
}
}