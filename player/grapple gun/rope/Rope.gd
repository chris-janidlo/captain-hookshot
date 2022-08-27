class_name Rope
extends Node2D


onready var _rope_segment_scene := preload("Rope Segment.tscn")

onready var _line := $Line as Line2D
onready var _joint := $Joint as Joint2D
onready var _rope_segments := []

var _segment_count: int


func _ready() -> void:
	print("rope")
	for i in _segment_count:
		var rope_segment := _rope_segment_scene.instance() as RopeSegment
		add_child(rope_segment)

		_rope_segments.append(rope_segment)
		_line.add_point(rope_segment.position)

		if i > 0:
			_rope_segments[i - 1].attach(rope_segment)
	
	_joint.node_b = _rope_segments[0].get_path()

	for i in _segment_count:
		_rope_segments[i].position = Vector2.ZERO
		_line.set_point_position(i, Vector2.ZERO)


func _physics_process(_delta: float) -> void:
	_animate_rope()


func init(segment_count: int) -> void:
	_segment_count = segment_count


func attach_to_end(node: RigidBody2D) -> void:
	_rope_segments[-1].attach(node)


func _animate_rope() -> void:
	for i in _segment_count:
		_line.set_point_position(i, _rope_segments[i].position)
