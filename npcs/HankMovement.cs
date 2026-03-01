using Godot;
using System;
using System.Collections.Generic;

public partial class HankMovement : Node2D
{
	[Export] public TileMap tileMap;
	[Export] public float MoveSpeed = 60f;
	[Export] public float ClimbSpeed = 60f;

	// UI for the Break Button
	private Button _breakButton;
	private Vector2I? _pendingBreakTile = null; 
	private Vector2I? _targetBreakTile = null;  

	public static Vector2 hankPosition;

	private AStar2D _astar = new AStar2D();
	private Dictionary<Vector2I, int> _cellToId = new();
	private int _idCounter = 0;

	private Vector2[] _currentPath = Array.Empty<Vector2>();
	private int _pathIndex = 0;

	private const int TILE_SIZE = 8;
	private const int HANK_HALF_HEIGHT = 8; 
	private const float EPSILON = 0.1f; 

	public override void _Ready()
	{
		if (tileMap == null)
			tileMap = GetTree().CurrentScene.GetNodeOrNull<TileMap>("TileMap");
		
		SetupBreakButton();
		BuildGraph();
	}

	private void SetupBreakButton()
	{
		_breakButton = new Button();
		_breakButton.Text = "Break";
		_breakButton.Visible = false;
		_breakButton.ZIndex = 10; 
		AddChild(_breakButton);

		_breakButton.Pressed += OnBreakButtonPressed;
	}

	private bool IsFloorTile(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		
		// 1. If empty, it's not a floor
		if (atlas == new Vector2I(-1, -1)) return false;

		// 2. IMPORTANT: If it is one of our background tiles (X: 4-7, Y: 0-3), 
		// it is NOT a solid floor for collision/pathfinding.
		if (atlas.X >= 4 && atlas.X <= 7 && atlas.Y >= 0 && atlas.Y <= 3) return false;

		// 3. Check for grass or other solid obstacles
		if (atlas == new Vector2I(8, 1)) return true; // Grass
		return atlas.Y > 1; // Standard Floor Obstacles
	}

	private bool IsLadderTile(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		if (atlas == new Vector2I(-1, -1)) return false;
		// Ensure we don't mistake a background tile for a ladder
		if (atlas.X >= 4 && atlas.X <= 7) return false; 
		return atlas.X <= 3;
	}

	private float GetSurfaceOffsetFromTop(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		if (atlas.Y > 1 && atlas != new Vector2I(8, 1) && atlas != new Vector2I(-1, -1)) 
			return 7.0f; 
		return 0.0f;
	}

	private void BuildGraph()
	{
		_cellToId.Clear();
		_astar.Clear();
		_idCounter = 0;

		foreach (Vector2I cell in tileMap.GetUsedCells(0))
		{
			if (!IsFloorTile(cell) && !IsLadderTile(cell)) continue;

			bool isSurface = IsFloorTile(cell) && !IsFloorTile(cell + Vector2I.Up);
			if (!isSurface && !IsLadderTile(cell)) continue;

			int id = _idCounter++;
			_cellToId[cell] = id;

			Vector2 tileCenter = tileMap.MapToLocal(cell);
			float topToSurface = GetSurfaceOffsetFromTop(cell);
			float topOfTile = tileCenter.Y - (TILE_SIZE / 2f);
			float standingY = topOfTile + topToSurface - HANK_HALF_HEIGHT - EPSILON;

			_astar.AddPoint(id, new Vector2(tileCenter.X, standingY));
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
			if (dir == Vector2I.Left || dir == Vector2I.Right) allow = true;
			if ((dir == Vector2I.Up || dir == Vector2I.Down) && IsLadderTile(cell) && IsLadderTile(neighbor)) allow = true;

			if (allow) _astar.ConnectPoints(id, _cellToId[neighbor], true);
		}
	}

	public override void _Process(double delta)
	{
		if (_currentPath == null || _pathIndex >= _currentPath.Length) 
		{
			if (_targetBreakTile.HasValue) PerformBreak();
			if (!IsCurrentlyOnLadder()) ApplyFloorSnap();
			return;
		}

		Vector2 target = _currentPath[_pathIndex];
		float speed = IsCurrentlyOnLadder() ? ClimbSpeed : MoveSpeed;

		GlobalPosition = GlobalPosition.MoveToward(target, speed * (float)delta);
		if (GlobalPosition.DistanceTo(target) < 0.1f) _pathIndex++;

		if (!IsCurrentlyOnLadder()) ApplyFloorSnap();
		hankPosition = GlobalPosition;
	}

