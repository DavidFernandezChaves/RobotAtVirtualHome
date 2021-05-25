using ROSUnityCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobotAtVirtualHome {

    public class GeneralManager : MonoBehaviour {

        [System.Serializable]
        public struct Agent {
            public string name;
            public string ip;
            public GameObject prefab;
        }

        public int verbose;
        public List<Agent> agentToInstantiate;
        public List<Transform> agents;

        #region Unity Functions
        void Start() {
            var ontologyManager = GetComponent<OntologyManager>();
            if(ontologyManager != null)
                ontologyManager.LoadOntology();
        }        
        #endregion

        #region Public Functions
        public void VirtualEnviromentLoaded(GameObject house) {
            CreateVirtualAgent(house.GetComponent<House>());
        }
        #endregion

        #region Private Functions
        private void CreateVirtualAgent(House house) {            
            if (house.virtualObjects.ContainsKey("Station_0")) {
                var origin = house.virtualObjects["Station_0"].transform.position;
                foreach (Agent r in agentToInstantiate) {
                    Transform agent = Instantiate(r.prefab, origin , Quaternion.identity, house.transform.parent).transform;
                    agent.GetComponent<ROS>().robotName = r.name;
                    agent.GetComponent<ROS>().Connect(r.ip);
                    agents.Add(agent);
                }
            } else { LogWarning("This house don't have robot station"); }
        }

        private void Log(string _msg) {
            if (verbose > 1)
                Debug.Log("[General Manager]: " + _msg);
        }

        private void LogWarning(string _msg) {
            if (verbose > 0)
                Debug.LogWarning("[General Manager]: " + _msg);
        }
        #endregion

    }
}

