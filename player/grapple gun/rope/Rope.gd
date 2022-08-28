class_name Rope
extends Node2D


onready var _rope_segment_scene := preload("Rope Segment.tscn")

onready var _line := $Line as Line2D
onready var _joint := $Joint as Joint2D
onready var _rope_segments := []

var _segment_count: int
var _look_direction: Vector2


func _ready() -> void:
	look_at(global_position + _look_direction)

	for i in _segment_count:
		var rope_segment := _rope_segment_scene.instance() as RopeSegment
		add_child(rope_segment)

		_rope_segments.append(rope_segment)
		
		if i > 0:
			_rope_segments[i - 1].attach(rope_segment)
			
		rope_segment.position = Vector2.ZERO
		_line.add_point(Vector2.ZERO)
	
	_joint.node_b = _rope_segments[0].get_path()


func _physics_process(_delta: float) -> void:
	_animate_rope()


func init(segment_count: int, look_direction: Vector2) -> void:
	_segment_count = segment_count
	_look_direction = look_direction


func attach_end_to(node: PhysicsBody2D) -> void:
	_rope_segments[-1].attach(node, true)


func _animate_rope() -> void:
	for i in _segment_count:
		_line.set_point_position(i, _rope_segments[i].position)
