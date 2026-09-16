using Godot;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Movement")]
	[Export] public float MoveSpeed = 5.0f;
	[Export] public float JumpVelocity = 5.0f;
	[Export] public float Acceleration = 35.0f;
	[Export] public float Braking = 50.0f;

	[ExportGroup("Resizing")]
	[Export] public float ResizeSpeed = 0.5f;
	[Export] public float MinSize = 0.25f;
	[Export] public float MaxSize = 3.0f;

	private float currentSize = 1.0f;
	private float originalHalfHeight;

	private Vector2 movementInput = Vector2.Zero;

	public override void _Ready()
	{
		var collider = GetNode<CollisionShape3D>("CollisionShape3D");
		var capsule = (CapsuleShape3D)collider.Shape;

		originalHalfHeight = capsule.Height * 0.5f;
	}

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
		UpdateSize(delta);

		Vector3 direction = Basis.Orthonormalized() * new Vector3(movementInput.X, 0, movementInput.Y);

		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}
		else if (Input.IsActionJustPressed("jump"))
		{
			velocity.Y = JumpVelocity;
		}

		Vector3 horizontalVelocity = new Vector3(velocity.X, 0, velocity.Z);

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

	private void UpdateSize(double delta)
	{
		float input = Input.GetAxis("Q", "E");

		float newSize = Mathf.Clamp(
			currentSize + input * ResizeSpeed * (float)delta,
			MinSize,
			MaxSize
		);

		float sizeChange = newSize - currentSize;

		Scale = Vector3.One * newSize;

		// Your capsule is centered on the player.
		// Move its center to keep the bottom at the same height.
		Position += Vector3.Up * originalHalfHeight * sizeChange;

		currentSize = newSize;
	}
}
