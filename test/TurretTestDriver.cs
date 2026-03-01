extends Node2D

func _ready():
	HankMovement.hank_position = Vector2(100, 100)
	var storage = get_node("/root/Storage")
	storage.add("wood", 50)
	print("=== TURRET TEST ===")
	print("Zombies should get shot automatically.")
	print("Press U to upgrade turret (costs wood).")