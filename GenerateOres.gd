extends TileMap

@export var bronze_coordinates: Vector2i
@export var stone_coordinates: Vector2i
@export var iron_coordinates: Vector2i
@export var gunpowder_coordinates: Vector2i
@export var diamond_coordinates: Vector2i
@export var medpack_coordinates: Vector2i

@export var stone_probability: int
@export var bronze_probability: int
@export var iron_probability: int
@export var gunpowder_probability: int
@export var diamond_probability: int

@export var tile_map_width: int
@export var tile_map_height: int

static var stone_positions: Array = []
static var bronze_positions: Array = []
static var iron_positions: Array = []
static var gunpowder_positions: Array = []
static var diamond_positions: Array = []
static var medpack_positions: Array = []

func _ready():
	var source_id = max(0, get_cell_source_id(0, Vector2i(0, 0)))

	for x in range(-tile_map_width, tile_map_width):
		for y in range(4, tile_map_height + 4):
			var pos = Vector2i(x, y)
			var rand = randi_range(0, 100)

			if rand < stone_probability:
				set_cell(0, pos, source_id, stone_coordinates)
				stone_positions.append(pos)
			elif rand < bronze_probability:
				set_cell(0, pos, source_id, bronze_coordinates)
				bronze_positions.append(pos)
			elif rand < iron_probability:
				set_cell(0, pos, source_id, iron_coordinates)
				iron_positions.append(pos)
			elif rand < gunpowder_probability:
				set_cell(0, pos, source_id, gunpowder_coordinates)
				gunpowder_positions.append(pos)
			elif rand < diamond_probability:
				set_cell(0, pos, source_id, diamond_coordinates)
				diamond_positions.append(pos)
			else:
				set_cell(0, pos, source_id, medpack_coordinates)
				medpack_positions.append(pos)