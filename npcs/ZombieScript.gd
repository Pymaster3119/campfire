extends Node2D

@export var tile_map: TileMap
@export var move_speed: float = 15.0
@export var climb_speed: float = 15.0
@export var fall_speed: float = 45.0
@export var path_update_interval: float = 0.2
@export var stop_distance: float = 8.0
@export var max_fall_distance: int = 15
@export var hankymylove: AnimatedSprite2D

const TILE_SIZE = 8
const HANK_HALF_HEIGHT = 8
const EPSILON = 0.1

var _astar: AStar2D = AStar2D.new()
var _cell_to_id: Dictionary = {}
var _id_counter: int = 0
var _current_path: PackedVector2Array = PackedVector2Array()
var _path_index: int = 0
var _update_timer: float = 0.0
var _last_target_pos: Vector2 = Vector2.ZERO

func _ready():
	if tile_map == null:
		tile_map = get_tree().current_scene.get_node_or_null("TileMap")
	add_to_group("zombies")
	_build_graph()

func _is_floor_tile(cell: Vector2i) -> bool:
	var atlas = tile_map.get_cell_atlas_coords(0, cell)
	if atlas == Vector2i(-1, -1): return false
	if atlas == Vector2i(8, 1): return true
	return atlas.y > 1

func _is_ladder_tile(cell: Vector2i) -> bool:
	var atlas = tile_map.get_cell_atlas_coords(0, cell)
	if atlas == Vector2i(-1, -1): return false
	return atlas.x <= 3

func _get_surface_offset_from_top(cell: Vector2i) -> float:
	var atlas = tile_map.get_cell_atlas_coords(0, cell)
	if atlas.y > 1 and atlas != Vector2i(8, 1) and atlas != Vector2i(-1, -1):
		return 7.0
	return 0.0

func _build_graph():
	_cell_to_id.clear()
	_astar.clear()
	_id_counter = 0
	for cell in tile_map.get_used_cells(0):
		var is_floor = _is_floor_tile(cell)
		var is_ladder = _is_ladder_tile(cell)
		var is_surface = is_floor and not _is_floor_tile(cell + Vector2i(0, -1))
		if not is_surface and not is_ladder:
			continue
		var id = _id_counter
		_id_counter += 1
		_cell_to_id[cell] = id
		var tile_center = tile_map.map_to_local(cell)
		var top_to_surface = _get_surface_offset_from_top(cell)
		var top_of_tile = tile_center.y - (TILE_SIZE / 2.0)
		var standing_y = top_of_tile + top_to_surface - HANK_HALF_HEIGHT - EPSILON
		_astar.add_point(id, Vector2(tile_center.x, standing_y))
	for cell in _cell_to_id:
		_connect_neighbors(cell, _cell_to_id[cell])

func _connect_neighbors(cell: Vector2i, id: int):
	var directions = [Vector2i(-1,0), Vector2i(1,0), Vector2i(0,-1), Vector2i(0,1)]
	for dir in directions:
		var neighbor = cell + dir
		if _cell_to_id.has(neighbor):
			var allow = (dir == Vector2i(-1,0) or dir == Vector2i(1,0)) or \
						((dir == Vector2i(0,-1) or dir == Vector2i(0,1)) and _is_ladder_tile(cell) and _is_ladder_tile(neighbor))
			if allow:
				_astar.connect_points(id, _cell_to_id[neighbor], true)
		elif dir == Vector2i(-1,0) or dir == Vector2i(1,0):
			for i in range(1, max_fall_distance + 1):
				var drop_cell = neighbor + Vector2i(0, i)
				if _cell_to_id.has(drop_cell):
					_astar.connect_points(id, _cell_to_id[drop_cell], false)
					break
				if _is_floor_tile(drop_cell):
					break

func _process(delta):
	_update_timer += delta
	var target_hank = HankMovement.hank_position

	if _update_timer >= path_update_interval:
		if global_position.distance_to(target_hank) > stop_distance:
			_update_path_to_player(target_hank)
		_update_timer = 0.0

	if _current_path.size() > 0 and _path_index < _current_path.size():
		if (_current_path[_path_index].y > global_position.y + 2.0 and not _is_currently_on_ladder()) or _is_currently_on_ladder():
			hankymylove.play("default")
		else:
			hankymylove.play("run")

		var target_point = _current_path[_path_index]
		var speed = climb_speed if _is_currently_on_ladder() else move_speed

		if global_position.distance_to(target_hank) < stop_distance:
			_current_path = PackedVector2Array()
			HankStats.health -= HoardGenerator.zomb_damage
			queue_free()
		else:
			var is_falling = target_point.y > global_position.y + 2.0 and not _is_currently_on_ladder()
			if is_falling:
				var new_x = move_toward(global_position.x, target_point.x, move_speed * delta)
				var new_y = move_toward(global_position.y, target_point.y, fall_speed * delta)
				global_position = Vector2(new_x, new_y)
			else:
				global_position = global_position.move_toward(target_point, speed * delta)

			if global_position.x > target_point.x + 0.5:
				hankymylove.scale = Vector2(1, 1)
			elif global_position.x < target_point.x - 0.5:
				hankymylove.scale = Vector2(-1, 1)

			if global_position.distance_to(target_point) < 0.5:
				_path_index += 1

	if not _is_currently_on_ladder():
		_apply_floor_snap()

func _update_path_to_player(target_pos: Vector2):
	var start_id = _astar.get_closest_point(global_position)
	var end_id = _astar.get_closest_point(target_pos)
	if _astar.has_point(start_id) and _astar.has_point(end_id):
		var new_path = _astar.get_point_path(start_id, end_id)
		if new_path.size() > 1:
			_current_path = new_path
			_path_index = 1

func _is_currently_on_ladder() -> bool:
	var cell = tile_map.local_to_map(global_position)
	return _is_ladder_tile(cell)

func _apply_floor_snap():
	var probe_pos = global_position + Vector2(0, HANK_HALF_HEIGHT + 1.0)
	var cell = tile_map.local_to_map(probe_pos)
	while _is_floor_tile(cell) and _is_floor_tile(cell + Vector2i(0, -1)):
		cell += Vector2i(0, -1)
	if _is_floor_tile(cell):
		var tile_center = tile_map.map_to_local(cell)
		var top_to_surface = _get_surface_offset_from_top(cell)
		var top_of_tile = tile_center.y - (TILE_SIZE / 2.0)
		var floor_y = top_of_tile + top_to_surface
		global_position = Vector2(global_position.x, floor_y - HANK_HALF_HEIGHT - EPSILON)