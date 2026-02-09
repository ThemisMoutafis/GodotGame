using Godot;
using System;
using System.Collections.Generic;

public partial class GlobalPause : CanvasLayer
{
    private int _selectedIndex = 0;
    private List<Label> _options = new List<Label>();
    private bool _isPaused = false;

    public override void _Ready()
    {
        // Hide the menu at the start
        Visible = false;

        var vbox = GetNode<VBoxContainer>("CenterContainer/VBoxContainer");
        foreach (Node child in vbox.GetChildren())
        {
            if (child is Label label) _options.Add(label);
        }
        UpdateVisuals();
    }

    public override void _Input(InputEvent @event)
{
    // 1. Scene Guard
    // Check if the tree or current scene is null to avoid the crash
    if (GetTree() == null || GetTree().CurrentScene == null) return;

    // 1. Scene Guard
    if (GetTree().CurrentScene.SceneFilePath.Contains("start_menu.tscn")) return;

    // 2. The Toggle (Escape)
    if (@event.IsActionPressed("ui_cancel"))
    {
        GetViewport().SetInputAsHandled(); // 🚩 Stop the event from bubbling
        TogglePause();
        return; 
    }

    // 3. Navigation (Only if paused)
    if (!_isPaused) return;

    if (@event.IsActionPressed("ui_up"))
    {
        GetViewport().SetInputAsHandled();
        _selectedIndex = Mathf.PosMod(_selectedIndex - 1, _options.Count);
        UpdateVisuals();
    }
    else if (@event.IsActionPressed("ui_down"))
    {
        GetViewport().SetInputAsHandled();
        _selectedIndex = Mathf.PosMod(_selectedIndex + 1, _options.Count);
        UpdateVisuals();
    }
    else if (@event.IsActionPressed("ui_accept"))
    {
        GetViewport().SetInputAsHandled();
        HandleSelection();
    }
}

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        GetTree().Paused = _isPaused;
        Visible = _isPaused;
        
        if (_isPaused) _selectedIndex = 0; // Reset index to Resume
        UpdateVisuals();
    }

    private void UpdateVisuals()
{
    for (int i = 0; i < _options.Count; i++)
    {
        string labelBaseName = _options[i].Name.ToString().ToUpper();
        string displayText = labelBaseName;

        // If it's the WindowMode label, show the current status
        if (labelBaseName == "WINDOW MODE")
        {
            var currentMode = DisplayServer.WindowGetMode();
            displayText = currentMode == DisplayServer.WindowMode.Fullscreen ? "MODE: FULLSCREEN" : "MODE: WINDOWED";
        }

        if (i == _selectedIndex)
        {
            _options[i].Modulate = new Color(1, 1, 0); // Yellow
            _options[i].Text = "> " + displayText + " <";
        }
        else
        {
            _options[i].Modulate = new Color(1, 1, 1); // White
            _options[i].Text = displayText;
        }
    }
}

    private void HandleSelection()
    {
        switch (_selectedIndex)
        {
            case 0: // Resume
                TogglePause();
                break;
            case 1: // Settings
                GD.Print("Settings coming soon...");
                break;
            case 2: // Toggle Window Mode
                ToggleWindowMode(); 
                break; 
            case 3: // Quit
                TogglePause(); // IMPORTANT: Unpause before leaving
                GetTree().ChangeSceneToFile("res://UI Elements/start_menu.tscn");
                break;
        }
    }
    private void ToggleWindowMode()
{
    // Check current mode and flip it
    if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen)
    {
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
    }
    else
    {
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
    }
    
    // Refresh visuals to show the new state
    UpdateVisuals();
}
}