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
	
	public void SetFillColor(Color newColor)
	{
		StyleBoxFlat fillStyleBox = GetThemeStylebox("fill") as StyleBoxFlat;

		if (fillStyleBox != null)
		{
			StyleBoxFlat uniqueFillStyleBox = (StyleBoxFlat)fillStyleBox.Duplicate();
			uniqueFillStyleBox.BgColor = newColor;
			AddThemeStyleboxOverride("fill", uniqueFillStyleBox);
		}
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Value = HankStats.StatLevel(attribute);
		ProgressBarText.Text = attribute;
		if (Value < 20.0f)
		{
			SetFillColor(Colors.Red);
		}
		else if (Value < 50.0f)
		{
			SetFillColor(Colors.Yellow);
		}
		else
		{
			SetFillColor(Colors.Green);
		}
	}
}
