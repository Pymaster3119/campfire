using Godot;
using System;

public partial class BuildSystem : Node2D
{
	private bool _buildMode = false;
	private TileMap _tileMap;
	private Label _label;

	public override void _Ready()
	{
		_tileMap = GetTree().CurrentScene.GetNode<TileMap>("TileMap");

		var canvas = new CanvasLayer();
		AddChild(canvas);

		_label = new Label();
		_label.Position = new Vector2(4, 4);
		_label.Visible = false;
		_label.Text = "Build Mode ON";
		canvas.AddChild(_label);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo
			&& keyEvent.Keycode == Key.B)
		{
			_buildMode = !_buildMode;
			_label.Visible = _buildMode;
		}

		if (_buildMode && @event is InputEventMouseButton mouse
			&& mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
		{
			PlaceTile();
			GetViewport().SetInputAsHandled();
		}
	}

	private void PlaceTile()
	{
		Vector2 mousePos = GetGlobalMousePosition();

		if (mousePos.DistanceTo(HankMovement.hankPosition) > 40f)
		{
			GD.Print("Too far away!");
			return;
		}

		Vector2I cell = _tileMap.LocalToMap(mousePos);

		if (_tileMap.GetCellSourceId(0, cell) != -1)
		{
			GD.Print("Tile already occupied!");
			return;
		}

		var storage = GetNode<NewScript>("/root/Storage");
		if (!storage.Remove("tile", 1))
		{
			GD.Print("No tiles in storage!");
			return;
		}

		_tileMap.SetCell(0, cell, 0, new Vector2I(0, 0));
		GD.Print("Placed tile!");
	}
}
