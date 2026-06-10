class_name RoomDoor extends Area2D
#region Exports
@export var target_room_name:String = ""
@export var target_entrance_id:int = 0
@export var active_by_default:bool = false
#endregion

@onready var level:Level = get_tree().current_scene

func toggle(value:bool) -> void:
	set_deferred_thread_group("monitoring", value)

func _ready() -> void:
	toggle(active_by_default)
	set_collision_mask_value(1, false)
	set_collision_mask_value(2, true)
	area_entered.connect(Callable(self, "_player_entered"))

func _player_entered(area:Area2D) -> void:
	if (area.get_parent() is Player):
		level.go_to_room(target_room_name, target_entrance_id)
