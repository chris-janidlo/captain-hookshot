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

    [Export] private float _hookExitSpeed, _maxRopeLength, _hookRetractSpeed;
    [Export] private int _ropeSegmentCount, _aimSnapRegions;

    [Export] private NodePath _hookPath, _barrelPath;

    [Export] private PackedScene _ropeScene, _tautLineScene;

    private Vector2 _aim;
    private Node2D _barrel, _hookFlightContainer;

    private bool _grabbed, _grabbing;
    private Hook _hook;
    private bool _shot;
    private StateMachine<GrappleGun> _stateMachine;

    public override void _Ready()
    {
        _hook = GetNode<Hook>(_hookPath);
        _barrel = GetNode<Node2D>(_barrelPath);
        _hookFlightContainer = GetNode<Node2D>(_hookFlightContainerPath);

        _aim = Vector2.Right;

        _stateMachine =
            new StateMachine<GrappleGun>(this, typeof(Idle),
                typeof(Idle), typeof(Shooting), typeof(Retracting));
    }

    public override void _Process(float delta)
    {
        ManageInput();
        _stateMachine.Process(delta, ProcessType.Idle);
    }

    public override void _PhysicsProcess(float delta)
    {
        _stateMachine.Process(delta, ProcessType.Physics);
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

    private class Idle : StateMachine<GrappleGun>.State
    {
        public override Type GetTransition()
        {
            return s._aim != Vector2.Zero && s._grabbed ? typeof(Shooting) : base.GetTransition();
        }

        public override void OnProcess(float delta)
        {
            s.LookAt(s.GlobalPosition + s._aim);

            // flip the sprite about the sprite's origin by inverting its scale
            // TODO: make the flipping symmetrical about the y axis, rather than copied across it
            s.Scale = s._aim.x < 0
                ? LeftAimScale
                : RightAimScale;
        }
    }

    private class Shooting : StateMachine<GrappleGun>.State
    {
        private Vector2 _hookVelocity;
        private Rope _rope;

        public override Type GetTransition()
        {
            return s._grabbed ||
                   s._hook.GlobalPosition.DistanceSquaredTo(s._barrel.GlobalPosition) >=
                   s._maxRopeLength * s._maxRopeLength
                ? typeof(Retracting)
                : base.GetTransition();
        }

        public override void OnEnter()
        {
            // BUG: rope's connection to hook is wonky if you buffer a shoot input in a different direction after retracting 
            _rope = s._ropeScene.Instance<Rope>();
            _rope.Init(s._ropeSegmentCount, s._aim);
            s._barrel.AddChild(_rope);

            var hook = s._hook;

            hook.GetParent().RemoveChild(hook);
            hook.Scale = Vector2.One;
            s._hookFlightContainer.AddChild(hook);
            _rope.AttachEndTo(hook);

            _hookVelocity = s._aim * s._hookExitSpeed;
            hook.LookAt(hook.GlobalPosition + _hookVelocity);
        }

        public override void OnPhysicsProcess(float delta)
        {
            s._hook.MoveAndSlide(_hookVelocity);
        }

        public override void OnExit()
        {
            s._barrel.RemoveChild(_rope);
            _rope.QueueFree();
        }
    }

    private class Retracting : StateMachine<GrappleGun>.State
    {
        private bool _hooked;
        private Line2D _line;

        public override Type GetTransition()
        {
            Vector2
                hookPos = s._hook.GlobalPosition,
                targetPos = s._barrel.GlobalPosition;
            var hookTravelPerFrame = s.GetPhysicsProcessDeltaTime() * s._hookRetractSpeed;
            return hookPos.DistanceSquaredTo(targetPos) < hookTravelPerFrame * hookTravelPerFrame * 4
                ? typeof(Idle)
                : base.GetTransition();
        }

        public override void OnEnter()
        {
            _hooked = s._grabbing && s._hook.TouchingHookable;

            _line = s._tautLineScene.Instance<Line2D>();
            s._barrel.AddChild(_line);
            _line.AddPoint(Vector2.Zero);
            _line.AddPoint(HookPosRelativeToBarrel());
        }

        public override void OnProcess(float delta)
        {
            _line.SetPointPosition(1, HookPosRelativeToBarrel());

            if (_hooked && !s._grabbing) _hooked = false;
        }

        public override void OnPhysicsProcess(float delta)
        {
            var hook = s._hook;
            if (_hooked)
            {
                // TODO
            }
            else
            {
                var dir = hook.GlobalPosition.DirectionTo(s._barrel.GlobalPosition);
                hook.MoveAndSlide(dir * s._hookRetractSpeed);
            }
        }

        public override void OnExit()
        {
            s._barrel.RemoveChild(_line);
            _line.QueueFree();

            var hook = s._hook;
            hook.GetParent().RemoveChild(hook);
            s._barrel.AddChild(hook);
            hook.Position = Vector2.Zero;
            hook.Rotation = 0;
        }

        private Vector2 HookPosRelativeToBarrel()
        {
            return s._barrel.ToLocal(s._hook.GlobalPosition);
        }
    }
}