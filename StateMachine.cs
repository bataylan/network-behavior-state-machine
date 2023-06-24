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
        private Dictionary<string, string> _stateChangedPaths;
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
            state.ForwardEnter();
            string sourceStateName = sourceState != null ? sourceState.stateName : "";
            AddAndCheckLoopOnForward(sourceStateName, state.stateName);

            var forwardConnections = _paths.Where(x => string.Equals(x.sourceName, x.targetName));
            foreach (var c in forwardConnections)
            {
                var forwardState = _states.FirstOrDefault(x => string.Equals(x.stateName, c.targetName));
                if (forwardState.condition.Value)
                {
                    state.ForwardExit();
                    SetForwardState(state, forwardState);
                    break;
                }
            }
        }

        private void OnConditionValueChange(StateCondition condition, bool value)
        {

        }

        public void AddAndCheckLoopOnForward(string sourceStateName, string targetStateName)
        {
            _stateChangedPaths.Add(sourceStateName, targetStateName);
            bool isLoopDetected = _stateChangedPaths.ContainsKey(targetStateName);
            if (isLoopDetected)
            {
                RemoveLoop(targetStateName);
            }
        }

        private void RemoveLoop(string targetStateName)
        {
            string lastTargetName = "";
            while (!string.Equals(lastTargetName, targetStateName))
            {
                string sourceStateName = lastTargetName;
                if (_stateChangedPaths.ContainsKey(sourceStateName))
                {
                    lastTargetName = _stateChangedPaths.GetValueOrDefault(sourceStateName);
                    _stateChangedPaths.Remove(sourceStateName);
                }
            }
        }
    }
}
