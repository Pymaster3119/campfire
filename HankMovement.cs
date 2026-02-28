using Godot;
using System;

public partial class HankMovement : CharacterBody2D
{
	[Export]
	public float Speed { get; set; } = 300.0f;
	public static Vector2 hankPosition;
	[Export]
	public TileMap scene;

	private Vector2 _targetPosition = Vector2.Zero;

	public override void _Ready()
	{
		hankPosition = GlobalPosition;
		_targetPosition = GlobalPosition;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("click"))
		{
			_targetPosition = GetGlobalMousePosition();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		hankPosition = GlobalPosition;
		Vector2 direction;
		if (System.Math.Abs(_targetPosition[1] - GlobalPosition[1]) > 5f && ladder())
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
		if (!ladder() && !ground())
		{
			Velocity = new Vector2(0,1) * Speed;
		}
		MoveAndSlide();
	}
	public bool ladder()
	{
		scene = GetTree().CurrentScene.GetNode<TileMap>("TileMap");
		Vector2 localPosition = scene.ToLocal(GlobalPosition);
		localPosition = new Vector2((float)Math.Floor(localPosition[0]),(float)Math.Floor(localPosition[1])-6);
		Vector2I mapCoords = scene.LocalToMap(localPosition);
		int layer = 0;
		int sourceId = scene.GetCellSourceId(layer, mapCoords);
		Vector2I atlasCoords = scene.GetCellAtlasCoords(layer, mapCoords);
		int alternativeTile = scene.GetCellAlternativeTile(layer, mapCoords);
		TileData tileData = scene.GetCellTileData(layer, mapCoords);

		if (sourceId != -1)
		{
			if (atlasCoords[0] <=3)
			{
				return true;
			}
		}
		
		localPosition = scene.ToLocal(GlobalPosition);
		localPosition = new Vector2((float)Math.Floor(localPosition[0]),(float)Math.Floor(localPosition[1])+7);
		mapCoords = scene.LocalToMap(localPosition);
		layer = 0;
		sourceId = scene.GetCellSourceId(layer, mapCoords);
		atlasCoords = scene.GetCellAtlasCoords(layer, mapCoords);
		alternativeTile = scene.GetCellAlternativeTile(layer, mapCoords);
		tileData = scene.GetCellTileData(layer, mapCoords);

		if (sourceId != -1)
		{
			if (atlasCoords[0] <=3)
			{
				return true;
			}
		}
		return false;
	}
	
	public bool ground()
	{
		scene = GetTree().CurrentScene.GetNode<TileMap>("TileMap");
		Vector2 localPosition = scene.ToLocal(GlobalPosition);
		localPosition = new Vector2((float)Math.Floor(localPosition[0]),(float)Math.Floor(localPosition[1])-6);
		Vector2I mapCoords = scene.LocalToMap(localPosition);
		int layer = 0;
		int sourceId = scene.GetCellSourceId(layer, mapCoords);
		Vector2I atlasCoords = scene.GetCellAtlasCoords(layer, mapCoords);
		int alternativeTile = scene.GetCellAlternativeTile(layer, mapCoords);
		TileData tileData = scene.GetCellTileData(layer, mapCoords);

		if (sourceId != -1)
		{
			if (atlasCoords[1] <=2 && System.Math.Abs(GlobalPosition[1] - mapCoords[1]) < 2f)
			{
				return true;
			}
		}
		return false;
	}
}
