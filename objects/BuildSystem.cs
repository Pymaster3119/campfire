using Godot;
using System;
public partial class BuildSystem : Node2D
{
	public static bool IsBuilding = false;
	private TileMap _tileMap;
	private bool _prevB = false;
	private bool _prevClick = false;
	private bool _prevR = false;
	private bool _prevT = false;
	private bool _turretMode = false;
	private PackedScene _turretScene;
	public override void _Ready()
	{
		_tileMap = GetTree().CurrentScene.GetNode<TileMap>("TileMap");
		_turretScene = GD.Load<PackedScene>("res://objects/turret.tscn");
		GD.Print(">>> BuildSystem READY <<<");
	}
	public override void _Process(double delta)
	{
		// B toggle build mode
		bool b = Input.IsKeyPressed(Key.B);
		if (b && !_prevB)
		{
			IsBuilding = !IsBuilding;
			if (IsBuilding) _turretMode = false;
			GD.Print($">>> Build mode: {IsBuilding} <<<");
		}
		_prevB = b;
		// T toggle turret mode
		bool t = Input.IsKeyPressed(Key.T);
		if (t && !_prevT)
		{
			_turretMode = !_turretMode;
			if (_turretMode) IsBuilding = false;
			GD.Print($">>> Turret mode: {_turretMode} <<<");
		}
		_prevT = t;
		// R reset
		bool r = Input.IsKeyPressed(Key.R);
		if (r && !_prevR)
			GetTree().ReloadCurrentScene();
		_prevR = r;
		// Click to place
		bool click = Input.IsMouseButtonPressed(MouseButton.Left);
		if (click && !_prevClick)
		{
			if (IsBuilding)
			{
				Vector2 mouse = GetGlobalMousePosition();
				Vector2I cell = _tileMap.LocalToMap(_tileMap.ToLocal(mouse));
				_tileMap.SetCell(0, cell, 0, new Vector2I(0, 0));
				GD.Print($">>> PLACED tile at {cell} <<<");
			}
			else if (_turretMode)
			{
				PlaceTurret();
			}
		}
		_prevClick = click;
	}
	private void PlaceTurret()
	{
		if (_turretScene == null)
		{
			GD.Print("Turret scene not loaded!");
			return;
		}
		Vector2 mouse = GetGlobalMousePosition();
		Vector2I cell = _tileMap.LocalToMap(_tileMap.ToLocal(mouse));
		Vector2 snapped = _tileMap.ToGlobal(_tileMap.MapToLocal(cell));
		var turret = _turretScene.Instantiate<Turret>();
		GetTree().CurrentScene.AddChild(turret);
		turret.GlobalPosition = snapped;
		GD.Print($">>> PLACED turret at {cell} <<<");
	}
}
