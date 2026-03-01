using Godot;
using System;
using System.Collections.Generic;
 
public partial class HankMovement : Node2D
{
	[Export] public TileMap tileMap;
	[Export] public float MoveSpeed = 100f; // Increased for better feel in WASD
	[Export] public float ClimbSpeed = 80f;

	private Vector2I? _pendingBreakTile = null; 
	private Vector2I? _targetBreakTile = null;  

	public static Vector2 hankPosition;

	private const int TILE_SIZE = 8;
	private const int HANK_HALF_HEIGHT = 8; 
	private const float EPSILON = 0.1f;

	[Export] public float Gravity = 400f;
	[Export] public float JumpForce = 160f;
	private float _yVelocity = 0f;
	private bool _jumpPressed = false;

	[Export] public Control deathscreen;
	[Export] public Sprite2D hankymylove;
	[Export] public float BreakHoldTime = 0.8f; // How many seconds to hold
	private float _currentHoldTimer = 0f;
	private bool _isHolding = false;
	private Vector2I _miningTile;

	public override void _Ready()
	{
		if (tileMap == null)
			tileMap = GetTree().CurrentScene.GetNodeOrNull<TileMap>("TileMap");
	}


	private bool IsBackgroundTile(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		if (atlas == new Vector2I(-1, -1)) return true;
		return atlas.X >= 4 && atlas.X <= 7 && atlas.Y >= 0 && atlas.Y <= 3;
	}

	private bool IsFloorTile(Vector2I cell)
	{
		Vector2I atlas = tileMap.GetCellAtlasCoords(0, cell);
		if (atlas == new Vector2I(-1, -1)) return false;
		if (IsBackgroundTile(cell)) return false; 
		if (atlas == new Vector2I(8, 1)) return true; // Grass
		if (atlas == new Vector2I(8, 0)) return true; // Stone
		return atlas.Y > 1; 
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
		Vector2I atlas2 = tileMap.GetCellAtlasCoords(0, cell + new Vector2I(0,1));
		if (atlas2.X == 8 && atlas2.Y == 1)
		{
			hankymylove.Position = new Vector2(0,0);
		}
		else
		{
			hankymylove.Position = new Vector2(0,-1);
		}
		if (atlas.Y > 1 && atlas != new Vector2I(8, 1) && atlas != new Vector2I(-1, -1) && IsLadderTile(cell + new Vector2I(0,-1)))
		{
			GD.Print("hoeijwoifjeowijojifeoijgroijrwgjoidijowefxjiorjiotrwjoiefijowdefwoeij");
			return 8.0f; 
		}
		else if (atlas.Y > 1 && atlas != new Vector2I(8, 1) && atlas != new Vector2I(-1, -1)) 
			return 0.0f;
		
		return 0.0f;
	}

