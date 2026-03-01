using Godot;
using System;
using System.Collections.Generic;

public partial class HankMovement : Node2D
{
	[Export] public TileMap tileMap;
	[Export] public float MoveSpeed = 60f;
	[Export] public float ClimbSpeed = 60f;

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

	// --- TILE CLASSIFICATION ---

	private bool IsBackgroundTile(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		// Treat empty space (-1,-1) as background so Hank can walk through it
		if (atlas == new Vector2I(-1, -1)) return true;
		return atlas.X >= 4 && atlas.X <= 7 && atlas.Y >= 0 && atlas.Y <= 3;
	}

	private bool IsFloorTile(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		if (atlas == new Vector2I(-1, -1)) return false;
		if (IsBackgroundTile(cell)) return false; 
		if (atlas == new Vector2I(8, 1)) return true; // Grass
		return atlas.Y > 1; 
	}

	private bool IsLadderTile(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		if (atlas == new Vector2I(-1, -1)) return false;
		// Ladders are X 0-3. We don't check IsBackground here to allow overlap if needed.
		return atlas.X <= 3;
	}

	private float GetSurfaceOffsetFromTop(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		if (atlas.Y > 1 && atlas != new Vector2I(8, 1) && atlas != new Vector2I(-1, -1)) 
			return -1.0f; 
		return 0.0f;
	}

	private void BuildGraph()
	{
		_cellToId.Clear();
		_astar.Clear();
		_idCounter = 0;

		// IMPORTANT: We check all used cells PLUS the space above them
		var usedCells = tileMap.GetUsedCells(0);
		var walkableCandidates = new HashSet<Vector2I>();

		foreach (var cell in usedCells)
		{
			walkableCandidates.Add(cell);
			walkableCandidates.Add(cell + Vector2I.Up); // Ensure we check the air above every tile
		}

		foreach (Vector2I cell in walkableCandidates)
		{
			bool isLadder = IsLadderTile(cell);
			bool isPassable = IsBackgroundTile(cell) || isLadder;
			bool hasFloorBelow = IsFloorTile(cell + Vector2I.Down);

			// A cell gets a node if it's a ladder OR (it's passable air and there's a floor below)
			if (isLadder || (isPassable && hasFloorBelow))
			{
				int id = _idCounter++;
				_cellToId[cell] = id;

				Vector2 tileCenter = tileMap.MapToLocal(cell);
				float floorTopOffset = hasFloorBelow ? GetSurfaceOffsetFromTop(cell + Vector2I.Down) : 0;
				
				// Align the node exactly with where ApplyFloorSnap wants Hank to be
				float floorLocalY = tileMap.MapToLocal(cell + Vector2I.Down).Y - (TILE_SIZE / 2f);
				float standingY = floorLocalY + floorTopOffset - HANK_HALF_HEIGHT - EPSILON;

				_astar.AddPoint(id, new Vector2(tileCenter.X, isLadder && !hasFloorBelow ? tileCenter.Y : standingY));
			}
		}

		foreach (var pair in _cellToId)
			ConnectNeighbors(pair.Key, pair.Value);
	}

	private void ConnectNeighbors(Vector2I cell, int id)
	{
		// Directions: Left, Right, Up, Down, and Diagonals for steps
		Vector2I[] directions = { 
			Vector2I.Left, Vector2I.Right, Vector2I.Up, Vector2I.Down,
			new Vector2I(1, -1), new Vector2I(-1, -1), // Step up
			new Vector2I(1, 1), new Vector2I(-1, 1)    // Step down
		};

		foreach (var dir in directions)
		{
			Vector2I neighbor = cell + dir;
			if (!_cellToId.ContainsKey(neighbor)) continue;

			bool isHorizontal = dir.Y == 0;
			bool isVerticalLadder = dir.X == 0 && IsLadderTile(cell) && IsLadderTile(neighbor);
			bool isDiagonalStep = dir.X != 0 && dir.Y != 0; // Allows walking up/down 1-tile bumps

			if (isHorizontal || isVerticalLadder || isDiagonalStep)
				_astar.ConnectPoints(id, _cellToId[neighbor], true);
		}
	}

	// --- MOVEMENT & INTERACTION ---

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
		
		if (GlobalPosition.DistanceTo(target) < 0.5f) // Increased epsilon for smoother pathing
			_pathIndex++;

		if (!IsCurrentlyOnLadder()) ApplyFloorSnap();
		hankPosition = GlobalPosition;
	}

	private void ApplyFloorSnap()
	{
		Vector2 probePos = GlobalPosition + new Vector2(0, HANK_HALF_HEIGHT + 1f);
		Vector2I cell = tileMap.LocalToMap(probePos);

		while (IsFloorTile(cell) && IsFloorTile(cell + Vector2I.Up)) cell += Vector2I.Up;

		if (IsFloorTile(cell))
		{
			Vector2 tileCenter = tileMap.MapToLocal(cell);
			float floorY = (tileCenter.Y - (TILE_SIZE / 2f)) + GetSurfaceOffsetFromTop(cell);
			GlobalPosition = new Vector2(GlobalPosition.X, floorY - HANK_HALF_HEIGHT - EPSILON);
		}
	}

	private void OnBreakButtonPressed()
	{
		if (!_pendingBreakTile.HasValue) return;
		//Reduce hunger
		HankStats.Hunger -= 5;
		_targetBreakTile = _pendingBreakTile;
		_breakButton.Visible = false;

		long startId = _astar.GetClosestPoint(GlobalPosition);
		// Find the best walkable node adjacent to the block we want to break
		float bestDist = float.MaxValue;
		long bestEndId = -1;

		foreach (var dir in new Vector2I[] { Vector2I.Left, Vector2I.Right, Vector2I.Up, Vector2I.Down })
		{
			Vector2I nCell = _targetBreakTile.Value + dir;
			if (_cellToId.ContainsKey(nCell))
			{
				float d = GlobalPosition.DistanceTo(_astar.GetPointPosition(_cellToId[nCell]));
				if (d < bestDist) { bestDist = d; bestEndId = _cellToId[nCell]; }
			}
		}

		if (bestEndId != -1)
		{
			_currentPath = _astar.GetPointPath(startId, bestEndId);
			_pathIndex = 0;
		}
	}

	private void PerformBreak()
	{
		Vector2I cell = _targetBreakTile.Value;
		int sourceId = Math.Max(0, tileMap.GetCellSourceId(0, cell));
		UpdateBackgroundAt(cell, sourceId);

		foreach (var dir in new Vector2I[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
		{
			if (IsBackgroundTile(cell + dir)) UpdateBackgroundAt(cell + dir, sourceId);
		}

		_targetBreakTile = null;
		BuildGraph(); 
	}

	private void UpdateBackgroundAt(Vector2I cell, int sourceId)
	{
		bool top = IsFloorTile(cell + Vector2I.Up);
		bool bottom = IsFloorTile(cell + Vector2I.Down);
		bool left = IsFloorTile(cell + Vector2I.Left);
		bool right = IsFloorTile(cell + Vector2I.Right);

		int targetX = left && right ? 7 : left ? 4 : right ? 6 : 5;
		int targetY = top && bottom ? 3 : top ? 0 : bottom ? 2 : 1;

		tileMap.SetCell(0, cell, sourceId, new Vector2I(targetX, targetY));
	}

	private bool IsCurrentlyOnLadder() => IsLadderTile(tileMap.LocalToMap(GlobalPosition));

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
