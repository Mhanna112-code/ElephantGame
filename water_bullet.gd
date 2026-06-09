extends Node2D

@export var Speed = 500

func _physics_process(delta: float) -> void:
	position += transform.x * Speed * delta



func _on_timer_timeout() -> void:
	queue_free()
