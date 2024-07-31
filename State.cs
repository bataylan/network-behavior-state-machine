using System;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace SimpleU.StateMachine.NetworkChainedStateMachine
{
    public abstract class State : NetworkBehaviour
    {
        public bool isDefault;
        public string stateName;
        public StateCondition condition;
        public StateCondition[] effects;


        private Action<State, StateCondition> _onNotCurrentStateConditionTrigger;
        public bool IsActive => _isActive;
        private bool _isActive;
        public bool IsCurrent => _isCurrent;
        private bool _isCurrent;

        internal bool CheckData()
        {
            if (!isDefault && condition == null)
            {
                Debug.LogError(stateName + " condition empty!");
                return false;
            }
            return true;
        }

        internal virtual void ForwardEnter()
        {
            if (_isActive)
            {
                Debug.LogError("Loop detected! " + gameObject.name);
            }

            _isActive = true;
            _isCurrent = true;
        }

        internal virtual void BackwardEnter()
        {
            _isCurrent = true;
        }

        internal virtual void StartUpdate() { }

        internal virtual void StopUpdate() { }

        internal virtual void ForwardExit()
        {
            _isCurrent = false;
            StopUpdate();
        }

        internal virtual void BackwardExit()
        {
            _isActive = false;
            _isCurrent = false;
            StopUpdate();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (_isCurrent)
                Handles.Label(transform.position + (2 * Vector3.up), stateName);
        }
#endif
    }
}