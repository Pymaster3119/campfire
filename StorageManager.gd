extends Node

var _amounts: Dictionary = {}

func _ready():
	print("StorageManager READY (autoload works)")
	_amounts["wood"] = 25
	_amounts["tile"] = 50

func get_amount(id: String) -> int:
	return _amounts[id] if _amounts.has(id) else 0

func has_amount(id: String, amount: int) -> bool:
	return get_amount(id) >= amount

func add(id: String, amount: int):
	_amounts[id] = get_amount(id) + amount
	print("Added ", amount, " ", id, ". Now: ", _amounts[id])

func remove(id: String, amount: int) -> bool:
	if not has_amount(id, amount):
		return false
	_amounts[id] -= amount
	print("Removed ", amount, " ", id, ". Now: ", _amounts[id])
	return true