extends Node2D

@export var Speed = 500

func _physics_process(delta: float) -> void:
	position += transform.x * Speed * delta



func _on_timer_timeout() -> void:
	queue_free()


func _on_area_2d_body_entered(body: Node2D) -> void:
	if !body.is_in_group("Player"):
		if body.is_in_group("Enemy"):
			body.Stun()
		else:
			queue_free()
