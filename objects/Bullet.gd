extends Area2D

var direction: Vector2 = Vector2.RIGHT
var speed: float = 150.0
var damage: float = 10.0
var _lifetime: float = 3.0

func _ready():
	var sprite = Sprite2D.new()
	var img = Image.create_empty(3, 3, false, Image.FORMAT_RGBA8)
	img.fill(Color.YELLOW)
	sprite.texture = ImageTexture.create_from_image(img)
	add_child(sprite)

	var shape = CollisionShape2D.new()
	var circle = CircleShape2D.new()
	circle.radius = 2.0
	shape.shape = circle
	add_child(shape)

	body_entered.connect(_on_body_entered)

func _process(delta):
	global_position += direction * speed * delta
	_lifetime -= delta
	if _lifetime <= 0.0:
		queue_free()

func _on_body_entered(body):
	if body.is_in_group("zombies"):
		body.queue_free()
		queue_free()