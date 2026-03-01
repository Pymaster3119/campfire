extends ProgressBar

@export var progress_bar_label: Label

func _ready():
	show_percentage = false

func _process(delta):
	value = HoardGenerator.timer
	progress_bar_label.text = str(int(60 - HoardGenerator.timer)) + "s"