using Godot;
using System;

public partial class LanternFlicker : PointLight2D
{
    [Export] public float BaseEnergy = 2.5f;
    [Export] public float FlickerAmount = 0.2f; // How intense the flicker is
    [Export] public float FlickerSpeed = 15.0f; // How fast the flicker is
    
    [Export] public float BaseScale = 1.0f;
    [Export] public float ScaleFlickerAmount = 0.05f; // Makes shadows "wobble"

    private float _time = 0.0f;
    private FastNoiseLite _noise = new FastNoiseLite();

    public override void _Ready()
    {
        // Randomize the noise seed so every lantern flickers differently
        _noise.Seed = (int)GD.Randi();
		_noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _noise.Frequency = 0.3f;
		
    }

  public override void _Process(double delta)
{
    if (!Enabled) return;

    _time += (float)delta * FlickerSpeed;
    float noiseVal = _noise.GetNoise1D(_time);
    
    // Normalize noise to 0.0 -> 1.0
    float normalizedNoise = (noiseVal + 1.0f) / 2.0f; 

    // CALCULATING THE PULSE
    // At 2.5 Energy, a 10% flicker (0.25) is plenty to see.
    // This keeps the light between 2.25 and 2.75 if BaseEnergy is 2.5.
    float flickerRange = BaseEnergy * 0.1f; 
    float minEnergy = BaseEnergy - flickerRange;
    float maxEnergy = BaseEnergy + flickerRange;

    Energy = Mathf.Lerp(minEnergy, maxEnergy, normalizedNoise);
    
    // Maintain a subtle scale wobble
    TextureScale = Mathf.Lerp(BaseScale * 0.97f, BaseScale * 1.03f, normalizedNoise);
}
}