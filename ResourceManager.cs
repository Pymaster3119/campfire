using Godot;
using System;

public partial class ResourceManager : Node2D
{
	public static int stone;
	public static int bronze;
	public static int iron;
	public static int gunpowder;
	public static int diamond;
	public static int medkit;

	public static int GetResource(string resourceName)
	{
		switch (resourceName.ToLower())
		{
			case "stone":
				return stone;

			case "bronze":
				return bronze;

			case "iron":
				return iron;

			case "gunpowder":
				return gunpowder;

			case "diamond":
				return diamond;

			case "medkit":
				return medkit;

			default:
				GD.Print("Invalid resource name.");
				return -1;
		}
	}
}
