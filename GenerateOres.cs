using Godot;
using System;

public partial class GenerateOres : TileMap
{
	//Ore definition
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
	[Export] public int medpackProbability;
	[Export] public int tileMapWidth;
	[Export] public int tileMapHeight;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		int sourceId = Math.Max(0, GetCellSourceId(0, new Vector2I(0,0)));
		//Generate Tiles
		for (int x = 0; x < tileMapWidth; x++)
		{
			for (int y = 0; y < tileMapHeight; y++)
			{
				//Randomly choose number
				int rand = GD.RandRange(0,100);
				if (rand < stoneProbability)
				{
					SetCell(0, new Vector2I(x, y), sourceId, stoneCoordinates);
				}
				else if (rand < bronzeProbability)
				{
					SetCell(0, new Vector2I(x, y), sourceId, bronzeCoordinates);
				}
				else if (rand < ironProbability)
				{
					SetCell(0, new Vector2I(x, y), sourceId, ironCoordinates);
				}
				else if (rand < gunpowderProbability)
				{
					SetCell(0, new Vector2I(x, y), sourceId, gunpowderCoordinates);
				}
				else if (rand < diamondProbability)
				{
					SetCell(0, new Vector2I(x, y), sourceId, diamondCoordinates);
				}
				else
				{
					SetCell(0, new Vector2I(x, y), sourceId, medpackCoordinates);
				}
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
