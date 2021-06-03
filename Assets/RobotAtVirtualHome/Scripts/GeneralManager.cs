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
        public bool skiptSave = false;

        #region Unity Functions
        void Start() {
            var ontologyManager = GetComponent<OntologySystem>();
            if(ontologyManager != null)
                ontologyManager.LoadOntology();

            if (!skiptSave && agentToInstantiate.Count > 0) {
                Agent agent = new Agent();
                agent.name = PlayerPrefs.GetString("robotName", "VirtualAgent");
                agent.ip = PlayerPrefs.GetString("ip", agentToInstantiate[0].ip);
                agentToInstantiate[0] = agent;                  
            }

        }

        private void OnApplicationQuit() {
            if (agentToInstantiate.Count > 0) {
                PlayerPrefs.SetString("robotName", agentToInstantiate[0].name);
                PlayerPrefs.SetString("ip", agentToInstantiate[0].ip);
                PlayerPrefs.Save();
            }
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

