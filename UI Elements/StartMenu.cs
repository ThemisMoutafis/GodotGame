using Godot;
using System;
using System.Collections.Generic;

public partial class StartMenu : CanvasLayer
{
    private int _selectedIndex = 0;
    private List<Label> _menuOptions = new List<Label>();
    
    // Configure colors for selection
    private readonly Color _selectedColor = new Color(1, 1, 0); // Yellow
    private readonly Color _defaultColor = new Color(1, 1, 1);  // White

    public override void _Ready()
    {
        // Get our labels from the VBox
        var vbox = GetNode<VBoxContainer>("CenterContainer/VBoxContainer");
        foreach (Node child in vbox.GetChildren())
        {
            if (child is Label label)
            {
                _menuOptions.Add(label);
            }
        }
        
        UpdateMenuVisuals();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_up"))
        {
            _selectedIndex = Mathf.PosMod(_selectedIndex - 1, _menuOptions.Count);
            UpdateMenuVisuals();
        }
        else if (@event.IsActionPressed("ui_down"))
        {
            _selectedIndex = Mathf.PosMod(_selectedIndex + 1, _menuOptions.Count);
            UpdateMenuVisuals();
        }
        else if (@event.IsActionPressed("ui_accept"))
        {
            HandleSelection();
        }
    }

  private void UpdateMenuVisuals()
{
    for (int i = 0; i < _menuOptions.Count; i++)
    {
        // Use .ToString() to force the correct ToUpper() overload
        string cleanName = _menuOptions[i].Name.ToString().ToUpper();

        if (i == _selectedIndex)
        {
            _menuOptions[i].Modulate = _selectedColor;
            _menuOptions[i].Text = "> " + cleanName + " <";
            
            // Re-centering pivot so the scale looks natural
            _menuOptions[i].PivotOffset = _menuOptions[i].Size / 2;
            _menuOptions[i].Scale = new Vector2(1.1f, 1.1f);
        }
        else
        {
            _menuOptions[i].Modulate = _defaultColor;
            _menuOptions[i].Text = cleanName;
            _menuOptions[i].Scale = new Vector2(1.0f, 1.0f);
        }
    }
}

    private void HandleSelection()
    {
        switch (_selectedIndex)
        {
            case 0: // Start
                GD.Print("Starting Game...");
                // Change to your actual game scene path
                GetTree().ChangeSceneToFile("res://Levels/SewersMap.tscn");
                break;
                
            case 1: // Gallery
                GD.Print("Gallery not implemented yet.");
                // Add a little shake or sound effect to show it's locked
                break;
                
            case 2: // Exit
                GD.Print("Exiting...");
                GetTree().Quit();
                break;
        }
    }
}