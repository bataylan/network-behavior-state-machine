using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarmer.Utility.StateMachine
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

        public void ListenCondition(Action<StateCondition, bool> action)
        {
            _onValueChanged += action;
        }

        private void TriggerOnValueChanged(bool previousValue, bool newValue)
        {
            if (previousValue == newValue)
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
            _networkVariable.OnValueChanged += TriggerOnValueChanged;
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