using AlienFarmer.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor.VersionControl;
using UnityEngine;

namespace AlienFarmer.Utility.StateMachine
{
    public class StateMachine : NetworkBehaviour
    {
        private HashSet<State> _states;
        private Dictionary<string, string> _stateChangeRecords;
        private HashSet<StatePathFinder.Connection> _paths;
        private HashSet<StateCondition> _conditions;

        //Prepare fields before network spawn
        void Awake()
        {
            PrepareConditions();
            PrepareStates();
            PreparePath();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            UpdateManager.Manager.AddFinish(LateNetworkSpawn, 0);
        }

        //Start working after network spawn
        private void LateNetworkSpawn(bool success, object data)
        {
            if (!success)
                return;

            StartListenConditions();
            StartWithDefaultState();
        }

        private void PrepareStates()
        {
            bool success = true;
            _states = transform.parent.GetComponentsInChildren<State>().ToHashSet();

            var defaultState = _states.SingleOrDefault(x => x.isDefault);
            if (defaultState == null)
            {
                Debug.LogError("Default state not found or there are multiple states!");
                success = false;
            }

            foreach (var s in _states)
            {
                if (!s.CheckData())
                {
                    success = false;
                }
            }

            if (!success)
            {
                throw new Exception(gameObject.name + "state data broken!");
            }
        }

        [ContextMenu("PreparePath")]
        private void PreparePath()
        {
            var states = transform.parent.GetComponentsInChildren<State>();

            var pathFinder = new StatePathFinder();
            _paths = pathFinder.PrepareStatePaths(states.FirstOrDefault(x => x.isDefault), states);
            if (_paths == null)
            {
                throw new Exception(gameObject.name + " StateMachine path null!");
            }
        }

        private void PrepareConditions()
        {
            bool success = true;
            _conditions = transform.parent.GetComponentsInChildren<StateCondition>().ToHashSet();
            foreach (var c in _conditions)
            {
                if (!c.CheckData())
                {
                    success = false;
                }
            }

            if (!success)
            {
                throw new Exception(gameObject.name + "Condition data broken!");
            }
        }

        private void StartListenConditions()
        {
            foreach (var c in _conditions)
            {
                c.ListenCondition(OnConditionValueChange);
            }
        }

        private void StartWithDefaultState()
        {
            var defaultState = _states.FirstOrDefault(x => x.isDefault);
            SetForwardState(null, defaultState);
        }

        //WARNING! Recursive function
        private void SetForwardState(State sourceState, State state)
        {
            //loop detected
            if (state.IsActive)
            {
                Debug.Log("Loop Detected!");
                RemoveLoopedRecord(state.stateName);
            }

            //set state entered
            state.ForwardEnter();
            AddStateChangeRecord(sourceState, state);

            //Check is there a available next state to move
            var forwardConnections = _paths.Where(x => string.Equals(x.sourceName, state.stateName));
            foreach (var c in forwardConnections)
            {
                var forwardState = _states.FirstOrDefault(x => string.Equals(x.stateName, c.targetName));
                if (forwardState.condition.Value)
                {
                    state.ForwardExit();
                    SetForwardState(state, forwardState);
                    return;
                }
            }

            //if there no state available to move, start update
            state.StartUpdate();
        }

        //WARNING! Recursive function
        private void SetBackwardState(State state)
        {
            //try find last record
            var lastRecord = _stateChangeRecords.LastOrDefault();
            if (string.Equals(lastRecord.Value, state.stateName))
            {
                throw new Exception("SetBackwardState state is not last active state!");
            }

            state.BackwardExit();

            //get source state and remove from records
            var backwardTargetState = GetStateByName(lastRecord.Key);
            backwardTargetState.BackwardEnter();
            RemoveStateChangeRecord(backwardTargetState);
            
            if (backwardTargetState.condition.Value)
            {
                backwardTargetState.StartUpdate();
                return;
            }

            //if condition not met, go backward again
            SetBackwardState(backwardTargetState);
        }

        private void OnConditionValueChange(StateCondition condition, bool value)
        {

        }

        public void AddStateChangeRecord(State sourceState, State state)
        {
            string sourceStateName = sourceState != null ? sourceState.stateName : "";
            _stateChangeRecords.Add(sourceStateName, state.stateName);
        }

        public void RemoveStateChangeRecord(State sourceState)
        {
            _stateChangeRecords.Remove(sourceState.stateName);
        }

        private void RemoveLoopedRecord(string targetStateName)
        {
            string lastTargetName = "";
            while (!string.Equals(lastTargetName, targetStateName))
            {
                string sourceStateName = lastTargetName;
                if (_stateChangeRecords.ContainsKey(sourceStateName))
                {
                    lastTargetName = _stateChangeRecords.GetValueOrDefault(sourceStateName);
                    _stateChangeRecords.Remove(sourceStateName);
                }
            }
        }

        private State GetStateByName(string stateName)
        {
            var state = _states.FirstOrDefault(x => string.Equals(x.stateName, stateName));
            if (state == null)
            {
                throw new Exception("State not found!");
            }

            return state;
        }
    }
}
