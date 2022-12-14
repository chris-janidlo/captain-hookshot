using System;
using CaptainHookshot.tools;
using Godot;

namespace CaptainHookshot.player.grapple_gun;

public class GrappleGun : Node2D
{
    private static readonly Vector2 FlippedYScale = new(1, -1), UnflippedYScale = new(1, 1);

    [Export(PropertyHint.Enum, "left,right")]
    private string _controlDirection;

    [Export] private NodePath _hookFlightContainerPath;

    [Export] private float _hookExitSpeed, _maxRopeLength, _hookRetractSpeed;
    [Export] private float _hookRetractCooldown, _hookPullAccel, _hookPullStopDistance;
    [Export] private int _ropeSegmentCount, _aimSnapRegions;

    [Export] private NodePath _hookPath, _barrelPath;

    [Export] private PackedScene _ropeScene;

    private Vector2 _aim;
    private Node2D _barrel, _hookFlightContainer;
    private CrateMachine<GrappleGun> _crateMachine;
    private Hook _hook;
    private PhysicsReport _playerPhysicsReport;
    private bool _shot, _shooting;

    public Vector2 PullAcceleration { get; private set; }
    public bool Braking { get; private set; }

    public override void _Ready()
    {
        _hook = GetNode<Hook>(_hookPath);
        _barrel = GetNode<Node2D>(_barrelPath);
        _hookFlightContainer = GetNode<Node2D>(_hookFlightContainerPath);

        _aim = Vector2.Right;

        _crateMachine = new CrateMachine<GrappleGun>(this)
            .SetInitialCrate<Idle>()
            .AddCrate(new Idle())
            .AddCrate(new Shooting())
            .AddCrate(new Retracting())
            .AddCrate(new LooseRope());
    }

    public override void _Process(float delta)
    {
        ManageInput();
        _crateMachine.Process(delta, ProcessType.Frame);
    }

    public override void _PhysicsProcess(float delta)
    {
        _crateMachine.Process(delta, ProcessType.Physics);
    }

    public void UpdatePhysicsReport(PhysicsReport report)
    {
        _playerPhysicsReport = report;
    }

    private void ManageInput()
    {
        var raw = Input.GetVector
        (
            $"aim_{_controlDirection}_left",
            $"aim_{_controlDirection}_right",
            $"aim_{_controlDirection}_up",
            $"aim_{_controlDirection}_down"
        );

        if (raw != Vector2.Zero)
        {
            var snapAngle = Mathf.Stepify(raw.Angle(), 2 * Mathf.Pi / _aimSnapRegions);
            _aim = Vector2.Right.Rotated(snapAngle).Normalized();
        }

        _shot = Input.IsActionJustPressed($"shoot_{_controlDirection}");
        _shooting = Input.IsActionPressed($"shoot_{_controlDirection}");
    }

    private class Idle : Crate<GrappleGun>
    {
        public override Type GetTransition()
        {
            return C._shooting ? typeof(Shooting) : base.GetTransition();
        }

        public override void OnProcessFrame(float delta)
        {
            C.LookAt(C.GlobalPosition + C._aim);

            const float up = Mathf.Pi / 2;
            var flip = C._controlDirection == "left"
                ? C._aim.Angle() is < -up or >= up
                : C._aim.Angle() is <= -up or > up;

            // flip the sprite about its origin by inverting its scale
            C.Scale = flip ? FlippedYScale : UnflippedYScale;
        }
    }

    private class Shooting : Crate<GrappleGun>
    {
        private bool _atEndOfRope;
        private float _cooldownTimer;
        private Vector2 _hookVelocity;
        private Rope _rope;

        public override Type GetTransition()
        {
            switch (true)
            {
                case true when _atEndOfRope && !C._shooting:
                case true when _cooldownTimer <= 0 && C._shooting && C._hook.TouchingHookable:
                    return typeof(Retracting);

                case true when _atEndOfRope && C._shooting:
                    return typeof(LooseRope);

                case true when _cooldownTimer <= 0 && C._shot:
                    return typeof(Shooting);

                default:
                    return base.GetTransition();
            }
        }

