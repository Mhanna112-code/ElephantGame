class_name Trigger extends Area2D
signal triggered

#region Exports
@export var mode:MODES = MODES.AREA
@export var active:bool = true
@export var deactivate_on_trigger:bool = false
#endregion

#region Properties
var is_player_inside:bool = false
#endregion

enum MODES {
	AREA,	# Triggers when the player enters the area
	INPUT	# Triggered when the player enters the area AND presses the interact button
}

# Kinda useless architecture, but just to make the code look cool
func trigger() -> void:
	triggered.emit()
	
	if deactivate_on_trigger:
		toggle(false)

func toggle(value:bool) -> void:
	active = value
	set_deferred_thread_group("monitoring", active)

func _ready() -> void:
	toggle(active)
	set_collision_mask_value(1, false)
	set_collision_mask_value(2, true)
	area_entered.connect(Callable(self, "_player_entered"))

func _input(event:InputEvent) -> void:
	if event.is_action_pressed("Interact") && mode == MODES.INPUT:
		trigger()

func _player_entered(area:Area2D) -> void:
	if (area.get_parent() is Player):
		if mode == MODES.AREA:
			trigger()
		else:
			is_player_inside = true

func _player_exited(area:Area2D) -> void:
	if (area.get_parent() is Player):
		if is_player_inside:
			is_player_inside = false
