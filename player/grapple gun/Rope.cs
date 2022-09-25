using Godot;

namespace CaptainHookshot.player.grapple_gun;

public class Rope : Node2D
{
    [Export] private int _verletNodeCount, _verletIterations;
    [Export] private float _verletGravityAccel, _verletNodeDistance;
    [Export] private Color _color;

    private Vector2[] _drawingPoints; // TODO: pool
    private VerletNode[] _nodes; // TODO: pool
    private Node2D _start, _end;

    private bool StartConnected => _start != null;
    private bool EndConnected => _end != null;

    public bool Taut { get; set; } = false;

    public override void _Ready()
    {
        _drawingPoints = new Vector2[_verletNodeCount];

        if (!Taut && _nodes == null) InitializeVerlet();
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!Taut) VerletIntegration(delta);

        Update();
    }

    public override void _Draw()
    {
        if (!Taut)
        {
            for (var i = 0; i < _nodes.Length; i++) _drawingPoints[i] = _nodes[i].Position;
        }
        else
        {
            _drawingPoints[0] = ToLocal(_start.GlobalPosition);
            _drawingPoints[1] = ToLocal(_end.GlobalPosition);
        }

        DrawPolyline(_drawingPoints, _color);
    }

    public void SetConnections(Node2D start = null, Node2D end = null)
    {
        _start = start;
        _end = end;
    }

    private void InitializeVerlet()
    {
        _nodes = new VerletNode[_verletNodeCount];

        for (var i = 0; i < _verletNodeCount; i++)
        {
            float
                radius = _verletNodeDistance,
                angle = GD.Randf() * Mathf.Tau;

            var randomCirclePoint = new Vector2(radius, 0).Rotated(angle);
            _nodes[i] = new VerletNode(randomCirclePoint);
        }
    }

    private void VerletIntegration(float delta)
    {
        if (_nodes == null) InitializeVerlet();

        Simulate(delta);
        for (var _ = 0; _ < _verletIterations; _++) Constrain(delta);
    }

    private void Simulate(float delta)
    {
        // ReSharper disable once ForCanBeConvertedToForeach (would need a variable anyway to modify the struct) 
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (i == 0 && StartConnected)
            {
                _nodes[i].Position = ToLocal(_start.GlobalPosition);
                continue;
            }

            if (i == _nodes.Length - 1 && EndConnected)
            {
                _nodes[i].Position = ToLocal(_end.GlobalPosition);
                continue;
            }

            var temp = _nodes[i].Position;
            _nodes[i].Position += _nodes[i].Position - _nodes[i].OldPosition +
                                  _verletGravityAccel * delta * delta * GlobalTransform.y;
            _nodes[i].OldPosition = temp;
        }
    }

    private void Constrain(float delta)
    {
        ConstrainNodeDistance(delta);
        // TODO: constrain to bounce angles when that's added
    }

    private void ConstrainNodeDistance(float delta)
    {
        for (var i = 0; i < _nodes.Length - 1; i++)
        {
            VerletNode
                nodeA = _nodes[i],
                nodeB = _nodes[i + 1];

            var distance = nodeA.Position.DistanceTo(nodeB.Position);
            if (distance == 0) continue;

            var dir = (nodeA.Position - nodeB.Position) / distance;
            var difference = _verletNodeDistance - distance;

            var weight = 0.5f;
            if (i == 0 && StartConnected) weight = 0;
            if (i == _nodes.Length - 2 && EndConnected) weight = 1;

            nodeA.Position += difference * weight * dir;
            nodeB.Position -= difference * (1 - weight) * dir;

            _nodes[i] = nodeA;
            _nodes[i + 1] = nodeB;
        }
    }

    private struct VerletNode
    {
        public Vector2 Position, OldPosition;

        public VerletNode(Vector2 startPosition)
        {
            OldPosition = Position = startPosition;
        }
    }
}