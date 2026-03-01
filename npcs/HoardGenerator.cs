using Godot;
using System;

public partial class HoardGenerator : Node2D
{
	[Export]
	public double hoardeTime = 60.0;
	public static double timer = 0.0;
	public int zombNumber = 5;
	[Export]
	public PackedScene zombTemplate;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		timer += delta;
		if (timer >= hoardeTime)
		{
			//Spawn in dem zombs
			for (int i = 0; i < zombNumber; i++)
			{
				Node2D enemy = zombTemplate.Instantiate<Node2D>();
				float randomX = (float)GD.RandRange(-50, 50);
				float randomY = -56;
				enemy.Position = new Vector2(randomX, randomY);
				GetTree().CurrentScene.AddChild(enemy);
			}
			
			//Update parmentiers
			timer = 0;
			zombNumber = zombNumber * zombNumber;
		}
	}
}
