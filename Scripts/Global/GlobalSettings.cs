using Godot;
using System;

public partial class GlobalSettings : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		LoadSettingsOnStart();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void LoadSettingsOnStart()
	{
		GetWindow().Mode = Window.ModeEnum.Windowed;
		GetWindow().Size = new Vector2I(1600, 900);
		GetWindow().Unresizable = false;
	}
}
