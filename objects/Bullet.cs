using Godot;
using System;

public partial class Bullet : Area2D
{
	public Vector2 Direction = Vector2.Right;
	public float Speed = 150f;
	public float Damage = 10f;
	private float _lifetime = 3f;

	public override void _Ready()
	{
		var sprite = new Sprite2D();
		var img = Image.CreateEmpty(3, 3, false, Image.Format.Rgba8);
		img.Fill(Colors.Yellow);
		sprite.Texture = ImageTexture.CreateFromImage(img);
		AddChild(sprite);

		var shape = new CollisionShape2D();
		var circle = new CircleShape2D();
		circle.Radius = 2f;
		shape.Shape = circle;
		AddChild(shape);

		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		GlobalPosition += Direction * Speed * (float)delta;
		_lifetime -= (float)delta;
		if (_lifetime <= 0f)
			QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is ZombieScript zombie)
		{
			zombie.QueueFree();
			QueueFree();
		}
	}
}
