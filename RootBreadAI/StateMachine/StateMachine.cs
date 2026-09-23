using System;
using System.Collections.Generic;
using RootBeard.Interface;

namespace RootBeard.Framework
{
    public class StateMachine
    {
        public IState CurrentState { get; private set; }
        public string CurrentStateName { get; private set; }
        public event Action<IState, IState> OnStateChanged;

        private readonly Dictionary<string, IState> registry = new Dictionary<string, IState>();

        public void Register(string name, IState state)
        {
            if (string.IsNullOrEmpty(name) || state == null) return;
            registry[name] = state;
        }

        public IState GetState(string name)
        {
            return registry.TryGetValue(name, out var s) ? s : null;
        }

        public void ChangeState(string name)
        {
            var s = GetState(name);
            if (s != null) ChangeState(s);
            else UnityEngine.Debug.LogWarning($"[SM] 找不到状态 '{name}'");
        }

        public void ChangeState(IState newState)
        {
            if (newState == null) throw new ArgumentNullException(nameof(newState));
            if (ReferenceEquals(CurrentState, newState)) return;

            IState oldState = CurrentState;
            if (oldState != null) oldState.Exit();

            CurrentState = newState;
            CurrentStateName = FindNameByState(newState);
            CurrentState.Enter();

            OnStateChanged?.Invoke(oldState, newState);
        }

        private string FindNameByState(IState state)
        {
            foreach (var kvp in registry)
                if (ReferenceEquals(kvp.Value, state)) return kvp.Key;
            return null;
        }

        public void Update() => CurrentState?.Update();

        public void ExitCurrentState()
        {
            if (CurrentState != null)
            {
                IState oldState = CurrentState;
                CurrentState = null;
                CurrentStateName = null;
                oldState.Exit();
                OnStateChanged?.Invoke(oldState, null);
            }
        }
    }
}