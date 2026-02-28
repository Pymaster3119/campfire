using Godot;
using System;
using System.Collections.Generic;

public partial class HankMovement : Node2D
{
	[Export] public TileMap tileMap;
	[Export] public float MoveSpeed = 60f;
	[Export] public float Gravity = 400f;
	[Export] public float ClimbSpeed = 60f;
	public static Vector2 hankPosition;

	private AStar2D _astar = new AStar2D();
	private Dictionary<Vector2I, int> _cellToId = new();
	private int _idCounter = 0;

	private Vector2[] _currentPath = Array.Empty<Vector2>();
	private int _pathIndex = 0;

	private float _yVelocity = 0f;

	private const int TILE_SIZE = 8;
	private const int HANK_HALF_HEIGHT = 0;

	public override void _Ready()
	{
		tileMap = GetTree().CurrentScene.GetNode<TileMap>("TileMap");
		BuildGraph();
	}

	// ===============================
	// BUILD PATH GRAPH
	// ===============================
	private void BuildGraph()
	{
		foreach (Vector2I cell in tileMap.GetUsedCells(0))
		{
			Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);

			if (atlas == new Vector2I(-1, -1))
				continue;

			bool isFloor = atlas.Y <= 1;
			bool isLadder = atlas.X <= 3;

			if (!isFloor && !isLadder)
				continue;

			int id = _idCounter++;
			_cellToId[cell] = id;

			// Standing position = center X, top of tile - half height
			Vector2 tileTopLeft = tileMap.MapToLocal(cell);
			Vector2 worldPos = tileTopLeft + new Vector2(TILE_SIZE / 2f, -HANK_HALF_HEIGHT);

			_astar.AddPoint(id, worldPos);
		}

		foreach (var pair in _cellToId)
		{
			ConnectNeighbors(pair.Key, pair.Value);
		}
	}

	private void ConnectNeighbors(Vector2I cell, int id)
	{
		Vector2I[] directions =
		{
			Vector2I.Left,
			Vector2I.Right,
			Vector2I.Up,
			Vector2I.Down
		};

		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		bool isFloor = atlas.Y <= 1;
		bool isLadder = atlas.X <= 3;

		foreach (var dir in directions)
		{
			Vector2I neighbor = cell + dir;

			if (!_cellToId.ContainsKey(neighbor))
				continue;

			bool allow = false;

			// Horizontal only from floor
			if ((dir == Vector2I.Left || dir == Vector2I.Right) && isFloor)
				allow = true;

			// Vertical only from ladder
			if ((dir == Vector2I.Up || dir == Vector2I.Down) && isLadder)
				allow = true;

			if (allow)
			{
				_astar.ConnectPoints(id, _cellToId[neighbor], false);
			}
		}
	}

	// ===============================
	// CLICK TO MOVE
	// ===============================
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent &&
			mouseEvent.Pressed &&
			mouseEvent.ButtonIndex == MouseButton.Left)
		{
			Vector2 clickPos = GetGlobalMousePosition();
			_currentPath = GetPath(GlobalPosition, clickPos);
			_pathIndex = 0;
		}
	}

	public Vector2[] GetPath(Vector2 worldStart, Vector2 worldEnd)
	{
		Vector2I startCell = tileMap.LocalToMap(worldStart);
		Vector2I endCell = tileMap.LocalToMap(worldEnd);

		if (!_cellToId.ContainsKey(startCell) || !_cellToId.ContainsKey(endCell))
			return Array.Empty<Vector2>();

		return _astar.GetPointPath(
			_cellToId[startCell],
			_cellToId[endCell]
		);
	}

	// ===============================
	// MOVEMENT + GRAVITY
	// ===============================
	public override void _Process(double delta)
	{
		float dt = (float)delta;

		bool climbing = false;

		// Path movement
		if (_currentPath.Length > 0 && _pathIndex < _currentPath.Length)
		{
			Vector2 target = _currentPath[_pathIndex];
			Vector2 difference = target - GlobalPosition;

			// Detect vertical path segment (ladder)
			if (Mathf.Abs(difference.Y) > 2f)
			{
				climbing = true;

				float moveY = Mathf.Sign(difference.Y) * ClimbSpeed * dt;
				GlobalPosition += new Vector2(0, moveY);

				if (Mathf.Abs(difference.Y) < 2f)
					_pathIndex++;
			}
			else
			{
				float moveX = Mathf.Sign(difference.X) * MoveSpeed * dt;
				GlobalPosition += new Vector2(moveX, 0);

				if (Mathf.Abs(difference.X) < 2f)
					_pathIndex++;
			}
		}

		// Gravity only if not climbing
		if (!climbing)
		{
			if (!IsStandingOnFloor())
			{
				_yVelocity += Gravity * dt;
			}
			else
			{
				_yVelocity = 0;
				SnapToFloor();
			}

			GlobalPosition += new Vector2(0, _yVelocity * dt);
			hankPosition = GlobalPosition;
		}
	}

	// ===============================
	// FLOOR CHECK
	// ===============================
	private bool IsStandingOnFloor()
	{
		Vector2 feet = GlobalPosition + new Vector2(0, HANK_HALF_HEIGHT);
		Vector2I cellBelow = tileMap.LocalToMap(feet);

		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cellBelow);
		if (atlas == new Vector2I(-1, -1))
			return false;

		return atlas.Y <= 1;
	}

	private void SnapToFloor()
	{
		// Check multiple points along feet
		float leftFoot = GlobalPosition.X - 4; // 4px left from center
		float rightFoot = GlobalPosition.X + 4; // 4px right from center
		float feetY = GlobalPosition.Y + HANK_HALF_HEIGHT;

		Vector2I leftCell = tileMap.LocalToMap(new Vector2(leftFoot, feetY));
		Vector2I rightCell = tileMap.LocalToMap(new Vector2(rightFoot, feetY));

		Vector2 tileTopLeft = tileMap.MapToLocal(leftCell); // just pick left for Y

		// Snap to top of floor
		GlobalPosition = new Vector2(
			GlobalPosition.X,
			tileTopLeft.Y + 2
		);
	}
}
