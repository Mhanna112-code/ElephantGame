@tool
class_name Room extends Node2D
#region Exports
@export var entrances:Array[Vector2] # Stores all possible spawnpoint for the player when entering the room

@export_group("Camera Limits") # "Invisible Wall" for cameras
@export var camera_limits:bool = false
@export var cm_limit_top:float = 0.0
@export var cm_limit_bottom:float = 0.0
@export var cm_limit_left:float = 0.0
@export var cm_limit_right:float = 0.0

@export_group("Debug")
@export var draw_camera_limits:bool = false:
	set(value):
		draw_camera_limits = value
		queue_redraw()
@export var draw_entrance_ids:bool = false:
	set(value):
		draw_entrance_ids = value
		queue_redraw()
#endregion

#region Debug
var _cm_limits:Array[int]
var _entrances:Array[Vector2]
#endregion

# Global
var level:Level
var player:Player
var camera:Camera

func _ready() -> void:
	if Engine.is_editor_hint():
		_request_draw_camera_limits()
	else:
		level = get_tree().current_scene
		player = level.player
		camera = level.camera

func _process(delta: float) -> void:
	if Engine.is_editor_hint():
		_request_draw_camera_limits()
	else:
		_rm_update(delta)

func _draw() -> void:
	if draw_camera_limits && camera_limits:
		if _cm_limits.is_empty(): return
		
		var color = Color.YELLOW
		var width = 8
		
		var top_left_corner = to_local(Vector2(_cm_limits[2], _cm_limits[0]))
		var top_right_corner = to_local(Vector2(_cm_limits[3], _cm_limits[0]))
		var bottom_left_corner = to_local(Vector2(_cm_limits[2], _cm_limits[1]))
		var bottom_right_corner = to_local(Vector2(_cm_limits[3], _cm_limits[1]))
		
		var points = [top_left_corner, top_right_corner, bottom_right_corner, bottom_left_corner]
		if points.is_empty(): return
		draw_rect(
			Rect2(
				top_left_corner,
				bottom_right_corner - top_left_corner
			),
			color,
			false, # outline only
			width
		)
	
	if draw_entrance_ids:
		var font = SystemFont.new()
		for i in entrances.size():
			var text := str(i)
			var pos := to_local(entrances[i])
			draw_circle(pos, 8, Color.WHITE)
			var text_size := font.get_string_size(text)
			draw_string(font, pos + Vector2(-4, 4), text, HORIZONTAL_ALIGNMENT_CENTER, -1, 16, Color.BLACK)

func _request_draw_camera_limits() -> void:
	if draw_camera_limits:
		_cm_limits = [
			cm_limit_top,
			cm_limit_bottom,
			cm_limit_left,
			cm_limit_right
		]
	queue_redraw()

# Do not use this
func enter(entrance_id:int) -> void:
	if camera_limits:
		camera.set_limits(cm_limit_top, cm_limit_bottom, cm_limit_left, cm_limit_right)
	
	if entrances.is_empty():
		push_error("Not door with ID: %d in %s" % [entrance_id, name])
		player.global_position = global_position
	else:
		player.global_position = entrances[entrance_id]
	_enter()

# Neither this...
func exit() -> void:
	_exit()

# Override this when entering a room!
func _enter() -> void: pass

# Override this when exiting a room!
func _exit() -> void: pass

# Override for _process!
func _rm_update(delta) -> void: pass
