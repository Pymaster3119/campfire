using Godot;
using System;

public partial class AttributeBar : ProgressBar
{
	[Export] public Label ProgressBarText;
	[Export] public string attribute;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Value = HankStats.StatLevel(attribute);
		ProgressBarText.Text = attribute;
	}
}
