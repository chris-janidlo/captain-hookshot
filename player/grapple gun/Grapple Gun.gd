extends Node2D


export var rope_segment_count: int

onready var _rope_scene := preload("rope/Rope.tscn")


func _ready() -> void:
	var rope := _rope_scene.instance() as Rope
	rope.init(rope_segment_count)
	add_child(rope)
