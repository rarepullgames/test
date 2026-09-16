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

	private float targetSize = 1.0f;
	private float originalRadius;

	private CapsuleShape3D growthCheckShape;
	private PhysicsShapeQueryParameters3D growthQuery;

	public override void _Ready()
	{
		AddToGroup("player");

		var collider = GetNode<CollisionShape3D>("CollisionShape3D");
		var capsule = (CapsuleShape3D)collider.Shape;

		originalHalfHeight = capsule.Height * 0.5f;
		originalRadius = capsule.Radius;

		currentSize = Scale.X;
		targetSize = currentSize;

		// Separate shape used only for checking available space.
		growthCheckShape = new CapsuleShape3D();

		growthQuery = new PhysicsShapeQueryParameters3D
		{
			Shape = growthCheckShape,
			CollisionMask = CollisionMask,
			CollideWithBodies = true,
			CollideWithAreas = false,
			Margin = 0.0f,
			Exclude = new Godot.Collections.Array<Rid>
		{
			GetRid()
		}
		};
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
		float step = ResizeSpeed * (float)delta;
		float input = Input.GetAxis("Q", "E");

		// Keep the Q/E testing controls.
		targetSize = Mathf.Clamp(
			targetSize + input * step,
			MinSize,
			MaxSize
		);

		float nextSize = Mathf.MoveToward(
			currentSize,
			targetSize,
			step
		);

		if (Mathf.IsEqualApprox(nextSize, currentSize))
			return;

		if (nextSize > currentSize && !CanFitSize(nextSize))
		{
			// Find a smaller growth step that still fits.
			float safeSize = currentSize;
			float blockedSize = nextSize;

			for (int i = 0; i < 6; i++)
			{
				float middle = (safeSize + blockedSize) * 0.5f;

				if (CanFitSize(middle))
					safeSize = middle;
				else
					blockedSize = middle;
			}

			nextSize = safeSize;
		}

		if (Mathf.IsEqualApprox(nextSize, currentSize))
			return;

		float sizeChange = nextSize - currentSize;

		Scale = Vector3.One * nextSize;

		// Preserve the height of the capsule's bottom.
		GlobalPosition += Vector3.Up * originalHalfHeight * sizeChange;

		currentSize = nextSize;
	}

	public void AddGrowth(float amount)
	{
		targetSize = Mathf.Clamp(
			targetSize + Mathf.Max(amount, 0.0f),
			MinSize,
			MaxSize
		);
	}

	private bool CanFitSize(float size)
	{
		float radius = originalRadius * size;
		float height = originalHalfHeight * 2.0f * size;

		// Tiny tolerance so normal floor contact doesn't block growth.
		float skin = Mathf.Min(0.001f, radius * 0.01f);

		growthCheckShape.Radius = radius - skin;
		growthCheckShape.Height = height - skin * 2.0f;

		Vector3 candidatePosition = GlobalPosition
			+ Vector3.Up * originalHalfHeight * (size - currentSize);

		growthQuery.Transform = new Transform3D(
			GlobalBasis.Orthonormalized(),
			candidatePosition
		);

		growthQuery.CollisionMask = CollisionMask;

		var space = GetWorld3D().DirectSpaceState;

		// One overlap is enough to reject this size.
		return space.IntersectShape(growthQuery, 1).Count == 0;
	}
}
