using Godot;

public partial class Camera : Node3D
{
    [Export] public float MouseSensitivity = 0.003f;
    [Export] public float MinPitchDegrees = -89f;
    [Export] public float MaxPitchDegrees = 89f;


    private Vector2 mouseMovement = Vector2.Zero;



    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            return;
        }

        if (@event is InputEventMouseMotion mouseMotion)
        {
            mouseMovement += mouseMotion.ScreenRelative;
        }
    }

    public override void _Process(double delta)
    {
        GetParent<Node3D>().RotateY(-mouseMovement.X * MouseSensitivity);
        RotateX(-mouseMovement.Y * MouseSensitivity);

        mouseMovement = Vector2.Zero;
    }


}