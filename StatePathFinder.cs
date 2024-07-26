using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleU.StateMachine.NetworkChainedStateMachine
{
    public class StatePathFinder
    {
        private HashSet<Connection> _path;

        public HashSet<Connection> PrepareStatePaths(State initialState, State[] allStates)
        {
            State[] organizedAllStates = allStates;
            allStates.CopyTo(organizedAllStates, 0);

            var allStatesAsList = organizedAllStates.ToList();

            if (allStates.Contains(initialState))
            {
                allStatesAsList.Remove(initialState);
                organizedAllStates = allStatesAsList.ToArray();
            }

            //check states
            for (int i = 0; i < organizedAllStates.Length; i++)
            {
                var state = organizedAllStates[i];
                if (string.IsNullOrEmpty(state.condition.Key))
                {
                    throw new Exception("StateCondition Key Empty");
                }
            }

            _path = new HashSet<Connection>();
            RegisterConnection(null, initialState, organizedAllStates);
            return _path;
        }

        //WARNING: Recursive function
        private void RegisterConnection(State prevState, State state, State[] allStates)
        {
            //prevent loop
            if (prevState != null && _path.Any(x => string.Equals(x.sourceName, state.stateName)))
            {
                return;
            }

            //WARNING: Loop inside a loop
            foreach (var effect in state.effects)
            {
                var targetStates = allStates.Where(x => string.Equals(x.condition.Key, effect.Key));
                if (targetStates == null || targetStates.Count() <= 0)
                {
                    _path = null;
                    return;
                }

                foreach (var target in targetStates)
                {
                    Debug.Log("RegisterConnection: k: " + state.stateName + " v: " + target.stateName);
                    _path.Add(new Connection(state.stateName, target.stateName));
                    RegisterConnection(state, target, allStates);
                }
            }

            return;
        }

        public struct Connection
        {
            public string sourceName;
            public string targetName;

            public Connection(string sourceName, string targetName)
            {
                this.sourceName = sourceName;
                this.targetName = targetName;
            }
        }
    }
}
