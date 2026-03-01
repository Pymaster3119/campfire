extends Node2D

static var stone: int = 0
static var bronze: int = 0
static var iron: int = 0
static var gunpowder: int = 0
static var diamond: int = 0
static var medkit: int = 0

static func get_resource(resource_name: String) -> int:
	match resource_name.to_lower():
		"stone":    return stone
		"bronze":   return bronze
		"iron":     return iron
		"gunpowder": return gunpowder
		"diamond":  return diamond
		"medkit":   return medkit
		_:
			print("Invalid resource name.")
			return -1