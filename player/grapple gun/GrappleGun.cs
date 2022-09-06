using CaptainHookshot.player.grapple_gun.rope;
using Godot;

namespace CaptainHookshot.player.grapple_gun
{
    public class GrappleGun : Node2D
    {
        [Export(PropertyHint.Enum, "left,right")]
        private string _controlDirection;

        [Export] private float _hookExitSpeed;
        [Export] private int _ropeSegmentCount;

        [Export] private NodePath _spawnContainerPath;
        [Export] private PackedScene _ropeScene, _hookScene;

        private Node2D _spawnContainer;

        public override void _Ready()
        {
            _spawnContainer = GetNode<Node2D>(_spawnContainerPath);
        }

        public override void _Process(float delta)
        {
            var dir = GetAimInput();
            var shot = Input.IsActionJustPressed($"shoot_{_controlDirection}");

            if (dir != Vector2.Zero && shot) Shoot(dir);
        }

        private Vector2 GetAimInput()
        {
            return Input.GetVector
            (
                $"aim_{_controlDirection}_left",
                $"aim_{_controlDirection}_right",
                $"aim_{_controlDirection}_up",
                $"aim_{_controlDirection}_down"
            ).Normalized();
        }

        private void Shoot(Vector2 direction)
        {
            foreach (Node node in _spawnContainer.GetChildren())
            {
                _spawnContainer.RemoveChild(node);
                node.QueueFree();
            }

            var rope = _ropeScene.Instance<Rope>();
            rope.Init(_ropeSegmentCount, direction);
            _spawnContainer.AddChild(rope);

            var hook = _hookScene.Instance<Hook>();
            _spawnContainer.AddChild(hook);
            rope.AttachEndTo(hook);

            hook.Velocity = direction * _hookExitSpeed;
        }
    }
}