	private void OnBreakButtonPressed()
	{
		if (!_pendingBreakTile.HasValue) return;
		_targetBreakTile = _pendingBreakTile;
		_breakButton.Visible = false;

		// Move Hank to the closest AStar point to the tile he wants to break
		long endId = _astar.GetClosestPoint(tileMap.MapToLocal(_targetBreakTile.Value));
		long startId = _astar.GetClosestPoint(GlobalPosition);

		if (_astar.HasPoint(startId) && _astar.HasPoint(endId))
		{
			_currentPath = _astar.GetPointPath(startId, endId);
			_pathIndex = 0;
		}
	}

	private void PerformBreak()
	{
		if (_targetBreakTile.HasValue)
		{
			Vector2I cell = _targetBreakTile.Value;

			// 1. DETECT SOURCE ID: Grab the ID of the tile we are about to destroy
			int sourceId = tileMap.GetCellSourceId(0, cell);

			// If the cell was somehow already empty (-1), we should still try to 
			// find a valid source ID from a neighbor so we don't place "nothing"
			if (sourceId == -1) 
			{
				sourceId = GetValidSourceId();
			}

			// 2. Replace broken tile with background using the detected Source ID
			UpdateBackgroundAt(cell, sourceId);

			// 3. Update neighbors
			Vector2I[] neighbors = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
			foreach (var dir in neighbors)
			{
				Vector2I neighborPos = cell + dir;
				Vector2I atlas = tileMap.GetCellAtlasCoords(0, neighborPos);
				
				// Check if neighbor is a background tile (X: 4-7, Y: 0-3)
				if (atlas.X >= 4 && atlas.X <= 7 && atlas.Y >= 0 && atlas.Y <= 3)
					UpdateBackgroundAt(neighborPos, sourceId);
			}

			_targetBreakTile = null;
			BuildGraph();
		}
	}

	private void UpdateBackgroundAt(Vector2I cell, int sourceId)
	{
		bool top = IsFloorTile(cell + Vector2I.Up);
		bool bottom = IsFloorTile(cell + Vector2I.Down);
		bool left = IsFloorTile(cell + Vector2I.Left);
		bool right = IsFloorTile(cell + Vector2I.Right);

		int targetX = 5; 
		if (left && right) targetX = 7;
		else if (left) targetX = 4;
		else if (right) targetX = 6;

		int targetY = 1; 
		if (top && bottom) targetY = 3;
		else if (top) targetY = 0;
		else if (bottom) targetY = 2;

		// Use the dynamically detected sourceId instead of hardcoded 0
		tileMap.SetCell(0, cell, sourceId, new Vector2I(targetX, targetY));
	}

	// Helper to find ANY valid source ID in the map if the current cell is empty
	private int GetValidSourceId()
	{
		var usedCells = tileMap.GetUsedCells(0);
		if (usedCells.Count > 0)
		{
			return tileMap.GetCellSourceId(0, usedCells[0]);
		}
		return 0; // Fallback to 0 if the map is somehow totally empty
	}

	private bool IsCurrentlyOnLadder()
	{
		Vector2I cell = tileMap.LocalToMap(GlobalPosition);
		return IsLadderTile(cell);
	}

	private void ApplyFloorSnap()
	{
		Vector2 probePos = GlobalPosition + new Vector2(0, HANK_HALF_HEIGHT + 1f);
		Vector2I cell = tileMap.LocalToMap(probePos);

		while (IsFloorTile(cell) && IsFloorTile(cell + Vector2I.Up))
			cell += Vector2I.Up;

		if (IsFloorTile(cell))
		{
			Vector2 tileCenter = tileMap.MapToLocal(cell);
			float topToSurface = GetSurfaceOffsetFromTop(cell);
			float floorY = (tileCenter.Y - (TILE_SIZE / 2f)) + topToSurface;
			GlobalPosition = new Vector2(GlobalPosition.X, floorY - HANK_HALF_HEIGHT - EPSILON);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			Vector2 clickPos = GetGlobalMousePosition();
			Vector2I cell = tileMap.LocalToMap(clickPos);

			if (IsFloorTile(cell))
			{
				_pendingBreakTile = cell;
				_breakButton.GlobalPosition = tileMap.MapToLocal(cell) + new Vector2(-20, -25);
				_breakButton.Visible = true;
			}
			else
			{
				_breakButton.Visible = false;
				_targetBreakTile = null;
				long startId = _astar.GetClosestPoint(GlobalPosition);
				long endId = _astar.GetClosestPoint(clickPos);
				if (_astar.HasPoint(startId) && _astar.HasPoint(endId))
				{
					_currentPath = _astar.GetPointPath(startId, endId);
					_pathIndex = 0;
				}
			}
		}
	}
}
