using Godot;
using System;
using System.Collections.Generic;

public partial class HankMovement : Node2D
{
	[Export] public TileMap tileMap;
	[Export] public float MoveSpeed = 100f; // Increased for better feel in WASD
	[Export] public float ClimbSpeed = 80f;

	private Button _breakButton;
	private Vector2I? _pendingBreakTile = null; 
	private Vector2I? _targetBreakTile = null;  

	public static Vector2 hankPosition;

	private const int TILE_SIZE = 8;
	private const int HANK_HALF_HEIGHT = 8; 
	private const float EPSILON = 0.1f; 
	
	[Export] public Control deathscreen;

	public override void _Ready()
	{
		if (tileMap == null)
			tileMap = GetTree().CurrentScene.GetNodeOrNull<TileMap>("TileMap");
		
		SetupBreakButton();
		// We no longer need BuildGraph() for WASD movement!
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

	// --- TILE CLASSIFICATION (Unchanged from your original) ---

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
		if (atlas.Y > 1 && atlas != new Vector2I(8, 1) && atlas != new Vector2I(-1, -1) && IsLadderTile(cell + new Vector2I(0,-1)))
		{
			GD.Print("hoeijwoifjeowijojifeoijgroijrwgjoidijowefxjiorjiotrwjoiefijowdefwoeij");
			return 7.0f; 
		}
		else if (atlas.Y > 1 && atlas != new Vector2I(8, 1) && atlas != new Vector2I(-1, -1)) 
			return 1.0f;
			
		return 0.0f;
	}

	public override void _Process(double delta)
	{
		UpdateUI();
		if (HankStats.Health <= 0) return;

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

		if (!onLadder || (onLadder && inputDir.Y == 0 && feetOnFloor))
		{
			ApplyFloorSnap();
			GD.Print("wallah");
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

		// If falling through air
		if (IsBackgroundTile(cell) && !IsLadderTile(cell))
		{
			GlobalPosition += new Vector2(0, 2f); // Simple gravity fall
			return;
		}

		while (IsFloorTile(cell) && IsFloorTile(cell + Vector2I.Up)) cell += Vector2I.Up;

		if (IsFloorTile(cell))
		{
			Vector2 tileCenter = tileMap.MapToLocal(cell);
			float floorY = (tileCenter.Y - (TILE_SIZE / 2f)) + GetSurfaceOffsetFromTop(cell);
			GlobalPosition = new Vector2(GlobalPosition.X, floorY - HANK_HALF_HEIGHT - EPSILON);
		}
	}

	// --- INTERACTION ---

	private void OnBreakButtonPressed()
	{
		if (!_pendingBreakTile.HasValue) return;
		
		// Resource logic remains the same...
		ProcessResourceCollection(_pendingBreakTile.Value);

		_targetBreakTile = _pendingBreakTile;
		_breakButton.Visible = false;
		
		// Instead of pathfinding, we just let the player walk there manually
		// But we set _targetBreakTile so PerformBreak triggers when they get close
	}

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
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			Vector2 clickPos = GetGlobalMousePosition();
			Vector2I cell = tileMap.LocalToMap(clickPos);

			if (IsFloorTile(cell) && HankStats.Hunger > 0)
			{
				_pendingBreakTile = cell;
				_breakButton.GlobalPosition = tileMap.MapToLocal(cell) + new Vector2(-20, -25);
				_breakButton.Visible = true;
			}
			else
			{
				_breakButton.Visible = false;
			}
		}
	}
}
