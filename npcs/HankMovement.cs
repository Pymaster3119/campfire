using Godot;
using System;
using System.Collections.Generic;

public partial class HankMovement : Node2D
{
	[Export] public TileMap tileMap;
	[Export] public float MoveSpeed = 60f;
	[Export] public float ClimbSpeed = 60f;

	public static Vector2 hankPosition;

	private AStar2D _astar = new AStar2D();
	private Dictionary<Vector2I, int> _cellToId = new();
	private int _idCounter = 0;

	private Vector2[] _currentPath = Array.Empty<Vector2>();
	private int _pathIndex = 0;

	private const int TILE_SIZE = 8;
	private const int HANK_HALF_HEIGHT = 8; 
	// A tiny value to prevent boundary rounding errors
	private const float EPSILON = 0.1f; 

	public override void _Ready()
	{
		if (tileMap == null)
			tileMap = GetTree().CurrentScene.GetNodeOrNull<TileMap>("TileMap");
		
		BuildGraph();
	}

	private bool IsFloorTile(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		if (atlas == new Vector2I(-1, -1)) return false;
		if (atlas == new Vector2I(8, 1)) return true; // Grass
		return atlas.Y > 1; // Floor Obstacles
	}

	private bool IsLadderTile(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		if (atlas == new Vector2I(-1, -1)) return false;
		return atlas.X <= 3;
	}

	private void BuildGraph()
	{
		_cellToId.Clear();
		_astar.Clear();
		_idCounter = 0;

		foreach (Vector2I cell in tileMap.GetUsedCells(0))
		{
			bool hasFloor = IsFloorTile(cell);
			bool hasLadder = IsLadderTile(cell);

			if (!hasFloor && !hasLadder) continue;

			int id = _idCounter++;
			_cellToId[cell] = id;

			Vector2 worldPos = tileMap.MapToLocal(cell);
			
			// We subtract EPSILON here. 
			// This places the pathfinding point 0.1 pixels above the floor.
			// This prevents Hank from "sinking" to reach a point exactly on the boundary.
			worldPos.Y += (TILE_SIZE / 2f) - HANK_HALF_HEIGHT - EPSILON;

			_astar.AddPoint(id, worldPos);
		}

		foreach (var pair in _cellToId)
			ConnectNeighbors(pair.Key, pair.Value);
	}

	private void ConnectNeighbors(Vector2I cell, int id)
	{
		Vector2I[] directions = { Vector2I.Left, Vector2I.Right, Vector2I.Up, Vector2I.Down };

		foreach (var dir in directions)
		{
			Vector2I neighbor = cell + dir;
			if (!_cellToId.ContainsKey(neighbor)) continue;

			bool allow = false;
			if ((dir == Vector2I.Left || dir == Vector2I.Right) &&
				(IsFloorTile(cell) || IsLadderTile(cell)) &&
				(IsFloorTile(neighbor) || IsLadderTile(neighbor)))
			{
				allow = true;
			}

			if ((dir == Vector2I.Up || dir == Vector2I.Down) &&
				IsLadderTile(cell) && IsLadderTile(neighbor))
			{
				allow = true;
			}

			if (allow)
				_astar.ConnectPoints(id, _cellToId[neighbor], true);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			Vector2 clickPos = GetGlobalMousePosition();
			long startId = _astar.GetClosestPoint(GlobalPosition);
			long endId = _astar.GetClosestPoint(clickPos);

			if (_astar.HasPoint(startId) && _astar.HasPoint(endId))
			{
				_currentPath = _astar.GetPointPath(startId, endId);
				_pathIndex = 0;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (_currentPath == null || _pathIndex >= _currentPath.Length) 
		{
			if (!IsCurrentlyOnLadder()) ApplyFloorSnap();
			return;
		}

		Vector2 target = _currentPath[_pathIndex];
		float speed = IsCurrentlyOnLadder() ? ClimbSpeed : MoveSpeed;

		Vector2 newPos = GlobalPosition.MoveToward(target, speed * (float)delta);
		GlobalPosition = newPos;

		if (GlobalPosition.DistanceTo(target) < 0.1f)
		{
			_pathIndex++;
		}

		if (!IsCurrentlyOnLadder())
		{
			ApplyFloorSnap();
		}

		hankPosition = GlobalPosition;
	}

	private bool IsCurrentlyOnLadder()
	{
		// Sample from the center of Hank
		Vector2I cell = tileMap.LocalToMap(GlobalPosition);
		return IsLadderTile(cell);
	}

	private void ApplyFloorSnap()
	{
		// IMPORTANT: We check 1 pixel BELOW the feet to ensure we find the actual floor tile,
		// not the tile Hank is currently standing in.
		Vector2 probePos = GlobalPosition + new Vector2(0, HANK_HALF_HEIGHT + 1f);
		Vector2I cellBelow = tileMap.LocalToMap(probePos);

		if (IsFloorTile(cellBelow))
		{
			Vector2 tileCenter = tileMap.MapToLocal(cellBelow);
			float floorY = tileCenter.Y - (TILE_SIZE / 2f);
			
			// Stay EPSILON above the floor so the next frame's 'probePos' 
			// doesn't accidentally sample the wrong tile.
			GlobalPosition = new Vector2(GlobalPosition.X, floorY - HANK_HALF_HEIGHT - EPSILON);
		}
	}
}
