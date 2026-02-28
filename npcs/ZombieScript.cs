using Godot;
using System;

public partial class ZombieScript : CharacterBody2D
{
	[Export]
	public float Speed { get; set; } = 300.0f;

	private Vector2 _targetPosition = Vector2.Zero;

	public override void _PhysicsProcess(double delta)
	{
		_targetPosition = HankMovement.hankPosition;
		Vector2 direction;
		if (ladder())
		{
			direction = new Vector2(0, 1) * GlobalPosition.DirectionTo(_targetPosition);
		}
		else
		{
			direction = new Vector2(1, 0) * GlobalPosition.DirectionTo(_targetPosition);
		}

		if (GlobalPosition.DistanceTo(_targetPosition) < 5.0f)
		{
			Velocity = Vector2.Zero;
		}
		else
		{
			Velocity = direction * Speed;
		}
		MoveAndSlide();
	}
	public bool ladder()
	{
		//TODO: Impliment this thing
		return false;
	}
}
