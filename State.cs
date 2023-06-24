using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarmer.Utility.StateMachine
{
    public abstract class State : NetworkBehaviour
    {
        public bool isDefault;
        public string stateName;
        public StateCondition condition;
        public bool isChainedToCondition;
        public StateCondition[] effects;


        private Action<State, StateCondition> _onNotCurrentStateConditionTrigger;
        public bool IsActive => _isActive;
        private bool _isActive;
        public bool IsCurrent => _isCurrent;
        private bool _isCurrent;

        private void Awake()
        {
            condition.ListenCondition(OnConditionChange);
        }

        public void ActivateDefault()
        {
            if (!isDefault)
            {
                throw new Exception("Trying to activate not-default state!");
            }
        }

        public virtual void EnsureInit(Action<State, StateCondition> onNotCurrentStateConditionTrigger)
        {
            _onNotCurrentStateConditionTrigger = onNotCurrentStateConditionTrigger;
        }

        private void OnConditionChange(StateCondition condition, bool newValue)
        {
            if (!_isActive)
            {
                return;
            }

            if (!_isCurrent && !newValue)
            {
                _onNotCurrentStateConditionTrigger.Invoke(this, condition);
            }
            else if (_isCurrent)
            {

            }
        }

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
                Debug.Log("Loop detected! " + gameObject.name);
            }

            _isActive = true;
            _isCurrent = true;
        }

        internal virtual void BackwardEnter()
        {
            _isCurrent = true;
        }

        internal virtual void ForwardExit()
        {
            _isCurrent = false;
        }

        internal virtual void BackwardExit()
        {
            _isActive = false;
            _isCurrent = false;
        }
    }
}