using Godot;
using System;

public partial class TimeToNextHoarde : ProgressBar
{
	
	[Export] public Label ProgressBarLabel;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ShowPercentage = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Value = HoardGenerator.timer;
		ProgressBarLabel.Text = (int)(60 - HoardGenerator.timer) + "s";
	}
	
}
