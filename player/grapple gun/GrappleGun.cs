using System;
using CaptainHookshot.player.grapple_gun.rope;
using CaptainHookshot.tools;
using Godot;

namespace CaptainHookshot.player.grapple_gun;

public class GrappleGun : Node2D
{
    private static readonly Vector2 LeftAimScale = new(1, -1), RightAimScale = new(1, 1);

    [Export(PropertyHint.Enum, "left,right")]
    private string _controlDirection;

    [Export] private NodePath _hookFlightContainerPath;

    [Export] private float _hookExitSpeed, _maxRopeLength, _hookRetractSpeed, _hookRetractCooldown, _hookPullAccel;
    [Export] private int _ropeSegmentCount, _aimSnapRegions;

    [Export] private NodePath _hookPath, _barrelPath;

    [Export] private PackedScene _ropeScene, _tautLineScene;

    private Vector2 _aim;
    private Node2D _barrel, _hookFlightContainer;
    private CrateMachine<GrappleGun> _crateMachine;
    private bool _grabbed, _grabbing, _shot;
    private Hook _hook;
    private PhysicsReport _playerPhysicsReport;

    public Vector2 PullAcceleration { get; private set; }

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
            .AddCrate(new Retracting());
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
        _grabbed = Input.IsActionJustPressed($"grab_{_controlDirection}");
        _grabbing = Input.IsActionPressed($"grab_{_controlDirection}");
    }

    private class Idle : Crate<GrappleGun>
    {
        public override Type GetTransition()
        {
            return C._grabbing ? typeof(Shooting) : base.GetTransition();
        }

        public override void OnProcessFrame(float delta)
        {
            C.LookAt(C.GlobalPosition + C._aim);

            // flip the sprite about the sprite's origin by inverting its scale
            // TODO: make the flipping symmetrical about the y axis, rather than copied across it
            C.Scale = C._aim.x < 0
                ? LeftAimScale
                : RightAimScale;
        }
    }

    private class Shooting : Crate<GrappleGun>
    {
        private float _cooldownTimer;
        private Vector2 _hookVelocity;
        private Rope _rope;

        public override Type GetTransition()
        {
            // TODO: should this be based on ignoring any initial area touching the hook, instead of time?
            var coolingDown = _cooldownTimer > 0;
            var manualRetract = !coolingDown && C._grabbed;
            var autoRetract =
                C._hook.GlobalPosition.DistanceSquaredTo(C._barrel.GlobalPosition) >=
                C._maxRopeLength * C._maxRopeLength;
            var hooked = !coolingDown && C._grabbing && C._hook.TouchingHookable;

            return manualRetract || autoRetract || hooked
                ? typeof(Retracting)
                : base.GetTransition();
        }

        public override void OnEnter()
        {
            // BUG: rope's connection to hook is wonky if you buffer a shoot input in a different direction after retracting 
            _rope = C._ropeScene.Instance<Rope>();
            _rope.Init(C._ropeSegmentCount, C._aim);
            C._barrel.AddChild(_rope);

            var hook = C._hook;

            hook.GetParent().RemoveChild(hook);
            hook.Scale = Vector2.One;
            C._hookFlightContainer.AddChild(hook);
            _rope.AttachEndTo(hook);

            _hookVelocity = C._aim * C._hookExitSpeed;
            hook.LookAt(hook.GlobalPosition + _hookVelocity);

            _cooldownTimer = C._hookRetractCooldown;
        }

        public override void OnProcessPhysics(float delta)
        {
            C._hook.MoveAndSlide(_hookVelocity);
            _cooldownTimer -= delta;
        }

        public override void OnExit()
        {
            C._barrel.RemoveChild(_rope);
            _rope.QueueFree();
        }
    }

    private class Retracting : Crate<GrappleGun>
    {
        private bool _hooked, _withinSnapDistance;
        private Line2D _line;

        public override Type GetTransition()
        {
            return !_hooked && _withinSnapDistance
                ? typeof(Idle)
                : base.GetTransition();
        }

        public override void OnEnter()
        {
            _hooked = C._grabbing && C._hook.TouchingHookable;

            _line = C._tautLineScene.Instance<Line2D>();
            C._barrel.AddChild(_line);
            _line.AddPoint(Vector2.Zero);
            _line.AddPoint(HookPosRelativeToBarrel());
        }

        public override void OnProcessFrame(float delta)
        {
            ManageStateFlags();
            _line.SetPointPosition(1, HookPosRelativeToBarrel());
        }

        public override void OnProcessPhysics(float delta)
        {
            ManageStateFlags();

            var hook = C._hook;
            var barrel = C._barrel;
            if (_hooked)
            {
                var velocity = C._playerPhysicsReport.Velocity;
                var heading = hook.GlobalPosition - barrel.GlobalPosition;
                var velPerpendicularToHeading = velocity - velocity.Project(heading);
                C.PullAcceleration = C._hookPullAccel * delta * (heading - velPerpendicularToHeading).Normalized();
            }
            else
            {
                var hookToGun = hook.GlobalPosition.DirectionTo(barrel.GlobalPosition);
                hook.MoveAndSlide(hookToGun * C._hookRetractSpeed);
                C.PullAcceleration = Vector2.Zero;
            }
        }

        public override void OnExit()
        {
            C._barrel.RemoveChild(_line);
            _line.QueueFree();

            var hook = C._hook;
            hook.GetParent().RemoveChild(hook);
            C._barrel.AddChild(hook);
            hook.Position = Vector2.Zero;
            hook.Rotation = 0;

            C.PullAcceleration = Vector2.Zero;
        }

        private void ManageStateFlags()
        {
            if (_hooked && !C._grabbing) _hooked = false;

            var hookTravelPerFrame = C.GetPhysicsProcessDeltaTime() * C._hookRetractSpeed;
            _withinSnapDistance =
                C._hook.GlobalPosition.DistanceSquaredTo(C._barrel.GlobalPosition) <
                hookTravelPerFrame * hookTravelPerFrame * 4;
        }

        private Vector2 HookPosRelativeToBarrel()
        {
            return C._barrel.ToLocal(C._hook.GlobalPosition);
        }
    }
}