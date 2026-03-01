extends Node

static var hygiene: float = 100.0
static var education: float = 0.0
static var hunger: float = 100.0
static var health: float = 100.0

const DECAY_HYGIENE = 5.0
const DECAY_EDUCATION = 0.0
const DECAY_HUNGER = 10.0

var _is_critical: Dictionary = {}
var _is_warning: Dictionary = {}

signal stat_critical(stat_name)
signal stat_warning(stat_name)
signal stat_recovered(stat_name)

func _ready():
	hygiene = 100.0
	education = 0.0
	hunger = 100.0
	health = 100.0
	for s in ["hygiene", "entertainment", "education", "hunger", "health"]:
		_is_critical[s] = false
		_is_warning[s] = false

func _process(delta):
	var dt = delta / 60.0
	hygiene   = clamp(hygiene   - _get_modified_decay("hygiene",   DECAY_HYGIENE)   * dt, 0.0, 100.0)
	education = clamp(education - _get_modified_decay("education", DECAY_EDUCATION) * dt, 0.0, 100.0)
	hunger    = clamp(hunger    - _get_modified_decay("hunger",    DECAY_HUNGER)    * dt, 0.0, 100.0)
	_check_triggers()

func _get_modified_decay(stat: String, base_rate: float) -> float:
	var rate = base_rate
	if health < 30.0:
		rate *= 1.5
	if stat == "health":
		if hunger  < 20.0: rate *= 3.0
		if hygiene < 20.0: rate *= 2.0
	return rate

func _check_triggers():
	_check_stat("hygiene",   hygiene)
	_check_stat("education", education)
	_check_stat("hunger",    hunger)
	_check_stat("health",    health)

static func stat_level(name: String) -> float:
	match name:
		"health":    return health
		"hunger":    return hunger
		"education": return education
		"hygiene":   return hygiene
	return -100.0

func _check_stat(name: String, value: float):
	if value < 20.0 and not _is_critical[name]:
		_is_critical[name] = true
		emit_signal("stat_critical", name)
	elif value < 40.0 and not _is_warning[name]:
		_is_warning[name] = true
		emit_signal("stat_warning", name)
	if value > 50.0 and (_is_critical[name] or _is_warning[name]):
		_is_critical[name] = false
		_is_warning[name] = false
		emit_signal("stat_recovered", name)

static func modify_stat(stat: String, amount: float):
	match stat.to_lower():
		"hygiene":   hygiene   = clamp(hygiene   + amount, 0.0, 100.0)
		"education": education = clamp(education + amount, 0.0, 100.0)
		"hunger":    hunger    = clamp(hunger    + amount, 0.0, 100.0)
		"health":    health    = clamp(health    + amount, 0.0, 100.0)