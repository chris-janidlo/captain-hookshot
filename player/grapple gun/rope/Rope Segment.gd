class_name RopeSegment
extends RigidBody2D


onready var _joint := $Joint as Joint2D


func attach(other: RigidBody2D) -> void:
	other.global_position = _joint.global_position
	_joint.node_b = other.get_path()


func _on_scrunch_rope () -> void:
	position = Vector2.ZERO
