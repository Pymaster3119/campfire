using Godot;

public partial class TurretTestDriver : Node2D
{
	public override void _Ready()
	{
		// Set Hank's position near the turret so upgrade (U key) works
		HankMovement.hankPosition = new Vector2(100, 100);

		// Give some wood for upgrades
		var storage = GetNode<NewScript>("/root/Storage");
		storage.Add("wood", 50);

		GD.Print("=== TURRET TEST ===");
		GD.Print("Zombies should get shot automatically.");
		GD.Print("Press U to upgrade turret (costs wood).");
	}
}
