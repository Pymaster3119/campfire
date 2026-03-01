extends Area2D

@export var level: int = 1

var _range: float = 60.0
var _fire_rate: float = 1.0
var _cooldown: float = 0.0
var _range_shape: CollisionShape2D

func _ready():
	_range_shape = get_node("RangeShape")
	_apply_level()

func _apply_level():
	_range = 60.0 + (level - 1) * 20.0
	_fire_rate = 1.0 - (level - 1) * 0.2
	if _range_shape and _range_shape.shape is CircleShape2D:
		_range_shape.shape.radius = _range
	print("Turret level ", level, ": range=", _range, " rate=", _fire_rate, "s")

func _process(delta):
	_cooldown -= delta
	if _cooldown > 0.0:
		return
	var target = _find_nearest_zombie()
	if target == null:
		return
	_delete_zombie(target)
	_cooldown = _fire_rate

func _find_nearest_zombie() -> Node2D:
	var closest = null
	var closest_dist = INF
	for node in get_tree().get_nodes_in_group("zombies"):
		if not node is Node2D:
			continue
		var dist = global_position.distance_to(node.global_position)
		if dist <= _range and dist < closest_dist:
			closest_dist = dist
			closest = node
	return closest

func _delete_zombie(target: Node2D):
	if is_instance_valid(target):
		if randi_range(1, 10) == 1:
			ResourceManager.medkit += 1
		target.queue_free()

func upgrade():
	if level >= 3:
		return
	var storage = get_node("/root/Storage")
	var cost = level * 2
	if not storage.remove("wood", cost):
		print("Need ", cost, " wood to upgrade turret")
		return
	level += 1
	_apply_level()

func _unhandled_input(event):
	if event is InputEventKey and event.pressed and event.keycode == KEY_U:
		var dist = global_position.distance_to(HankMovement.hank_position)
		if dist < 20.0:
			upgrade()