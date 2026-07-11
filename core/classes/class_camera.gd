class_name Camera extends Camera2D
#region Exports
@export var mode:MODES = MODES.PLAYER
@export var limits_already_enabled:bool = true
#endregion

#region Components
@onready var level:Level = get_tree().current_scene
@onready var player = $"../Player"
#endregion

#region Properties
var _def_limits = [-10000000, 10000000, -10000000, 10000000]
var _saved_limits = []
var is_limited:bool = false
#endregion

enum MODES {
	PLAYER,
	CUTSCENE
}

func toggle_limits(value:bool) -> void:
	is_limited = value
	var sl_limits = _saved_limits if is_limited else _def_limits
	set_limits(sl_limits[0], sl_limits[1], sl_limits[2], sl_limits[3])

func set_limits(top:int, bottom:int, left:int, right:int) -> void:
	if [top,bottom,left,right] != _def_limits:
		_saved_limits = [top,bottom,left,right]
	
	set("limit_top", top)
	set("limit_bottom", bottom)
	set("limit_left", left)
	set("limit_right", right)

func _ready():
	if limits_already_enabled:
		is_limited = true
		set_limits(_def_limits[0], _def_limits[1], _def_limits[2], _def_limits[3])

func _process(delta: float) -> void:
	match mode:
		MODES.PLAYER:
			global_position = player.global_position
