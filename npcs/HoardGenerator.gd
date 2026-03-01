extends Node2D

@export var hoarde_time: float = 60.0
@export var zomb_template: PackedScene

static var timer: float = 0.0
static var zomb_damage: int = 50

var zomb_number: int = 5
var hoard_number: int = 0

func _process(delta):
	timer += delta
	if timer >= hoarde_time:
		hoard_number += 1
		for i in range(zomb_number):
			var enemy = zomb_template.instantiate()
			var random_x = randf_range(-50, 50)
			var random_y = -56
			enemy.position = Vector2(random_x, random_y)
			get_tree().current_scene.add_child(enemy)
		timer = 0
		zomb_number = max(100, zomb_number * 2)
		zomb_damage = int(0.5 * hoard_number * hoard_number) + 50