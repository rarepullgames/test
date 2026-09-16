using Godot;
using System;

public partial class GlobalDebug : Node
{

	public override void _Process(double delta)
	{
		if (Input.MouseMode == Input.MouseModeEnum.Visible && Input.IsActionJustPressed("M1"))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		if (Input.MouseMode == Input.MouseModeEnum.Captured && Input.IsActionJustPressed("esc"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}
}
