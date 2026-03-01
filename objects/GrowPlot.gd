extends Area2D

enum State { EMPTY, GROWING, READY }

var _state = State.EMPTY
var _grow_time: float = 30.0
var _grow_timer: float = 0.0
var _watered: bool = false
var _player_inside: bool = false
var _label: Label
var _sprite: Sprite2D

func _ready():
	_label = get_node("Label")
	_sprite = get_node("Sprite2D")
	_update_display()
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _on_body_entered(body):
	if body.name == "Hank":
		_player_inside = true

func _on_body_exited(body):
	if body.name == "Hank":
		_player_inside = false

func _process(delta):
	if _state != State.GROWING:
		return
	var speed = 2.0 if _watered else 1.0
	_grow_timer += delta * speed
	if _grow_timer >= _grow_time:
		_state = State.READY
		_update_display()

func _unhandled_input(event):
	if not _player_inside:
		return
	if not (event is InputEventKey and event.pressed):
		return
	var storage = get_node("/root/Storage")
	if event.keycode == KEY_E:
		match _state:
			State.EMPTY:
				if storage.remove("seed", 1):
					_state = State.GROWING
					_grow_timer = 0.0
					_watered = false
					_update_display()
					print("Planted a seed!")
				else:
					print("No seeds!")
			State.GROWING:
				if not _watered:
					_watered = true
					print("Watered the plot! Growing faster.")
					_update_display()
			State.READY:
				storage.add("food", 2)
				_state = State.EMPTY
				_watered = false
				_update_display()
				print("Harvested food!")

func _update_display():
	match _state:
		State.EMPTY:
			_label.text = "[E] Plant"
			_sprite.modulate = Color(0.6, 0.4, 0.2)
		State.GROWING:
			_label.text = "Growing ~" if _watered else "[E] Water"
			_sprite.modulate = Color(0.4, 0.7, 0.3)
		State.READY:
			_label.text = "[E] Harvest"
			_sprite.modulate = Color(1.0, 0.9, 0.2)