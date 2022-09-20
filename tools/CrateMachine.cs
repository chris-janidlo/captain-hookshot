using System;
using System.Collections.Generic;

namespace CaptainHookshot.tools;

public struct CrateMachine<TParent>
{
    private readonly Dictionary<Type, Crate<TParent>> _crates = new();
    private readonly TParent _parent;
    private Crate<TParent> _currentCrate;
    private Type _initialCrateType;

    public CrateMachine(TParent parent)
    {
        _parent = parent;
        _currentCrate = null;
        _initialCrateType = null;
    }

    public CrateMachine<TParent> AddCrate<TCrate>(TCrate crate) where TCrate : Crate<TParent>
    {
        _crates[typeof(TCrate)] = crate;
        crate.SetParent(_parent);
        return this;
    }

    public CrateMachine<TParent> SetInitialCrate<TCrate>() where TCrate : Crate<TParent>
    {
        _initialCrateType = typeof(TCrate);
        return this;
    }

    public void Process(float delta, ProcessType processType)
    {
        _currentCrate ??= _crates[_initialCrateType];

        switch (processType)
        {
            case ProcessType.Frame:
                _currentCrate.OnProcessFrame(delta);
                break;

            case ProcessType.Physics:
                _currentCrate.OnProcessPhysics(delta);
                break;

            default:
                throw new InvalidOperationException("unexpected " + nameof(ProcessType) + " " + processType);
        }

        DoTransition(_currentCrate.GetTransition());
    }

    private void DoTransition(Type newCrateType)
    {
        if (newCrateType is null) return;

        _currentCrate.OnExit();
        _currentCrate = _crates[newCrateType];
        _currentCrate.OnEnter();
    }
}

public abstract class Crate<TParent>
{
    // ReSharper disable once InconsistentNaming
    protected TParent C;

    public void SetParent(TParent parent)
    {
        C = parent;
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

    public virtual void OnProcessFrame(float delta)
    {
    }

    public virtual void OnProcessPhysics(float delta)
    {
    }
}

public enum ProcessType
{
    Frame,
    Physics
}