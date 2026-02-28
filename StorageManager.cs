using Godot;
using System;
using System.Collections.Generic;

public partial class NewScript : Node
{
	private Dictionary<string, int> _amounts = new();

	public override void _Ready()
	{
		GD.Print("StorageManager READY (autoload works)");
		_amounts["wood"] = 25;
	}

	public int GetAmount(string id)
	{
		return _amounts.ContainsKey(id) ? _amounts[id] : 0;
	}

	public bool HasAmount(string id, int amount)
	{
		return GetAmount(id) >= amount;
	}

	public void Add(string id, int amount)
	{
		_amounts[id] = GetAmount(id) + amount;
		GD.Print($"Added {amount} {id}. Now: {_amounts[id]}");
	}

	public bool Remove(string id, int amount)
	{
		if (!HasAmount(id, amount))
			return false;
		_amounts[id] -= amount;
		GD.Print($"Removed {amount} {id}. Now: {_amounts[id]}");
		return true;
	}
}
