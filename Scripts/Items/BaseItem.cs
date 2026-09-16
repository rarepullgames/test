using Godot;
using System;

public partial class BaseItem : RigidBody3D
{
	[ExportGroup("Item")]
	[Export] public string ItemName = "Item";

	[ExportGroup("Pickup")]
	[Export] public float RequiredPlayerSize = 0.25f;

	[ExportGroup("Reward")]
	[Export] public float GrowthReward = 0.05f;

	public bool CanPickUp(float playerSize)
	{
		return playerSize >= RequiredPlayerSize;
	}
}