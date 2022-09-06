using System.Linq;
using Godot;
using Godot.Collections;

namespace CaptainHookshot.player.grapple_gun.rope;

public class Rope : KinematicBody2D
{
    [Export] private NodePath _linePath, _jointPath;
    [Export] private PackedScene _ropeSegmentScene;

    private Joint2D _joint;
    private Line2D _line;

    private Vector2 _lookDirection;
    private Array<RopeSegment> _ropeSegments;

    public override void _Ready()
    {
        _joint = GetNode<Joint2D>(_jointPath);
        _line = GetNode<Line2D>(_linePath);

        SpawnRope();
    }

    public override void _PhysicsProcess(float delta)
    {
        AnimateRope();
    }

    public void Init(int segmentCount, Vector2 lookDirection)
    {
        _ropeSegments = new Array<RopeSegment>();
        _ropeSegments.Resize(segmentCount);
        _lookDirection = lookDirection;
    }

    public void AttachEndTo(PhysicsBody2D other)
    {
        _ropeSegments.Last().Attach(other);
    }

    private void SpawnRope()
    {
        LookAt(GlobalPosition + _lookDirection);

        for (var i = 0; i < _ropeSegments.Count; i++)
        {
            var ropeSegment = _ropeSegmentScene.Instance<RopeSegment>();
            AddChild(ropeSegment);
            _ropeSegments[i] = ropeSegment;

            if (i > 0) _ropeSegments[i - 1].Attach(ropeSegment);

            ropeSegment.Position = Vector2.Zero;
            _line.AddPoint(Vector2.Zero);
        }

        _joint.NodeB = _ropeSegments[0].GetPath();
    }

    private void AnimateRope()
    {
        for (var i = 0; i < _ropeSegments.Count; i++) _line.SetPointPosition(i, _ropeSegments[i].Position);
    }
}