using System.Collections.Generic;
using CaptainHookshot.player.grapple_gun.rope;
using Godot;

namespace CaptainHookshot.player.grapple_gun;

public class GrappleGun : Node2D
{
    private static readonly Vector2 LeftAimScale = new(1, -1), RightAimScale = new(1, 1);

    [Export(PropertyHint.Enum, "left,right")]
    private string _controlDirection;

    [Export] private NodePath _hookFlightContainerPath;

    [Export] private float _hookExitSpeed, _maxRopeLength, _hookRetractSpeed;
    [Export] private int _ropeSegmentCount;

    [Export] private NodePath _hookPath, _barrelPath;

    [Export] private PackedScene _ropeScene, _tautLineScene;

    private Vector2 _aim;
    private Node2D _barrel, _hookFlightContainer;

    private State _currentState;
    private bool _grabbed;
    private Hook _hook;
    private bool _shot;
    private Dictionary<State, State> _stateTransitions;

    public override void _Ready()
    {
        _hook = GetNode<Hook>(_hookPath);
        _barrel = GetNode<Node2D>(_barrelPath);
        _hookFlightContainer = GetNode<Node2D>(_hookFlightContainerPath);

        var idle = new Idle(this);
        var shooting = new Shooting(this);
        var retracting = new Retracting(this);

        _stateTransitions = new Dictionary<State, State>
        {
            { idle, shooting },
            { shooting, retracting },
            { retracting, idle }
        };

        _currentState = idle;
    }

    public override void _Process(float delta)
    {
        ManageInput();

        _currentState.OnProcess();
        ManageState();
    }

    public override void _PhysicsProcess(float delta)
    {
        _currentState.OnPhysicsProcess();
    }

    private void ManageInput()
    {
        var dir = Input.GetVector
        (
            $"aim_{_controlDirection}_left",
            $"aim_{_controlDirection}_right",
            $"aim_{_controlDirection}_up",
            $"aim_{_controlDirection}_down"
        ).Normalized();

        if (dir != Vector2.Zero) _aim = dir;

        _shot = Input.IsActionPressed($"shoot_{_controlDirection}");
        _grabbed = Input.IsActionPressed($"grab_{_controlDirection}");
    }

    private void ManageState()
    {
        if (_currentState.ExitCondition())
        {
            _currentState.OnExit();
            _currentState = _stateTransitions[_currentState];
            _currentState.OnEntered();
        }
    }

    private abstract class State
    {
        protected readonly GrappleGun Gun;

        protected State(GrappleGun gun)
        {
            Gun = gun;
        }

        public abstract bool ExitCondition();

        public abstract void OnEntered();
        public abstract void OnProcess();
        public abstract void OnPhysicsProcess();
        public abstract void OnExit();
    }

    private class Idle : State
    {
        public Idle(GrappleGun gun) : base(gun)
        {
        }

        public override bool ExitCondition()
        {
            return Gun._aim != Vector2.Zero && Gun._shot && !Gun._grabbed;
        }

        public override void OnEntered()
        {
        }

        public override void OnProcess()
        {
            Gun.LookAt(Gun.GlobalPosition + Gun._aim);

            // flip the sprite about the sprite's origin by inverting its scale
            Gun.Scale = Gun._aim.x < 0
                ? LeftAimScale
                : RightAimScale;
        }

        public override void OnPhysicsProcess()
        {
        }

        public override void OnExit()
        {
        }
    }

    private class Shooting : State
    {
        private Vector2 _hookVelocity;
        private Rope _rope;

        public Shooting(GrappleGun gun) : base(gun)
        {
        }

        public override bool ExitCondition()
        {
            return Gun._grabbed ||
                   Gun._hook.GlobalPosition.DistanceSquaredTo(Gun._barrel.GlobalPosition) >=
                   Gun._maxRopeLength * Gun._maxRopeLength;
        }

        public override void OnEntered()
        {
            // BUG: rope's connection to hook is wonky if you buffer a shoot input in a different direction after retracting 
            _rope = Gun._ropeScene.Instance<Rope>();
            _rope.Init(Gun._ropeSegmentCount, Gun._aim);
            Gun._barrel.AddChild(_rope);

            var hook = Gun._hook;

            hook.GetParent().RemoveChild(hook);
            hook.Scale = Vector2.One;
            Gun._hookFlightContainer.AddChild(hook);
            _rope.AttachEndTo(hook);

            _hookVelocity = Gun._aim * Gun._hookExitSpeed;
            hook.LookAt(hook.GlobalPosition + _hookVelocity);
        }

        public override void OnProcess()
        {
        }

        public override void OnPhysicsProcess()
        {
            Gun._hook.MoveAndSlide(_hookVelocity);
        }

        public override void OnExit()
        {
            Gun._barrel.RemoveChild(_rope);
            _rope.QueueFree();
        }
    }

    private class Retracting : State
    {
        private bool _hooked;
        private Line2D _line;

        public Retracting(GrappleGun gun) : base(gun)
        {
        }

        public override bool ExitCondition()
        {
            Vector2
                hookPos = Gun._hook.GlobalPosition,
                targetPos = Gun._barrel.GlobalPosition;
            return hookPos.DistanceSquaredTo(targetPos) < 5;
        }

        public override void OnEntered()
        {
            _hooked = Gun._grabbed && Gun._hook.TouchingHookable();

            _line = Gun._tautLineScene.Instance<Line2D>();
            Gun._barrel.AddChild(_line);
            _line.AddPoint(Vector2.Zero);
            _line.AddPoint(HookPosRelativeToBarrel());
        }

        public override void OnProcess()
        {
            _line.SetPointPosition(1, HookPosRelativeToBarrel());

            if (_hooked && !Gun._grabbed) _hooked = false;
        }

        public override void OnPhysicsProcess()
        {
            var hook = Gun._hook;
            if (_hooked)
            {
                // TODO
            }
            else
            {
                var dir = hook.GlobalPosition.DirectionTo(Gun._barrel.GlobalPosition);
                hook.MoveAndSlide(dir * Gun._hookRetractSpeed);
            }
        }

        public override void OnExit()
        {
            Gun._barrel.RemoveChild(_line);
            _line.QueueFree();

            var hook = Gun._hook;
            hook.GetParent().RemoveChild(hook);
            Gun._barrel.AddChild(hook);
            hook.Position = Vector2.Zero;
            hook.Rotation = 0;
        }

        private Vector2 HookPosRelativeToBarrel()
        {
            return Gun._barrel.ToLocal(Gun._hook.GlobalPosition);
        }
    }
}