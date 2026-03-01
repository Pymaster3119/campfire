using Godot;
using System;
using System.Collections.Generic;

public partial class GenerateOres : TileMap
{
	[Export] public Vector2I bronzeCoordinates;
	[Export] public Vector2I stoneCoordinates;
	[Export] public Vector2I ironCoordinates;
	[Export] public Vector2I gunpowderCoordinates;
	[Export] public Vector2I diamondCoordinates;
	[Export] public Vector2I medpackCoordinates;

	[Export] public int stoneProbability;
	[Export] public int bronzeProbability;
	[Export] public int ironProbability;
	[Export] public int gunpowderProbability;
	[Export] public int diamondProbability;

	[Export] public int tileMapWidth;
	[Export] public int tileMapHeight;

	public static List<Vector2I> stonePositions = new();
	public static List<Vector2I> bronzePositions = new();
	public static List<Vector2I> ironPositions = new();
	public static List<Vector2I> gunpowderPositions = new();
	public static List<Vector2I> diamondPositions = new();
	public static List<Vector2I> medpackPositions = new();

	public override void _Ready()
	{
		int sourceId = Math.Max(0, GetCellSourceId(0, new Vector2I(0, 0)));

		for (int x = -tileMapWidth; x < tileMapWidth; x++)
		{
			for (int y = 4; y < tileMapHeight + 4; y++)
			{
				Vector2I pos = new Vector2I(x, y);
				int rand = GD.RandRange(0, 100);

				if (rand < stoneProbability)
				{
					SetCell(0, pos, sourceId, stoneCoordinates);
					stonePositions.Add(pos);
				}
				else if (rand < bronzeProbability)
				{
					SetCell(0, pos, sourceId, bronzeCoordinates);
					bronzePositions.Add(pos);
				}
				else if (rand < ironProbability)
				{
					SetCell(0, pos, sourceId, ironCoordinates);
					ironPositions.Add(pos);
				}
				else if (rand < gunpowderProbability)
				{
					SetCell(0, pos, sourceId, gunpowderCoordinates);
					gunpowderPositions.Add(pos);
				}
				else if (rand < diamondProbability)
				{
					SetCell(0, pos, sourceId, diamondCoordinates);
					diamondPositions.Add(pos);
				}
				else
				{
					SetCell(0, pos, sourceId, medpackCoordinates);
					medpackPositions.Add(pos);
				}
			}
		}
	}

	public override void _Process(double delta)
	{
	}
}
