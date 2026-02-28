using Godot;
using System;

public partial class Turret : Area2D
{
	[Export] public int Level { get; set; } = 1;

	private float _range = 60f;
	private float _damage = 10f;
	private float _fireRate = 1.0f;
	private float _cooldown = 0f;

	private CollisionShape2D _rangeShape;

	public override void _Ready()
	{
		_rangeShape = GetNode<CollisionShape2D>("RangeShape");
		ApplyLevel();
	}

	private void ApplyLevel()
	{
		_range = 60f + (Level - 1) * 20f;
		_damage = 10f + (Level - 1) * 8f;
		_fireRate = 1.0f - (Level - 1) * 0.2f;

		if (_rangeShape?.Shape is CircleShape2D circle)
			circle.Radius = _range;

		GD.Print($"Turret level {Level}: range={_range} dmg={_damage} rate={_fireRate}s");
	}

	public override void _Process(double delta)
	{
		_cooldown -= (float)delta;
		if (_cooldown > 0f)
			return;

		var target = FindNearestZombie();
		if (target == null)
			return;

		Shoot(target);
		_cooldown = _fireRate;
	}

	private Node2D FindNearestZombie()
	{
		Node2D closest = null;
		float closestDist = float.MaxValue;

		foreach (Node2D body in GetOverlappingBodies())
		{
			if (body is not ZombieScript)
				continue;

			float dist = GlobalPosition.DistanceTo(body.GlobalPosition);
			if (dist < closestDist)
			{
				closestDist = dist;
				closest = body;
			}
		}
		return closest;
	}

	private void Shoot(Node2D target)
	{
		var bullet = new Bullet();
		bullet.GlobalPosition = GlobalPosition;
		bullet.Direction = GlobalPosition.DirectionTo(target.GlobalPosition);
		bullet.Damage = _damage;
		GetTree().CurrentScene.AddChild(bullet);
	}

	public void Upgrade()
	{
		if (Level >= 3)
			return;

		var storage = GetNode<NewScript>("/root/Storage");
		int cost = Level * 2;
		if (!storage.Remove("wood", cost))
		{
			GD.Print($"Need {cost} wood to upgrade turret");
			return;
		}

		Level++;
		ApplyLevel();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.U)
		{
			float dist = GlobalPosition.DistanceTo(HankMovement.hankPosition);
			if (dist < 20f)
				Upgrade();
		}
	}
}
