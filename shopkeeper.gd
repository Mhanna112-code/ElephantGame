extends Node2D

var InArea = false

func _on_area_2d_body_entered(body: Node2D) -> void:
	if body.is_in_group("Player"):
		InArea = true	


func _on_area_2d_body_exited(body: Node2D) -> void:
	if body.is_in_group("Player"):
		InArea = false	

#func _input(event: InputEvent) -> void:
	#if event.is_action_pressed("Interact") && InArea:
		
