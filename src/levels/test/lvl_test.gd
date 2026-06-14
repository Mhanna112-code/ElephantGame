extends Level

func _open_gate() -> void:
	print("Gate Open!")
	$"Rooms/Hall 3/Objects/Gate".queue_free()


func _finish_level() -> void:
	print_rich("[font_size=64]YOU WON![/font_size]")


func _trigger_train_exit() -> void:
	print("Exit Open!")
	$Rooms/Entrance/Objects/Trigger.toggle(true)
