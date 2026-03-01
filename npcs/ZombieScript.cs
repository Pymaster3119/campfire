using Godot;
using System;
using System.Collections.Generic;

public partial class ZombieScript : Node2D
{
	[Export] public TileMap tileMap;
	[Export] public float MoveSpeed = 15f;
	[Export] public float ClimbSpeed = 15f;
	[Export] public float FallSpeed = 45f; // Speed when dropping off a ledge
	[Export] public float PathUpdateInterval = 0.2f;
	[Export] public float StopDistance = 8.0f; //Protect hankypoo
	[Export] public int MaxFallDistance = 15; // Max tiles a zombie will drop down to chase

	private AStar2D _astar = new AStar2D();
	private Dictionary<Vector2I, int> _cellToId = new();
	private int _idCounter = 0;

	private Vector2[] _currentPath = Array.Empty<Vector2>();
	private int _pathIndex = 0;
	private float _updateTimer = 0f;
	private Vector2 _lastTargetPos = Vector2.Zero;

	private const int TILE_SIZE = 8;
	private const int HANK_HALF_HEIGHT = 8; 
	private const float EPSILON = 0.1f; 
	[Export] public Sprite2D hankymylove;

	public override void _Ready()
	{
		if (tileMap == null)
			tileMap = GetTree().CurrentScene.GetNodeOrNull<TileMap>("TileMap");
		AddToGroup("zombies");
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
			bool isFloor = IsFloorTile(cell);
			bool isLadder = IsLadderTile(cell);
			bool isSurface = isFloor && !IsFloorTile(cell + Vector2I.Up);
			if (!isSurface && !isLadder) continue;

			int id = _idCounter++;
			_cellToId[cell] = id;
			Vector2 tileCenter = tileMap.MapToLocal(cell);
			float topToSurface = GetSurfaceOffsetFromTop(cell);
			float topOfTile = tileCenter.Y - (TILE_SIZE / 2f);
			float standingY = topOfTile + topToSurface - HANK_HALF_HEIGHT - EPSILON;
			_astar.AddPoint(id, new Vector2(tileCenter.X, standingY));
		}

		foreach (var pair in _cellToId) ConnectNeighbors(pair.Key, pair.Value);
	}

	private void ConnectNeighbors(Vector2I cell, int id)
	{
		Vector2I[] directions = { Vector2I.Left, Vector2I.Right, Vector2I.Up, Vector2I.Down };
		foreach (var dir in directions)
		{
			Vector2I neighbor = cell + dir;
			if (_cellToId.ContainsKey(neighbor))
			{
				bool allow = (dir == Vector2I.Left || dir == Vector2I.Right) || 
							 ((dir == Vector2I.Up || dir == Vector2I.Down) && IsLadderTile(cell) && IsLadderTile(neighbor));
				if (allow) _astar.ConnectPoints(id, _cellToId[neighbor], true);
			}
			else if (dir == Vector2I.Left || dir == Vector2I.Right)
			{
				// FALLING LOGIC: If the space to the left or right is empty, scan downwards
				for (int i = 1; i <= MaxFallDistance; i++)
				{
					Vector2I dropCell = neighbor + new Vector2I(0, i);
					
					if (_cellToId.ContainsKey(dropCell))
					{
						// Found a valid landing surface below! Create a ONE-WAY connection down.
						_astar.ConnectPoints(id, _cellToId[dropCell], false);
						break;
					}
					
					if (IsFloorTile(dropCell))
					{
						// We hit a solid block that isn't a walkable surface (e.g., hitting the side of a wall inside the ground)
						break;
					}
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		_updateTimer += (float)delta;
		Vector2 targetHank = HankMovement.hankPosition;

		if (_updateTimer >= PathUpdateInterval)
		{
			if (GlobalPosition.DistanceTo(targetHank) > StopDistance)
			{
				UpdatePathToPlayer(targetHank);
			}
			_updateTimer = 0f;
		}

		if (_currentPath != null && _pathIndex < _currentPath.Length)
		{
			Vector2 targetPoint = _currentPath[_pathIndex];
			float speed = IsCurrentlyOnLadder() ? ClimbSpeed : MoveSpeed;

			if (GlobalPosition.DistanceTo(targetHank) < StopDistance)
			{
				_currentPath = Array.Empty<Vector2>();
				//Damage that guy and die
				if (GlobalPosition.DistanceTo(HankMovement.hankPosition) < StopDistance)
				{
					HankStats.Health -= HoardGenerator.zombDamage;
					QueueFree();
				}
			}
			else
			{
				// Check if we are currently falling down to a lower ledge
				bool isFalling = targetPoint.Y > GlobalPosition.Y + 2.0f && !IsCurrentlyOnLadder();

				if (isFalling)
				{
					// Move X and Y separately to simulate a natural arc/fall instead of a diagonal float
					float newX = Mathf.MoveToward(GlobalPosition.X, targetPoint.X, MoveSpeed * (float)delta);
					float newY = Mathf.MoveToward(GlobalPosition.Y, targetPoint.Y, FallSpeed * (float)delta);
					GlobalPosition = new Vector2(newX, newY);
				}
				else
				{
					// Normal walking/climbing
					GlobalPosition = GlobalPosition.MoveToward(targetPoint, speed * (float)delta);
				}

				// Flip Sprite
				if (GlobalPosition.X > targetPoint.X + 0.5f)
				{
					hankymylove.Scale = new Vector2(1, 1);
				}
				else if (GlobalPosition.X < targetPoint.X - 0.5f)
				{
					hankymylove.Scale = new Vector2(-1, 1);
				}

				// Slightly increased threshold (0.5f) to ensure they hit the waypoint smoothly while falling
				if (GlobalPosition.DistanceTo(targetPoint) < 0.5f)
					_pathIndex++;
			}
		}

		if (!IsCurrentlyOnLadder()) ApplyFloorSnap();
	}

	private void UpdatePathToPlayer(Vector2 targetPos)
	{
		long startId = _astar.GetClosestPoint(GlobalPosition);
		long endId = _astar.GetClosestPoint(targetPos);

		if (_astar.HasPoint(startId) && _astar.HasPoint(endId))
		{
			Vector2[] newPath = _astar.GetPointPath(startId, endId);
			
			if (newPath.Length > 1) 
			{
				_currentPath = newPath;
				_pathIndex = 1; 
			}
		}
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
		while (IsFloorTile(cell) && IsFloorTile(cell + Vector2I.Up)) { cell += Vector2I.Up; }
		if (IsFloorTile(cell))
		{
			Vector2 tileCenter = tileMap.MapToLocal(cell);
			float topToSurface = GetSurfaceOffsetFromTop(cell);
			float topOfTile = tileCenter.Y - (TILE_SIZE / 2f);
			float floorY = topOfTile + topToSurface;
			GlobalPosition = new Vector2(GlobalPosition.X, floorY - HANK_HALF_HEIGHT - EPSILON);
		}
	}
}
