class_name Player extends CharacterBody2D

#Character & Movement
@export var Speed = 150
var chardir : Vector2
var Sprinting = false
@onready var character_sprite: Sprite2D = $Sprite2D
@export var MaxStamina = 100
var Stamina
var RegeneratingStamina = true
@onready var stamina_bar: TextureProgressBar = $CanvasLayer/StaminaBar

#Gun
var debounce
var WeaponOut = false
@onready var shoot_point: Marker2D = $WeaponOffset/WeaponPoint/WaterGun/ShootPoint
const WATER_BULLET = preload("uid://d3rv0ku666nm8")
@onready var weapon_point: Marker2D = $WeaponOffset/WeaponPoint
@onready var weapon_offset: Marker2D = $WeaponOffset
@onready var water_gun: Node2D = $WeaponOffset/WeaponPoint/WaterGun

#Progress
var has_water_gun:bool = false

func _ready() -> void:
	Stamina = MaxStamina
	stamina_bar.max_value = MaxStamina

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("Run"):
		if !WeaponOut:
			Sprinting = true
			Speed = 200
	elif event.is_action_released("Run"):
		Sprinting = false
		Speed = 150
	if event.is_action_pressed("Aim"):
		if !Sprinting:
			water_gun.visible = true
			WeaponOut = true
	elif event.is_action_released("Aim"):
		water_gun.visible = false
		WeaponOut = false
	if event.is_action_pressed("Shoot") && WeaponOut:
		Shoot()

func _physics_process(delta: float) -> void:
	var mouseDir = weapon_offset.global_position.direction_to(get_global_mouse_position())
	
	chardir.x = Input.get_axis("ui_left", "ui_right")
	chardir.y = Input.get_axis("ui_up", "ui_down")
	
	weapon_offset.rotation = lerp_angle(weapon_offset.rotation, (get_global_mouse_position() - global_position).angle(), 10.5*delta)
	if mouseDir.x > 0:
		character_sprite.flip_h = false
	else:
		character_sprite.flip_h = true
	
	if chardir:
		velocity = chardir * Speed
	else:
		velocity = velocity.move_toward(Vector2.ZERO, Speed)
	
	if RegeneratingStamina && Stamina < MaxStamina:
		Stamina += delta + 0.1
	
	# Supossedly fixed so stamina doesn't waste when not running
	if (Sprinting && Stamina > 0) && velocity.length() > 0:
		Speed = 200
		Stamina -= delta + 0.25
		$RegenTimer.stop()
		RegeneratingStamina = false
	else:
		Speed = 150
		if $RegenTimer.is_stopped():
			$RegenTimer.start()
	move_and_slide()

func _process(_delta: float) -> void:
	stamina_bar.value = Stamina

func Shoot():
	if !debounce:
		debounce = true
		var BulletClone = WATER_BULLET.instantiate()
		BulletClone.global_position = shoot_point.global_position
		BulletClone.global_rotation = shoot_point.global_rotation
		get_tree().root.add_child(BulletClone)
		$TimeBetweenShots.start()

func _on_time_between_shots_timeout() -> void:
	debounce = false

func _on_regen_timer_timeout() -> void:
	RegeneratingStamina = true
