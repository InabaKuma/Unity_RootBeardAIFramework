using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RootBeard.Interface
{
    public interface IState
    {
        void Enter();
        void Update();
        void Exit();
    }
}