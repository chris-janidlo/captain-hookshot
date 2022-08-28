extends Node2D


export var hook_exit_speed: float

export var rope_segment_count: int
export(String, "left", "right") var control_direction: String

onready var _rope_scene := preload("rope/Rope.tscn")
onready var _hook_scene := preload("Hook.tscn")

onready var _spawn_container := $"Spawn Container"


enum InputAction { Grab, Shoot }


func _process(_delta: float) -> void:
	var dir := _get_aim_input()
	var shot := Input.is_action_just_pressed(_full_action_name(InputAction.Shoot))

	if dir and shot:
		_shoot(dir)


func _get_aim_input() -> Vector2:
	var up := "aim_" + control_direction + "_up"
	var down := "aim_" + control_direction + "_down"
	var left := "aim_" + control_direction + "_left"
	var right := "aim_" + control_direction + "_right"

	return Input.get_vector(left, right, up, down)


func _full_action_name(action: int) -> String:
	var base_action_name: String

	match action:
		InputAction.Grab:
			base_action_name = "grab"
		InputAction.Shoot:
			base_action_name = "shoot"
		_:
			push_error("unexpected action type")
			return "ERROR"

	return base_action_name + "_" + control_direction


func _shoot(direction: Vector2) -> void:
	for n in _spawn_container.get_children():
		_spawn_container.remove_child(n)
		n.queue_free()
		
	direction = direction.normalized()

	var rope := _rope_scene.instance() as Rope
	rope.init(rope_segment_count, direction)
	_spawn_container.add_child(rope)

	var hook := _hook_scene.instance() as Hook
	_spawn_container.add_child(hook)
	rope.attach_end_to(hook)
	
	hook.velocity = direction * hook_exit_speed
