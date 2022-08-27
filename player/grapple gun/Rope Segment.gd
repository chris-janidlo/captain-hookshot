extends RigidBody2D
class_name RopeSegment


onready var _joint := $Joint as Joint2D


func attach(other: Node2D) -> void:
	other.global_position = _joint.global_position
	_joint.node_b = other.get_path()
