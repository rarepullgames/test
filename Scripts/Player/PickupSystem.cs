using Godot;

public partial class PickupSystem : Node
{
	[ExportGroup("Pickup")]
	[Export] public float PickupReach = 2.0f;

	[Export(PropertyHint.Layers3DPhysics)]
	public uint PickupMask = 1;

	[ExportGroup("Holding")]
	[Export] public float HoldDistance = 1.2f;
	[Export] public float HoldStrength = 80.0f;
	[Export] public float HoldDamping = 18.0f;
	[Export] public float ThrowSpeed = 8.0f;

	[ExportGroup("Held Rotation")]
	[Export] public float RotationStrength = 60.0f;
	[Export] public float RotationDamping = 16.0f;

	private Camera3D playerCamera;
	private CharacterBody3D player;

	private BaseItem targetedItem;
	private BaseItem heldItem;

	private float savedGravityScale;
	private Basis heldRotationOffset;
	private PhysicsRayQueryParameters3D rayQuery;

	public override void _Ready()
	{
		player = GetParent<CharacterBody3D>();
		playerCamera = GetNode<Camera3D>("../Camera3D");

		rayQuery = new PhysicsRayQueryParameters3D
		{
			CollisionMask = PickupMask,
			Exclude = new Godot.Collections.Array<Rid>
			{
				player.GetRid()
			}
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GodotObject.IsInstanceValid(heldItem))
		{
			if (Input.MouseMode != Input.MouseModeEnum.Captured)
			{
				DropItem();
			}
			else if (Input.IsActionJustPressed("M2"))
			{
				ThrowItem();
			}
			else if (Input.IsActionJustPressed("M1"))
			{
				DropItem();
			}
			else
			{
				UpdateHeldItem();
			}

			return;
		}

		heldItem = null;
		UpdateTarget();

		if (Input.MouseMode == Input.MouseModeEnum.Captured
			&& Input.IsActionJustPressed("M1"))
		{
			TryPickUp();
		}
	}

	private void UpdateTarget()
	{
		BaseItem nextItem = null;

		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			Vector3 origin = playerCamera.GlobalPosition;
			Vector3 forward = -playerCamera.GlobalBasis.Z.Normalized();

			float reach = PickupReach * player.GlobalBasis.X.Length();

			rayQuery.From = origin;
			rayQuery.To = origin + forward * reach;
			rayQuery.CollisionMask = PickupMask;

			var space = playerCamera.GetWorld3D().DirectSpaceState;
			var hit = space.IntersectRay(rayQuery);

			if (hit.Count > 0)
			{
				nextItem = hit["collider"].AsGodotObject() as BaseItem;
			}
		}

		if (nextItem == targetedItem)
			return;

		targetedItem = nextItem;

		if (targetedItem != null)
			GD.Print($"Looking at: {targetedItem.ItemName}");
		else
			GD.Print("No item targeted");
	}

	private void TryPickUp()
	{
		if (!GodotObject.IsInstanceValid(targetedItem))
			return;

		float playerSize = player.GlobalBasis.X.Length();

		if (!targetedItem.CanPickUp(playerSize))
		{
			GD.Print("You need to grow before picking this up!");
			return;
		}

		heldItem = targetedItem;
		targetedItem = null;

		savedGravityScale = heldItem.GravityScale;
		heldItem.GravityScale = 0.0f;

		// Remember how the item was oriented when grabbed.
		Basis cameraRotation = playerCamera.GlobalBasis.Orthonormalized();

		heldRotationOffset = cameraRotation.Inverse()
			* heldItem.GlobalBasis.Orthonormalized();

		heldItem.AddCollisionExceptionWith(player);
		player.AddCollisionExceptionWith(heldItem);

		heldItem.Sleeping = false;
	}

	private void UpdateHeldItem()
	{
		float playerSize = player.GlobalBasis.X.Length();
		Vector3 forward = -playerCamera.GlobalBasis.Z.Normalized();

		Vector3 holdPosition = playerCamera.GlobalPosition
			+ forward * HoldDistance * playerSize;

		Vector3 offset = holdPosition - heldItem.GlobalPosition;

		Vector3 acceleration =
			offset * HoldStrength
			- heldItem.LinearVelocity * HoldDamping;

		heldItem.Sleeping = false;
		heldItem.ApplyCentralForce(acceleration * heldItem.Mass);

		UpdateHeldRotation();
	}

	private void UpdateHeldRotation()
	{
		Basis targetBasis = playerCamera.GlobalBasis.Orthonormalized()
			* heldRotationOffset;

		Quaternion targetRotation = targetBasis.GetRotationQuaternion();
		Quaternion currentRotation = heldItem.GlobalBasis
			.Orthonormalized().GetRotationQuaternion();

		Quaternion difference =
			(targetRotation * currentRotation.Inverse()).Normalized();

		// Choose the shorter direction around to the target.
		if (difference.W < 0.0f)
		{
			difference = new Quaternion(
				-difference.X,
				-difference.Y,
				-difference.Z,
				-difference.W
			);
		}

		Vector3 imaginary = new Vector3(
			difference.X, difference.Y, difference.Z
		);

		float length = imaginary.Length();
		Vector3 rotationError = Vector3.Zero;

		if (length > 0.00001f)
		{
			float angle = 2.0f * Mathf.Atan2(length, difference.W);
			rotationError = imaginary / length * angle;
		}

		Vector3 angularAcceleration =
			rotationError * RotationStrength
			- heldItem.AngularVelocity * RotationDamping;

		// Limit extreme corrections during sudden turns.
		angularAcceleration = angularAcceleration.LimitLength(100.0f);

		// Inertia is the rotational equivalent of mass.
		Basis inverseInertia = heldItem.GetInverseInertiaTensor();

		if (inverseInertia.Determinant() == 0.0f)
			return;

		Vector3 torque = inverseInertia.Inverse() * angularAcceleration;
		heldItem.ApplyTorque(torque);
	}

	private void DropItem()
	{
		if (GodotObject.IsInstanceValid(heldItem))
		{
			heldItem.GravityScale = savedGravityScale;

			heldItem.RemoveCollisionExceptionWith(player);
			player.RemoveCollisionExceptionWith(heldItem);

			heldItem.Sleeping = false;
		}

		heldItem = null;
	}

	private void ThrowItem()
	{
		if (!GodotObject.IsInstanceValid(heldItem))
			return;

		BaseItem item = heldItem;
		Vector3 forward = -playerCamera.GlobalBasis.Z.Normalized();

		DropItem();

		item.ApplyCentralImpulse(forward * ThrowSpeed * item.Mass);
	}
}