using Godot;
using System;

public partial class BuildSystem : Node2D
{
	public static bool IsBuilding { get; private set; } = false;
	private bool _buildMode = false;
	private TileMap _tileMap;
	private Label _label;
	private Sprite2D _ghost;
	private bool _bWasPressed = false;
	private bool _rWasPressed = false;
	private bool _clickWasPressed = false;

	public override void _Ready()
	{
		_tileMap = GetTree().CurrentScene.GetNode<TileMap>("TileMap");

		// Ghost preview sprite (8x8 semi-transparent block)
		_ghost = new Sprite2D();
		var img = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8);
		img.Fill(new Color(1f, 1f, 1f, 0.5f));
		_ghost.Texture = ImageTexture.CreateFromImage(img);
		_ghost.Visible = false;
		_ghost.ZIndex = 10;
		GetTree().CurrentScene.AddChild(_ghost);

		// HUD label
		var canvas = new CanvasLayer();
		AddChild(canvas);
		_label = new Label();
		_label.Position = new Vector2(4, 4);
		_label.Visible = false;
		_label.Text = "Build Mode ON [click to place]";
		canvas.AddChild(_label);
	}

	public override void _Process(double delta)
	{
		// Toggle build mode with B
		bool bNow = Input.IsKeyPressed(Key.B);
		if (bNow && !_bWasPressed)
		{
			_buildMode = !_buildMode;
			IsBuilding = _buildMode;
			_label.Visible = _buildMode;
			_ghost.Visible = _buildMode;
			GD.Print($"Build mode: {_buildMode}");
		}
		_bWasPressed = bNow;

		// Reset with R
		bool rNow = Input.IsKeyPressed(Key.R);
		if (rNow && !_rWasPressed)
			GetTree().ReloadCurrentScene();
		_rWasPressed = rNow;

		// Update ghost position
		if (_buildMode)
		{
			Vector2 mouseWorld = _tileMap.GetGlobalMousePosition();
			Vector2I cell = _tileMap.LocalToMap(_tileMap.ToLocal(mouseWorld));
			Vector2 snapped = _tileMap.MapToLocal(cell);
			_ghost.GlobalPosition = _tileMap.ToGlobal(snapped);

			bool canPlace = CanPlaceAt(cell);
			_ghost.Modulate = canPlace ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.3f);
		}

		// Place tile with left click
		bool clickNow = Input.IsMouseButtonPressed(MouseButton.Left);
		if (_buildMode && clickNow && !_clickWasPressed)
		{
			PlaceTile();
		}
		_clickWasPressed = clickNow;
	}

	// Consume left clicks in build mode so mining doesn't trigger
	public override void _Input(InputEvent @event)
	{
		if (_buildMode && @event is InputEventMouseButton mouse
			&& mouse.ButtonIndex == MouseButton.Left)
		{
			GetViewport().SetInputAsHandled();
		}
	}

	private bool CanPlaceAt(Vector2I cell)
	{
		int sourceId = _tileMap.GetCellSourceId(0, cell);
		if (sourceId == -1)
			return true; // empty sky cell

		Vector2I atlas = _tileMap.GetCellAtlasCoords(0, cell);
		bool isBackground = atlas.X >= 4 && atlas.X <= 8 && atlas.Y >= 0 && atlas.Y <= 3;
		return isBackground;
	}

	private void PlaceTile()
	{
		Vector2 mouseWorld = _tileMap.GetGlobalMousePosition();
		Vector2I cell = _tileMap.LocalToMap(_tileMap.ToLocal(mouseWorld));

		if (!CanPlaceAt(cell))
		{
			GD.Print("Can't place here!");
			return;
		}

		float dist = _tileMap.ToGlobal(_tileMap.MapToLocal(cell)).DistanceTo(HankMovement.hankPosition);
		if (dist > 40f)
		{
			GD.Print("Too far away!");
			return;
		}

		var storage = GetNode<NewScript>("/root/Storage");
		if (!storage.Remove("tile", 1))
		{
			GD.Print("No tiles in storage!");
			return;
		}

		// Place stone tile (source 0, atlas 0,0)
		_tileMap.SetCell(0, cell, 0, new Vector2I(0, 0));
		GD.Print($"Placed tile at {cell}!");
	}
}
