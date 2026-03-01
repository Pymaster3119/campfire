using Godot;
using System;

public partial class HoardGenerator : Node2D
{
	[Export]
	public double hoardeTime = 60.0;
	public static double timer = 60.0;
	public int zombNumber = 5;
	[Export]
	public PackedScene zombTemplate;
	public static int zombDamage = 50;
	public int hoardenumber = 0;
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
			hoardenumber ++;
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
			zombNumber = Math.Max(100, zombNumber * 2);
			zombDamage = (int) (0.5f * hoardenumber * hoardenumber) + 50;
		}
	}
}
