extends Area2D

const LOOT_TABLE = ["seed", "seed", "seed", "turret", "wood", "wood"]
var _player_inside: bool = false
var _opened: bool = false
var _label: Label

func _ready():
	_label = get_node("Label")
	_label.text = "?"
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _on_body_entered(body):
	if body.name == "Hank":
		_player_inside = true

func _on_body_exited(body):
	if body.name == "Hank":
		_player_inside = false

func _unhandled_input(event):
	if _opened or not _player_inside:
		return
	if event is InputEventKey and event.pressed and event.keycode == KEY_E:
		_open_crate()

func _open_crate():
	_opened = true
	var loot = LOOT_TABLE[randi() % LOOT_TABLE.size()]
	var amount = 1 if loot == "turret" else randi_range(1, 3)
	var storage = get_node("/root/Storage")
	storage.add(loot, amount)
	_label.text = "+%d %s" % [amount, loot]
	print("Crate opened! Got ", amount, "x ", loot)
	get_tree().create_timer(2.0).timeout.connect(queue_free)
