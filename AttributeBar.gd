extends ProgressBar

@export var progress_bar_text: Label
@export var attribute: String

func _process(delta):
	value = HankStats.stat_level(attribute)
	progress_bar_text.text = attribute
	if value < 20.0:
		_set_fill_color(Color.RED)
	elif value < 50.0:
		_set_fill_color(Color.YELLOW)
	else:
		_set_fill_color(Color.GREEN)

func _set_fill_color(new_color: Color):
	var fill_stylebox = get_theme_stylebox("fill") as StyleBoxFlat
	if fill_stylebox != null:
		var unique_stylebox = fill_stylebox.duplicate() as StyleBoxFlat
		unique_stylebox.bg_color = new_color
		add_theme_stylebox_override("fill", unique_stylebox)