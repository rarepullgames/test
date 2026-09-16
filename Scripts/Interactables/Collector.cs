using Godot;

public partial class Collector : Area3D
{
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is not BaseItem item)
			return;

		// Prevent multiple collectors rewarding the same item.
		if (item.IsQueuedForDeletion())
			return;

		Player player = GetTree()
			.GetFirstNodeInGroup("player") as Player;

		if (player == null)
		{
			GD.PushWarning("Collector could not find the player.");
			return;
		}

		float reward = item.GrowthReward;

		player.AddGrowth(reward);
		item.QueueFree();

		GD.Print($"Collected {item.ItemName}: +{reward} size");
	}
}