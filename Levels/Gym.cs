using Godot;
using System;

public partial class Gym : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public override void _Input(InputEvent @event)
{
    if (@event.IsActionPressed("ui_cancel")) // Usually the 'Esc' key
    {
        // Toggle between Fullscreen and Windowed
        var mode = DisplayServer.WindowGetMode();
        if (mode == DisplayServer.WindowMode.Fullscreen)
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        else
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
    }
}
}
