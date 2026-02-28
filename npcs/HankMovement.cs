using Godot;
using System;

public partial class HankMovement : CharacterBody2D
{
	[Export] public float MoveSpeed = 60f;
	[Export] public float Gravity = 300f;
	[Export] public float ClimbSpeed = 50f;
	[Export] public float JumpForce = 130f;
	public static Vector2 hankPosition;

	private TileMap _tileMap;
	private float _yVelocity = 0f;
	private bool _jumpRequested = false;

	private const int TILE_SIZE = 8;

	public override void _Ready()
	{
		_tileMap = GetTree().CurrentScene.GetNode<TileMap>("TileMap");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.W)
		{
			if (IsOnSolidGround() && !IsOnLadder())
				_jumpRequested = true;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		bool onLadder = IsOnLadder();
		bool onFloor = IsOnSolidGround();

		// ── Horizontal ──
		float inputX = 0f;
		if (Input.IsKeyPressed(Key.A)) inputX -= 1f;
		if (Input.IsKeyPressed(Key.D)) inputX += 1f;

		if (inputX != 0f)
		{
			float newX = GlobalPosition.X + inputX * MoveSpeed * dt;
			if (!IsSolidAt(new Vector2(newX + inputX * 4f, GlobalPosition.Y))
			 && !IsSolidAt(new Vector2(newX + inputX * 4f, GlobalPosition.Y - 6f)))
			{
				GlobalPosition = new Vector2(newX, GlobalPosition.Y);
			}
		}

		// ── Vertical ──
		if (onLadder && (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.S)))
		{
			_yVelocity = 0f;
			float inputY = 0f;
			if (Input.IsKeyPressed(Key.W)) inputY -= 1f;
			if (Input.IsKeyPressed(Key.S)) inputY += 1f;
			GlobalPosition += new Vector2(0, inputY * ClimbSpeed * dt);
		}
		else
		{
			if (onFloor)
			{
				if (_yVelocity > 0f)
				{
					_yVelocity = 0f;
					SnapToFloor();
				}
				if (_jumpRequested)
				{
					_yVelocity = -JumpForce;
					_jumpRequested = false;
				}
			}
			else
			{
				_yVelocity += Gravity * dt;
				_jumpRequested = false;
			}

			GlobalPosition += new Vector2(0, _yVelocity * dt);
		}

		hankPosition = GlobalPosition;
	}

	// ── Tile checks ──

	private bool IsOnLadder()
	{
		Vector2I cell = _tileMap.LocalToMap(GlobalPosition);
		return IsLadderTile(cell);
	}

	private bool IsOnSolidGround()
	{
		Vector2 feetPos = GlobalPosition + new Vector2(0, 4f);
		Vector2I cellBelow = _tileMap.LocalToMap(feetPos);
		return IsFloorTile(cellBelow);
	}

	private bool IsSolidAt(Vector2 worldPos)
	{
		Vector2I cell = _tileMap.LocalToMap(worldPos);
		return IsFloorTile(cell);
	}

	private bool IsFloorTile(Vector2I cell)
	{
		Vector2I atlas = _tileMap.GetCellAtlasCoords(0, cell);
		if (atlas == new Vector2I(-1, -1)) return false;
		return atlas.Y <= 1;
	}

	private bool IsLadderTile(Vector2I cell)
	{
		Vector2I atlas = _tileMap.GetCellAtlasCoords(0, cell);
		if (atlas == new Vector2I(-1, -1)) return false;
		return atlas.X <= 3;
	}

	private void SnapToFloor()
	{
		Vector2 feetPos = GlobalPosition + new Vector2(0, 4f);
		Vector2I cell = _tileMap.LocalToMap(feetPos);
		Vector2 tileCenter = _tileMap.MapToLocal(cell);
		GlobalPosition = new Vector2(GlobalPosition.X, tileCenter.Y - 4f);
	}
}
