using ROSUnityCore;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ViMantic;

namespace RobotAtVirtualHome {

    public class Initializer : MonoBehaviour {

        [System.Serializable]
        public struct Agent {
            public string name;
            public string ip;
            public GameObject prefab;
            public Agent(string name, string ip, string root)
            {
                this.name = name;
                this.ip = ip;
                this.prefab = (GameObject)AssetDatabase.LoadAssetAtPath(root, typeof(GameObject)); ;
            }
        }

        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;
        [Tooltip("Insert the prefab of the robots you want to load at the start of the simulation.")]
        public List<Agent> agentToInstantiate;
        public List<Transform> agents { set; private get; }


        #region Unity Functions
        private void Awake()
        {
            agents = new List<Transform>();
        }
        void Start() {
            var ontologyManager = GetComponent<OntologySystem>();
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
                foreach (Agent r in agentToInstantiate)
                {
                    Transform agent = Instantiate(r.prefab, origin, Quaternion.identity, house.transform.parent).transform;
                    agent.GetComponent<ROS>().robotName = r.name;
                    agent.GetComponent<ROS>().Connect(r.ip);
                    agents.Add(agent);
                }
            } else { Log("This house don't have robot station",LogLevel.Error,true); }
        }

        private void Log(string _msg, LogLevel lvl, bool Warning = false)
        {
            if (LogLevel <= lvl && LogLevel != LogLevel.Nothing)
            {
                if (Warning)
                {
                    Debug.LogWarning("[General Manager]: " + _msg);
                }
                else
                {
                    Debug.Log("[General Manager]: " + _msg);
                }
            }

        }
        #endregion

    }
}

