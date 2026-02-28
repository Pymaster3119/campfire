using Godot;
using System;

public partial class BuildSystem : Node2D
{
	private bool _buildMode = false;
	private string _selectedItem = "turret";
	private PackedScene _turretScene;
	private TileMap _tileMap;
	private Label _label;

	public override void _Ready()
	{
		_turretScene = GD.Load<PackedScene>("res://objects/turret.tscn");
		_tileMap = GetTree().CurrentScene.GetNode<TileMap>("TileMap");

		var canvas = new CanvasLayer();
		AddChild(canvas);

		_label = new Label();
		_label.Position = new Vector2(4, 4);
		_label.Text = "";
		canvas.AddChild(_label);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			switch (keyEvent.Keycode)
			{
				case Key.B:
					_buildMode = !_buildMode;
					UpdateLabel();
					break;
				case Key.Key1:
					if (_buildMode) { _selectedItem = "turret"; UpdateLabel(); }
					break;
				case Key.Key2:
					if (_buildMode) { _selectedItem = "tile"; UpdateLabel(); }
					break;
			}
		}

		if (_buildMode && @event is InputEventMouseButton mouse
			&& mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
		{
			PlaceItem();
			GetViewport().SetInputAsHandled();
		}
	}

	private void PlaceItem()
	{
		var storage = GetNode<NewScript>("/root/Storage");
		Vector2 mousePos = GetGlobalMousePosition();

		if (mousePos.DistanceTo(HankMovement.hankPosition) > 40f)
		{
			GD.Print("Too far away!");
			return;
		}

		switch (_selectedItem)
		{
			case "turret":
				if (!storage.Remove("turret", 1))
				{
					GD.Print("No turrets in storage!");
					return;
				}
				var turret = _turretScene.Instantiate<Node2D>();
				Vector2I tCell = _tileMap.LocalToMap(mousePos);
				turret.GlobalPosition = _tileMap.MapToLocal(tCell);
				GetTree().CurrentScene.AddChild(turret);
				GD.Print("Placed turret!");
				break;

			case "tile":
				if (!storage.Remove("wood", 1))
				{
					GD.Print("No wood in storage!");
					return;
				}
				Vector2I cell = _tileMap.LocalToMap(mousePos);
				_tileMap.SetCell(0, cell, 0, new Vector2I(0, 0));
				GD.Print("Placed tile!");
				break;
		}
	}

	private void UpdateLabel()
	{
		_label.Text = _buildMode ? $"Build Mode: {_selectedItem} (1=turret 2=tile)" : "";
	}
}