        public override void OnEnter()
        {
            var hook = C._hook;
            hook.GetParent().RemoveChild(hook);
            C._hookFlightContainer.AddChild(hook);
            hook.Scale = Vector2.One;
            hook.GlobalPosition = C._barrel.GlobalPosition;

            _hookVelocity = C._aim * C._hookExitSpeed;
            hook.LookAt(hook.GlobalPosition + _hookVelocity);

            _rope = C._ropeScene.Instance<Rope>();
            C._barrel.AddChild(_rope);
            _rope.SetConnections(C._barrel, hook);

            _cooldownTimer = C._hookRetractCooldown;
        }

        public override void OnProcessPhysics(float delta)
        {
            C._hook.MoveAndSlide(_hookVelocity);
            _cooldownTimer -= delta;

            _atEndOfRope = C._hook.GlobalPosition.DistanceSquaredTo(C._barrel.GlobalPosition) >=
                           C._maxRopeLength * C._maxRopeLength;
        }

        public override void OnExit()
        {
            C._barrel.RemoveChild(_rope);
            _rope.QueueFree();
        }
    }

    private class Retracting : Crate<GrappleGun>
    {
        private bool _hooked;
        private Rope _rope;

        public override Type GetTransition()
        {
            var hookTravelPerFrame = C.GetPhysicsProcessDeltaTime() * C._hookRetractSpeed;
            var withinSnapDistance =
                C._hook.GlobalPosition.DistanceSquaredTo(C._barrel.GlobalPosition) <
                hookTravelPerFrame * hookTravelPerFrame;

            return !_hooked && withinSnapDistance
                ? typeof(Idle)
                : base.GetTransition();
        }

        public override void OnEnter()
        {
            _hooked = C._shooting && C._hook.TouchingHookable;

            _rope = C._ropeScene.Instance<Rope>();
            C._barrel.AddChild(_rope);
            _rope.SetConnections(C._barrel, C._hook);
            _rope.Taut = true;
        }

        public override void OnProcessFrame(float delta)
        {
            ManageState();
        }

        public override void OnProcessPhysics(float delta)
        {
            ManageState();

            var hook = C._hook;
            var barrel = C._barrel;
            var velocity = C._playerPhysicsReport.Velocity;

            var stopDistanceSquared = C._hookPullStopDistance * C._hookPullStopDistance;
            var atHook = hook.GlobalPosition.DistanceSquaredTo(barrel.GlobalPosition) <= stopDistanceSquared / delta;

            switch (_hooked)
            {
                case true when atHook:
                    C.PullAcceleration = Vector2.Zero;
                    C.Braking = true;
                    break;

                case true when !atHook:
                    var heading = hook.GlobalPosition - barrel.GlobalPosition;
                    var velPerpendicularToHeading = velocity - velocity.Project(heading);
                    C.PullAcceleration = C._hookPullAccel * delta * (heading - velPerpendicularToHeading).Normalized();
                    C.Braking = false;
                    break;

                case false:
                    var hookToGun = hook.GlobalPosition.DirectionTo(barrel.GlobalPosition);
                    hook.MoveAndSlide(hookToGun * C._hookRetractSpeed);
                    C.PullAcceleration = Vector2.Zero;
                    C.Braking = false;
                    break;
            }
        }

        public override void OnExit()
        {
            C._barrel.RemoveChild(_rope);
            _rope.QueueFree();

            var hook = C._hook;
            hook.GetParent().RemoveChild(hook);
            C._barrel.AddChild(hook);
            hook.Position = Vector2.Zero;
            hook.Rotation = 0;

            C.PullAcceleration = Vector2.Zero;
            C.Braking = false;
        }

        private void ManageState()
        {
            if (_hooked && !C._shooting) _hooked = false;
        }
    }

    private class LooseRope : Crate<GrappleGun>
    {
        // TODO: implement this class
        public override Type GetTransition()
        {
            return typeof(Shooting);
        }
    }
}