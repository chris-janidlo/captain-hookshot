using System;
using System.Collections.Generic;

namespace CaptainHookshot.tools;

public struct StateMachine<TParent>
{
    private readonly Dictionary<Type, State> _states = new();
    private State _currentState;

    public StateMachine(TParent parent, Type initialState, params Type[] stateTypes)
    {
        foreach (var t in stateTypes)
        {
            AssertIsState(t);

            var state = (State)Activator.CreateInstance(t);
            Debug.Assert(state != null, $"failure initializing {nameof(state)} - returned null");
            state!.SetParent(parent);
            _states[t] = state;
        }

        AssertIsState(initialState);
        _currentState = _states[initialState];
        _currentState.OnEnter();
    }

    public void Process(float delta, ProcessType processType)
    {
        switch (processType)
        {
            case ProcessType.Idle:
                _currentState.OnProcess(delta);
                break;

            case ProcessType.Physics:
                _currentState.OnPhysicsProcess(delta);
                break;

            default:
                throw new InvalidOperationException($"unexpected {nameof(ProcessType)} {processType}");
        }

        if (_currentState.GetTransition() is { } t)
        {
            AssertIsState(t);

            _currentState.OnExit();
            _currentState = _states[t];
            _currentState.OnEnter();
        }
    }

    private static void AssertIsState(Type t)
    {
        Debug.Assert(t.IsSubclassOf(typeof(State)), $"{t} is not a {nameof(State)}");
    }

    public abstract class State
    {
        // ReSharper disable once InconsistentNaming
        protected TParent s;

        public void SetParent(TParent parent)
        {
            s = parent;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual Type GetTransition()
        {
            return null;
        }

        public virtual void OnProcess(float delta)
        {
        }

        public virtual void OnPhysicsProcess(float delta)
        {
        }
    }
}

public enum ProcessType
{
    Idle,
    Physics
}