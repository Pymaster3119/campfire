extends Node2D

@export var tile_map: TileMap
@export var move_speed: float = 100.0
@export var climb_speed: float = 80.0
@export var gravity: float = 400.0
@export var jump_force: float = 160.0
@export var break_hold_time: float = 0.8
@export var deathscreen: Control
@export var hankymylove: AnimatedSprite2D

static var hank_position: Vector2

const TILE_SIZE = 8
const HANK_HALF_HEIGHT = 8
const EPSILON = 0.1

var _y_velocity: float = 0.0
var _jump_pressed: bool = false
var _pending_break_tile = null
var _target_break_tile = null
var _current_hold_timer: float = 0.0
var _is_holding: bool = false
var _mining_tile: Vector2i

func _ready():
	if tile_map == null:
		tile_map = get_tree().current_scene.get_node_or_null("TileMap")

func _is_background_tile(cell: Vector2i) -> bool:
	var source_id = tile_map.get_cell_source_id(0, cell)
	if source_id == -1: return true
	if source_id == 0: return false
	var atlas = tile_map.get_cell_atlas_coords(0, cell)
	return atlas.x >= 4 and atlas.x <= 7 and atlas.y >= 0 and atlas.y <= 3

func _is_floor_tile(cell: Vector2i) -> bool:
	var source_id = tile_map.get_cell_source_id(0, cell)
	if source_id == -1: return false
	if source_id == 0: return true
	var atlas = tile_map.get_cell_atlas_coords(0, cell)
	if _is_background_tile(cell): return false
	if atlas == Vector2i(8, 1): return true
	if atlas == Vector2i(8, 0): return true
	return atlas.y > 1

func _is_ladder_tile(cell: Vector2i) -> bool:
	var source_id = tile_map.get_cell_source_id(0, cell)
	if source_id != 1: return false
	var atlas = tile_map.get_cell_atlas_coords(0, cell)
	return atlas.x <= 3

func _get_surface_offset_from_top(cell: Vector2i) -> float:
	var atlas = tile_map.get_cell_atlas_coords(0, cell)
	print(atlas)
	if atlas.x == 8 and atlas.y == 1:
		hankymylove.position = Vector2(0, -1)
	else:
		hankymylove.position = Vector2(0, -2)
	if atlas.y > 1 and atlas != Vector2i(8, 1) and atlas != Vector2i(-1, -1) and _is_ladder_tile(cell + Vector2i(0, -1)):
		return 8.0
	elif atlas.y > 1 and atlas != Vector2i(8, 1) and atlas != Vector2i(-1, -1):
		return 0.0
	return 0.0

func _process(delta):
	_update_ui()
	if HankStats.health <= 0:
		return

	if _is_holding:
		var current_hover_tile = tile_map.local_to_map(get_global_mouse_position())
		if current_hover_tile != _mining_tile:
			_is_holding = false
			_current_hold_timer = 0.0
		else:
			_current_hold_timer += delta
			if _current_hold_timer >= break_hold_time:
				_is_holding = false
				_process_resource_collection(_mining_tile)
				_target_break_tile = _mining_tile
				_perform_break()

		if Input.is_action_just_pressed("medkit"):
			if ResourceManager.medkit > 0:
				HankStats.health = 100
				ResourceManager.medkit -= 1

	var input_dir = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	if input_dir != Vector2.ZERO:
		hankymylove.play("run")
	else:
		hankymylove.play("default")

	var center_cell = tile_map.local_to_map(global_position)
	var feet_cell = tile_map.local_to_map(global_position + Vector2(0, HANK_HALF_HEIGHT))

	var on_ladder = _is_ladder_tile(center_cell) or _is_ladder_tile(feet_cell)
	var feet_on_floor = _is_floor_tile(feet_cell)

	var velocity = Vector2.ZERO

	if input_dir.x != 0:
		var move_step = input_dir.x * move_speed * delta
		var next_pos = global_position + Vector2(move_step, 0)
		if _can_move_to(next_pos):
			velocity.x = move_step
		elif _can_move_to(next_pos + Vector2(0, -TILE_SIZE)):
			if input_dir.y == 0:
				velocity.x = move_step
				velocity.y = -TILE_SIZE
		if velocity.x > 0:
			hankymylove.scale = Vector2(-1, 1)
		else:
			hankymylove.scale = Vector2(1, 1)

	if on_ladder and input_dir.y != 0:
		var climb_step = input_dir.y * climb_speed * delta
		var next_pos = global_position + Vector2(0, climb_step)
		var next_feet_cell = tile_map.local_to_map(next_pos + Vector2(0, HANK_HALF_HEIGHT))
		if input_dir.y > 0:
			if _is_floor_tile(next_feet_cell):
				velocity.y = 0
				_apply_floor_snap()
			else:
				velocity.y = climb_step
		else:
			if _can_move_to(next_pos):
				velocity.y = climb_step

	global_position += velocity

	if on_ladder and input_dir.y != 0:
		_y_velocity = 0.0
	else:
		_y_velocity += gravity * delta
		if feet_on_floor and _y_velocity >= 0.0:
			_y_velocity = 0.0
			_apply_floor_snap()
			var space_now = Input.is_key_pressed(KEY_SPACE) or Input.is_key_pressed(KEY_W)
			if space_now and not _jump_pressed:
				_y_velocity = -jump_force
			_jump_pressed = space_now
		else:
			_jump_pressed = Input.is_key_pressed(KEY_SPACE)
		global_position += Vector2(0, _y_velocity * delta)

	hank_position = global_position

