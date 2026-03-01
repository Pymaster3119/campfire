extends Node2D

static var is_building: bool = false
var _tile_map: TileMap
var _prev_b: bool = false
var _prev_click: bool = false
var _prev_r: bool = false
var _prev_t: bool = false
var _turret_mode: bool = false
var _turret_scene: PackedScene

func _ready():
	_tile_map = get_tree().current_scene.get_node("TileMap")
	_turret_scene = load("res://objects/turret.tscn")
	print(">>> BuildSystem READY <<<")

func _process(delta):
	# B toggle build mode
	var b = Input.is_key_pressed(KEY_B)
	if b and not _prev_b:
		is_building = !is_building
		if is_building:
			_turret_mode = false
		print(">>> Build mode: ", is_building, " <<<")
	_prev_b = b

	# T toggle turret mode
	var t = Input.is_key_pressed(KEY_T)
	if t and not _prev_t:
		_turret_mode = !_turret_mode
		if _turret_mode:
			is_building = false
		print(">>> Turret mode: ", _turret_mode, " <<<")
	_prev_t = t

	# R reset
	var r = Input.is_key_pressed(KEY_R)
	if r and not _prev_r:
		get_tree().reload_current_scene()
	_prev_r = r

	# Click to place
	var click = Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT)
	if click and not _prev_click:
		if is_building:
			var mouse = get_global_mouse_position()
			var cell = _tile_map.local_to_map(_tile_map.to_local(mouse))
			_tile_map.set_cell(0, cell, 0, Vector2i(0, 0))
			print(">>> PLACED tile at ", cell, " <<<")
		elif _turret_mode:
			_place_turret()
	_prev_click = click

func _place_turret():
	if _turret_scene == null:
		print("Turret scene not loaded!")
		return
	var mouse = get_global_mouse_position()
	var cell = _tile_map.local_to_map(_tile_map.to_local(mouse))
	var snapped = _tile_map.to_global(_tile_map.map_to_local(cell))
	var turret = _turret_scene.instantiate()
	get_tree().current_scene.add_child(turret)
	turret.global_position = snapped
	print(">>> PLACED turret at ", cell, " <<<")
