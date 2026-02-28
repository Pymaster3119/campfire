using Godot;
using System;

public partial class CrateScript : Area2D
{
	private static readonly string[] LootTable = { "seed", "seed", "seed", "turret", "wood", "wood" };

	private bool _playerInside = false;
	private bool _opened = false;
	private Label _label;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
		_label.Text = "?";

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.Name == "Hank")
			_playerInside = true;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body.Name == "Hank")
			_playerInside = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_opened || !_playerInside)
			return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.E)
		{
			OpenCrate();
		}
	}

	private void OpenCrate()
	{
		_opened = true;

		var rng = new RandomNumberGenerator();
		rng.Randomize();
		string loot = LootTable[rng.RandiRange(0, LootTable.Length - 1)];
		int amount = loot == "turret" ? 1 : rng.RandiRange(1, 3);

		var storage = GetNode<NewScript>("/root/Storage");
		storage.Add(loot, amount);

		_label.Text = $"+{amount} {loot}";
		GD.Print($"Crate opened! Got {amount}x {loot}");

		var timer = GetTree().CreateTimer(2.0);
		timer.Timeout += () => QueueFree();
	}
}