func _can_move_to(global_pos: Vector2) -> bool:
	var cell = tile_map.local_to_map(global_pos)
	return _is_background_tile(cell) or _is_ladder_tile(cell)

func _update_ui():
	if HankStats.health > 0:
		deathscreen.hide()
	else:
		deathscreen.show()

func _apply_floor_snap():
	var probe_pos = global_position + Vector2(0, HANK_HALF_HEIGHT + 1.0)
	var cell = tile_map.local_to_map(probe_pos)
	if _is_background_tile(cell) and not _is_ladder_tile(cell):
		return
	while _is_floor_tile(cell) and _is_floor_tile(cell + Vector2i(0, -1)):
		cell += Vector2i(0, -1)
	if _is_floor_tile(cell):
		var tile_center = tile_map.map_to_local(cell)
		var floor_y = (tile_center.y - (TILE_SIZE / 2.0)) + _get_surface_offset_from_top(cell)
		global_position = Vector2(global_position.x, floor_y - HANK_HALF_HEIGHT - EPSILON)

func _process_resource_collection(tile: Vector2i):
	HankStats.hunger -= 5
	if GenerateOres.stone_positions.has(tile):
		GenerateOres.stone_positions.erase(tile)
		ResourceManager.stone += 1
	elif GenerateOres.bronze_positions.has(tile):
		GenerateOres.bronze_positions.erase(tile)
		ResourceManager.bronze += 1
	elif GenerateOres.iron_positions.has(tile):
		GenerateOres.iron_positions.erase(tile)
		ResourceManager.iron += 1
	elif GenerateOres.gunpowder_positions.has(tile):
		GenerateOres.gunpowder_positions.erase(tile)
		ResourceManager.gunpowder += 1
	elif GenerateOres.diamond_positions.has(tile):
		GenerateOres.diamond_positions.erase(tile)
		ResourceManager.diamond += 1
	elif GenerateOres.medpack_positions.has(tile):
		GenerateOres.medpack_positions.erase(tile)
		ResourceManager.medkit += 1

func _perform_break():
	var cell = _target_break_tile
	var source_id = max(0, tile_map.get_cell_source_id(0, cell))
	_update_background_at(cell, source_id)
	for dir in [Vector2i(0,-1), Vector2i(0,1), Vector2i(-1,0), Vector2i(1,0)]:
		if _is_background_tile(cell + dir):
			_update_background_at(cell + dir, source_id)
	_target_break_tile = null

func _update_background_at(cell: Vector2i, source_id: int):
	var top = _is_floor_tile(cell + Vector2i(0, -1))
	var bottom = _is_floor_tile(cell + Vector2i(0, 1))
	var left = _is_floor_tile(cell + Vector2i(-1, 0))
	var right = _is_floor_tile(cell + Vector2i(1, 0))
	var target_x = 7 if (left and right) else 4 if left else 6 if right else 5
	var target_y = 3 if (top and bottom) else 0 if top else 2 if bottom else 1
	tile_map.set_cell(0, cell, source_id, Vector2i(target_x, target_y))

func _unhandled_input(event):
	if BuildSystem.is_building:
		return
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			var cell = tile_map.local_to_map(get_global_mouse_position())
			if _is_floor_tile(cell) and HankStats.hunger > 0:
				_is_holding = true
				_mining_tile = cell
				_current_hold_timer = 0.0
		else:
			_is_holding = false
			_current_hold_timer = 0.0