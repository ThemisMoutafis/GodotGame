using Godot;
using System.Collections.Generic;

public partial class AmbientManager : Node
{
    [Export] public AudioStream[] AmbientSounds; // Drag your 3 sounds here in the Inspector
    [Export] public Vector2 DelayRange = new Vector2(10.0f, 30.0f); // Min and Max seconds between sounds

    private AudioStreamPlayer _audioPlayer;
    private Timer _timer;

    public override void _Ready()
    {
        _audioPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        _timer = GetNode<Timer>("Timer");

        // Connect the timer signal
        _timer.Timeout += OnTimerTimeout;
        
        // Start the first countdown
        ScheduleNextSound();
    }

    private void OnTimerTimeout()
    {
        if (AmbientSounds == null || AmbientSounds.Length == 0) return;

        // 1. Pick a random sound from the array
        long index = GD.Randi() % AmbientSounds.Length;
        _audioPlayer.Stream = AmbientSounds[index];

        // 2. Earthy Randomization
        _audioPlayer.PitchScale = (float)GD.RandRange(0.8, 1.2);
        _audioPlayer.VolumeDb = (float)GD.RandRange(-10.0, -5.0); // Keep flavor sounds subtle

        _audioPlayer.Play();

        // 3. Queue the next one
        ScheduleNextSound();
    }

    private void ScheduleNextSound()
    {
        float nextDelay = (float)GD.RandRange(DelayRange.X, DelayRange.Y);
        _timer.Start(nextDelay);
    }
}