using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public float MoveSpeed = 5.0f;
	[Export] public float JumpVelocity = 5.0f;
	[Export] public float Acceleration = 35.0f;
	[Export] public float Braking = 50.0f;

	private Vector2 movementInput = Vector2.Zero;

	public override void _Process(double delta)
	{
		movementInput = Input.GetVector(
			"move_left",
			"move_right",
			"move_forward",
			"move_backwards"
		);
	}


	public override void _PhysicsProcess(double delta)
	{
		Vector3 direction = Basis * new Vector3(
			movementInput.X, 0, movementInput.Y
		);

		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}
		else if (Input.IsActionJustPressed("jump"))
		{
			velocity.Y = JumpVelocity;
		}

		Vector3 horizontalVelocity = new Vector3(
	velocity.X, 0, velocity.Z
);

		Vector3 targetVelocity = direction * MoveSpeed;

		float rate = movementInput.IsZeroApprox()
			? Braking
			: Acceleration;

		horizontalVelocity = horizontalVelocity.MoveToward(
			targetVelocity,
			rate * (float)delta
		);

		velocity.X = horizontalVelocity.X;
		velocity.Z = horizontalVelocity.Z;

		Velocity = velocity;
		MoveAndSlide();
	}
}
