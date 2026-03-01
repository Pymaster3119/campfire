using Godot;
using System;

public partial class BuildSystem : Node2D
{
	public static bool IsBuilding = false;
	private TileMap _tileMap;
	private bool _prevB = false;
	private bool _prevClick = false;
	private bool _prevR = false;

	public override void _Ready()
	{
		_tileMap = GetTree().CurrentScene.GetNode<TileMap>("TileMap");
		GD.Print(">>> BuildSystem READY <<<");
	}

	public override void _Process(double delta)
	{
		// B toggle
		bool b = Input.IsKeyPressed(Key.B);
		if (b && !_prevB)
		{
			IsBuilding = !IsBuilding;
			GD.Print($">>> Build mode: {IsBuilding} <<<");
		}
		_prevB = b;

		// R reset
		bool r = Input.IsKeyPressed(Key.R);
		if (r && !_prevR)
			GetTree().ReloadCurrentScene();
		_prevR = r;

		// Place on click
		bool click = Input.IsMouseButtonPressed(MouseButton.Left);
		if (IsBuilding && click && !_prevClick)
		{
			Vector2 mouse = GetGlobalMousePosition();
			Vector2I cell = _tileMap.LocalToMap(_tileMap.ToLocal(mouse));
			_tileMap.SetCell(0, cell, 0, new Vector2I(0, 0));
			GD.Print($">>> PLACED at {cell} <<<");
		}
		_prevClick = click;
	}
}
