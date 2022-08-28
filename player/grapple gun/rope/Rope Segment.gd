class_name RopeSegment
extends RigidBody2D


onready var _joint := $Joint as Joint2D


func attach(other: PhysicsBody2D, move_segment_to_other: bool = false) -> void:
	if move_segment_to_other:
		global_position = other.global_position

	other.global_position = _joint.global_position
	_joint.node_b = other.get_path()
