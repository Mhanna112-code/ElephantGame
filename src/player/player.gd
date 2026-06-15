extends CharacterBody2D

#Character & Movement
@export var Speed = 150
var chardir : Vector2
var Sprinting = false
@onready var character_sprite: Sprite2D = $Sprite2D
@export var MaxStamina = 100
var Stamina
var RegeneratingStamina = true
@onready var stamina_bar: TextureProgressBar = $CanvasLayer/StaminaBar
@onready var stamina_anim: AnimationPlayer = $CanvasLayer/StaminaAnim
@onready var player_anim: AnimationPlayer = $PlayerAnim


#Gun
var debounce
var WeaponOut = false
@onready var shoot_point: Marker2D = $WeaponOffset/WeaponPoint/WaterGun/ShootPoint
const WATER_BULLET = preload("uid://d3rv0ku666nm8")
@onready var gun_sound: AudioStreamPlayer2D = $GunSound
@onready var weapon_point: Marker2D = $WeaponOffset/WeaponPoint
@onready var weapon_offset: Marker2D = $WeaponOffset
@onready var water_gun: Node2D = $WeaponOffset/WeaponPoint/WaterGun
var MaxAmmo = 10
var Ammo = 15

#Panning
var desiredOffset: Vector2
var min_offset = -200.0
var max_offset = 150.0

func _ready() -> void:
	Ammo = MaxAmmo
	Stamina = MaxStamina
	stamina_bar.max_value = MaxStamina

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("Run"):
		if !WeaponOut:
			if RegeneratingStamina && stamina_anim.animation_finished:
				stamina_anim.play("StaminaBurn")
			Sprinting = true
	elif event.is_action_released("Run"):
		Sprinting = false
		$RegenTimer.start()
	if event.is_action_pressed("Aim"):
		if !Sprinting:
			water_gun.visible = true
			WeaponOut = true
			$Aim.visible = true
			Input.mouse_mode = Input.MOUSE_MODE_HIDDEN
	elif event.is_action_released("Aim"):
		water_gun.visible = false
		WeaponOut = false
		$Aim.visible = false
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		$Camera2D.global_position = global_position
	if event.is_action_pressed("Shoot") && WeaponOut:
		Shoot()

func _physics_process(delta: float) -> void:
	var mouseDir = weapon_offset.global_position.direction_to(get_global_mouse_position())
	
	chardir.x = Input.get_axis("ui_left", "ui_right")
	chardir.y = Input.get_axis("ui_up", "ui_down")
	
	weapon_offset.rotation = lerp_angle(weapon_offset.rotation, (get_global_mouse_position() - global_position).angle(), 10.5*delta)
	if WeaponOut:
		if mouseDir.x > 0:
			character_sprite.flip_h = false
			$WeaponOffset/WeaponPoint/WaterGun/WeaponSprite.flip_v = false
		else:
			character_sprite.flip_h = true
			$WeaponOffset/WeaponPoint/WaterGun/WeaponSprite.flip_v = true
	
	if chardir:
		velocity = chardir * Speed
		player_anim.play("Moving")
		if !WeaponOut:
			if chardir.x > 0:
				character_sprite.flip_h = false
			elif chardir.x < 0:
				character_sprite.flip_h = true
	else:
		velocity = velocity.move_toward(Vector2.ZERO, Speed)
		player_anim.play("Idle")
	
	if RegeneratingStamina && Stamina < MaxStamina:
		Stamina += delta + 0.1
	
	if Sprinting && Stamina > 0:
		Speed = 200
		Stamina -= delta + 0.25
		$RegenTimer.stop()
		RegeneratingStamina = false
	else:
		Speed = 150
		#print("StartRegen")
	move_and_slide()

func _process(_delta: float) -> void:
	stamina_bar.value = Stamina
	$Aim.global_position = get_global_mouse_position()
	if WeaponOut:
		cameraUpdate()
	queue_redraw()

func Shoot():
	if !debounce && Ammo > 0:
		debounce = true
		Ammo -= 1
		var BulletClone = WATER_BULLET.instantiate()
		BulletClone.global_position = shoot_point.global_position
		BulletClone.global_rotation = shoot_point.global_rotation
		get_tree().root.add_child(BulletClone)
		gun_sound.play()
		$TimeBetweenShots.start()

func _on_time_between_shots_timeout() -> void:
	debounce = false


func _on_regen_timer_timeout() -> void:
	RegeneratingStamina = true
	stamina_anim.play("StaminaRegen")
	#print("StartRegen")

func _draw() -> void:
	if WeaponOut:
		draw_line(to_local(shoot_point.global_position), to_local(get_global_mouse_position()), Color.WHITE)

func cameraUpdate():
	desiredOffset = (get_global_mouse_position() - $Camera2D.position) * 0.5
	desiredOffset.x = clamp(desiredOffset.x, min_offset, max_offset)
	desiredOffset.y = clamp(desiredOffset.y, min_offset / 2, max_offset / 2)
	
	$Camera2D.global_position = global_position + desiredOffset
