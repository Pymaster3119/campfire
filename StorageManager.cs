using Godot;
using System;

public partial class NewScript : Node
{
	private Dictionary<string, int> _amounts = new();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("StorageManager READY (autoload works)");
		_amounts["wood"] = 25;
	}

	public int GetAmount(string id)
	{
		return _amounts.ContainsKey(id) ? _amounts[id] : 0;
	}
	
	public void Add(string id, int amount)
	{
		// Update the stored amount: current amount + amount added.
		_amounts[id] = GetAmount(id) + amount;

		// Print to Output so we can see it updating.
		GD.Print($"Added {amount} {id}. Now: {_amounts[id]}");
	}
	
}
