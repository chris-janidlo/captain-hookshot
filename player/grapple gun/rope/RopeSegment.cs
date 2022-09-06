using Godot;

namespace CaptainHookshot.player.grapple_gun.rope
{
    public class RopeSegment : RigidBody2D
    {
        [Export] private NodePath _jointPath;

        private Joint2D _joint;

        public override void _Ready()
        {
            _joint = GetNode<Joint2D>(_jointPath);
        }

        public void Attach(PhysicsBody2D other, bool moveSegmentToOther = false)
        {
            if (moveSegmentToOther)
                GlobalPosition = other.GlobalPosition;

            other.GlobalPosition = _joint.GlobalPosition;
            _joint.NodeB = other.GetPath();
        }
    }
}