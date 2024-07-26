using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SimpleU.StateMachine.NetworkChainedStateMachine
{
    //Conditions are unique for states
    public class StateCondition : NetworkBehaviour
    {
        public string Key => _key;
        [SerializeField] private string _key;
        public StateCondition chainedCondition;

        public bool IsChainedCondition => chainedCondition != null;
        public bool Value => chainedCondition != null ? chainedCondition.Value && SelfValue : SelfValue;
        public bool SelfValue => _networkVariable.Value;
        private NetworkVariable<bool> _networkVariable = new NetworkVariable<bool>();
        private Action<StateCondition, bool> _onValueChanged;

        private bool _initted;
        private bool _isDefault;

        public void ListenCondition(Action<StateCondition, bool> action)
        {
            _onValueChanged += action;
        }

        public void SetAsDefault(State state)
        {
            if (!state.isDefault)
            {
                throw new Exception("State Condition can't be set default by not default state!");
            }

            _isDefault = true;
            _networkVariable.Value = true;
        }

        public void ChangeValue(bool value)
        {
            if (value == _networkVariable.Value)
                return;

            _networkVariable.Value = value;
            TriggerOnValueChanged(!value, value);
        }

        private void TriggerOnValueChanged(bool previousValue, bool newValue)
        {
            if (_isDefault || previousValue == newValue)
                return;

            if (_onValueChanged != null)
            {
                _onValueChanged.Invoke(this, newValue);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _initted = true;
            //_networkVariable.OnValueChanged += TriggerOnValueChanged;
        }

        internal bool CheckData()
        {
            if (string.IsNullOrEmpty(Key))
            {
                Debug.LogError(gameObject.name + " condition key empty!");
                return false;
            }

            return true;
        }

        //WARNING! Recursive function
        public List<StateCondition> GetConditionChain(List<StateCondition> conditionChain)
        {
            conditionChain.Add(this);

            if (!IsChainedCondition)
            {
                return conditionChain;
            }

            return chainedCondition.GetConditionChain(conditionChain);
        }
    }
}