	public override void _Process(double delta)
	{
		UpdateUI();
		if (HankStats.Health <= 0) return;

		// --- HOLD TO BREAK LOGIC ---
		if (_isHolding)
		{
			// Check if mouse moved to a different tile while holding
			Vector2I currentHoverTile = tileMap.LocalToMap(GetGlobalMousePosition());
			if (currentHoverTile != _miningTile)
			{
				_isHolding = false;
				_currentHoldTimer = 0f;
			}
			else
			{
				_currentHoldTimer += (float)delta;
				if (_currentHoldTimer >= BreakHoldTime)
				{
					_isHolding = false; // Reset state
					ProcessResourceCollection(_miningTile);
					_targetBreakTile = _miningTile;
					PerformBreak();
				}
			}
		}

		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		
		Vector2I centerCell = tileMap.LocalToMap(GlobalPosition);
		Vector2I feetCell = tileMap.LocalToMap(GlobalPosition + new Vector2(0, HANK_HALF_HEIGHT));
		
		bool onLadder = IsLadderTile(centerCell) || IsLadderTile(feetCell);
		bool feetOnFloor = IsFloorTile(feetCell);

		Vector2 velocity = Vector2.Zero;

		if (inputDir.X != 0)
		{
			float moveStep = inputDir.X * MoveSpeed * (float)delta;
			Vector2 nextPos = GlobalPosition + new Vector2(moveStep, 0);
			
			if (CanMoveTo(nextPos))
			{
				velocity.X = moveStep;
			}
			else if (CanMoveTo(nextPos + new Vector2(0, -TILE_SIZE)))
			{
				// Only auto-step up if we aren't actively trying to climb/descend
				if (inputDir.Y == 0) 
				{
					velocity.X = moveStep;
					velocity.Y = -TILE_SIZE; 
				}
			}
		}

		if (onLadder && inputDir.Y != 0)
		{
			float climbStep = inputDir.Y * ClimbSpeed * (float)delta;
			Vector2 nextPos = GlobalPosition + new Vector2(0, climbStep);
			Vector2I nextFeetCell = tileMap.LocalToMap(nextPos + new Vector2(0, HANK_HALF_HEIGHT));

			if (inputDir.Y > 0) 
			{
				if (IsFloorTile(nextFeetCell))
				{
					velocity.Y = 0;
					ApplyFloorSnap(); 
				}
				else
				{
					velocity.Y = climbStep;
				}
			}
			else
			{
				if (CanMoveTo(nextPos))
				{
					velocity.Y = climbStep;
				}
			}
		}

		GlobalPosition += velocity;

		if (onLadder && inputDir.Y != 0)
		{
			_yVelocity = 0f;
		}
		else
		{
			_yVelocity += Gravity * (float)delta;

			if (feetOnFloor && _yVelocity >= 0f)
			{
				_yVelocity = 0f;
				ApplyFloorSnap();

				bool spaceNow = Input.IsKeyPressed(Key.Space);
				if (spaceNow && !_jumpPressed)
					_yVelocity = -JumpForce;
				_jumpPressed = spaceNow;
			}
			else
			{
				_jumpPressed = Input.IsKeyPressed(Key.Space);
			}

			GlobalPosition += new Vector2(0, _yVelocity * (float)delta);
		}

		hankPosition = GlobalPosition;
	}

	private bool CanMoveTo(Vector2 globalPos)
	{
		Vector2I cell = tileMap.LocalToMap(globalPos);
		// Hank can move if the tile is background OR a ladder
		return IsBackgroundTile(cell) || IsLadderTile(cell);
	}

	private void UpdateUI()
	{
		if(HankStats.Health > 0) deathscreen.Hide();
		else deathscreen.Show();
	}

	private void ApplyFloorSnap()
	{
		Vector2 probePos = GlobalPosition + new Vector2(0, HANK_HALF_HEIGHT + 1f);
		Vector2I cell = tileMap.LocalToMap(probePos);

		if (IsBackgroundTile(cell) && !IsLadderTile(cell))
			return;

		while (IsFloorTile(cell) && IsFloorTile(cell + Vector2I.Up)) cell += Vector2I.Up;

		if (IsFloorTile(cell))
		{
			Vector2 tileCenter = tileMap.MapToLocal(cell);
			float floorY = (tileCenter.Y - (TILE_SIZE / 2f)) + GetSurfaceOffsetFromTop(cell);
			GlobalPosition = new Vector2(GlobalPosition.X, floorY - HANK_HALF_HEIGHT - EPSILON);
		}
	}

	// --- INTERACTION ---

	private void ProcessResourceCollection(Vector2I tile)
	{
		HankStats.Hunger -= 5;
		if (GenerateOres.stonePositions.Contains(tile)) { GenerateOres.stonePositions.Remove(tile); ResourceManager.stone++; }
		else if (GenerateOres.bronzePositions.Contains(tile)) { GenerateOres.bronzePositions.Remove(tile); ResourceManager.bronze++; }
		else if (GenerateOres.ironPositions.Contains(tile)) { GenerateOres.ironPositions.Remove(tile); ResourceManager.iron++; }
		else if (GenerateOres.gunpowderPositions.Contains(tile)) { GenerateOres.gunpowderPositions.Remove(tile); ResourceManager.gunpowder++; }
		else if (GenerateOres.diamondPositions.Contains(tile)) { GenerateOres.diamondPositions.Remove(tile); ResourceManager.diamond++; }
		else if (GenerateOres.medpackPositions.Contains(tile)) { GenerateOres.medpackPositions.Remove(tile); ResourceManager.medkit++; }
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
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			if (mouseEvent.Pressed)
			{
				Vector2I cell = tileMap.LocalToMap(GetGlobalMousePosition());
				
				if (IsFloorTile(cell) && HankStats.Hunger > 0)
				{
					_isHolding = true;
					_miningTile = cell;
					_currentHoldTimer = 0f;
				}
			}
			else // Mouse Released
			{
				_isHolding = false;
				_currentHoldTimer = 0f;
			}
		}
	}
}
