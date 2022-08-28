class_name Hook
extends KinematicBody2D


var velocity := Vector2.ZERO


func _physics_process(_delta: float) -> void:
	move_and_slide(velocity)
	look_at(global_position + velocity)
