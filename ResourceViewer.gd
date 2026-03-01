extends TextureRect

@export var inventory_thingy: Label
@export var resource: String

func _process(delta):
	inventory_thingy.text = str(ResourceManager.get_resource(resource))