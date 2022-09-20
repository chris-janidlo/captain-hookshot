using CaptainHookshot.player.grapple_gun;
using Godot;

namespace CaptainHookshot.player;

public class Player : Node2D
{
    [Export] private float _gravityAccel, _drag;
    [Export] private NodePath _leftGunPath, _rightGunPath, _bodyPath;

    [Export] private float _killFloor;
    [Export] private bool _debugRespawnMode;

    private KinematicBody2D _body;

    private GrappleGun _leftGun, _rightGun;
    private Vector2 _velocity, _lastFrameVelocity;

    public override void _Ready()
    {
        _leftGun = GetNode<GrappleGun>(_leftGunPath);
        _rightGun = GetNode<GrappleGun>(_rightGunPath);

        _body = GetNode<KinematicBody2D>(_bodyPath);
    }

    public override void _PhysicsProcess(float delta)
    {
        MoveBody(delta);
        ManageLifecycle();
        ReportPhysics();
    }

    private void MoveBody(float delta)
    {
        _lastFrameVelocity = _velocity;

        _velocity +=
            _leftGun.PullAcceleration +
            _rightGun.PullAcceleration +
            _gravityAccel * delta * Vector2.Down;

        // separate step so that drag is always up to date
        _velocity -= _velocity.Normalized() * _velocity.LengthSquared() * _drag * delta;

        _body.MoveAndCollide(_velocity * delta);
    }

    private void ManageLifecycle()
    {
        if (_body.GlobalPosition.y <= _killFloor) return;

        if (!_debugRespawnMode)
        {
            GD.Print("you died"); // TODO
        }
        else
        {
            _body.Position = Vector2.Zero;
            _velocity = Vector2.Zero;
            _lastFrameVelocity = Vector2.Zero; // to prevent weirdness with physics reports to guns
        }
    }

    private void ReportPhysics()
    {
        var report = new PhysicsReport
        {
            Position = _body.GlobalPosition,
            Velocity = _velocity,
            Acceleration = _lastFrameVelocity - _velocity
        };

        _leftGun.UpdatePhysicsReport(report);
        _rightGun.UpdatePhysicsReport(report);
    }
}