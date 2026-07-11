class_name Level extends Node2D
#region Exports
@export_group("Components")
@export var player:Player
@export var camera:Camera
@export var rooms:Node2D

@export_group("Settings")
@export var default_room_name:String
@export var default_entrance_id:int
#endregion

#region Properties
var current_room:Room = null
#endregion

#region Rooms Functionality
func go_to_room(room_name:String, entrance_id:int) -> void:
	if current_room != null:
		current_room.exit()
	current_room = get_room(room_name)
	current_room.enter(entrance_id)

func has_room(room_name:String) -> bool:
	return rooms.has_node(room_name)

func get_room(room_name:String) -> Room:
	if !has_room(room_name):
		push_error("Non existent room named: %s" % room_name)
		return null
	return rooms.get_node(room_name)
#endregion

func _ready() -> void:
	if !default_room_name.is_empty():
		go_to_room(default_room_name, default_entrance_id)
