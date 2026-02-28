using Godot;
using System;

public partial class GrowPlot : Area2D
{
	private enum State { Empty, Growing, Ready }

	private State _state = State.Empty;
	private float _growTime = 30f;
	private float _growTimer = 0f;
	private bool _watered = false;
	private bool _playerInside = false;

	private Label _label;
	private Sprite2D _sprite;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		UpdateDisplay();

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.Name == "Hank")
			_playerInside = true;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body.Name == "Hank")
			_playerInside = false;
	}

	public override void _Process(double delta)
	{
		if (_state != State.Growing)
			return;

		float speed = _watered ? 2f : 1f;
		_growTimer += (float)delta * speed;

		if (_growTimer >= _growTime)
		{
			_state = State.Ready;
			UpdateDisplay();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_playerInside)
			return;

		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
			return;

		var storage = GetNode<NewScript>("/root/Storage");

		switch (keyEvent.Keycode)
		{
			case Key.E:
				if (_state == State.Empty)
				{
					if (storage.Remove("seed", 1))
					{
						_state = State.Growing;
						_growTimer = 0f;
						_watered = false;
						UpdateDisplay();
						GD.Print("Planted a seed!");
					}
					else
						GD.Print("No seeds!");
				}
				else if (_state == State.Ready)
				{
					storage.Add("food", 2);
					_state = State.Empty;
					_watered = false;
					UpdateDisplay();
					GD.Print("Harvested food!");
				}
				break;

			case Key.W:
				if (_state == State.Growing && !_watered)
				{
					_watered = true;
					GD.Print("Watered the plot! Growing faster.");
					UpdateDisplay();
				}
				break;
		}
	}

	private void UpdateDisplay()
	{
		switch (_state)
		{
			case State.Empty:
				_label.Text = "[E] Plant";
				_sprite.Modulate = new Color(0.6f, 0.4f, 0.2f);
				break;
			case State.Growing:
				_label.Text = _watered ? "Growing ~" : "[W] Water";
				_sprite.Modulate = new Color(0.4f, 0.7f, 0.3f);
				break;
			case State.Ready:
				_label.Text = "[E] Harvest";
				_sprite.Modulate = new Color(1f, 0.9f, 0.2f);
				break;
		}
	}
}
