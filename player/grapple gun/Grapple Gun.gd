extends Node2D


export var rope_segment_count: int

onready var _rope_segment_scene := preload("Rope Segment.tscn")
onready var _hook_scene := preload("Hook.tscn")

onready var _line := $Line as Line2D
onready var _joint := $Joint as Joint2D

onready var _rope_segments := []


func _ready() -> void:
	_instantiate_rope()


func _physics_process(_delta: float) -> void:
	_animate_rope()


func _instantiate_rope() -> void:
	for i in rope_segment_count:
		var rope_segment := _rope_segment_scene.instance() as RopeSegment
		add_child(rope_segment)

		_rope_segments.append(rope_segment)
		_line.add_point(rope_segment.position)

		if i > 0:
			_rope_segments[i - 1].attach(rope_segment)
	
	var hook := _hook_scene.instance() as RigidBody2D
	add_child(hook)

	_joint.node_b = _rope_segments[0].get_path()
	_rope_segments[-1].attach(hook)

	# now scrunch the rope up
	# TODO: do this with a signal
	for i in rope_segment_count:
		_rope_segments[i].position = Vector2.ZERO
	hook.position = Vector2.ZERO


func _animate_rope() -> void:
	for i in rope_segment_count:
		_line.set_point_position(i, _rope_segments[i].position)
