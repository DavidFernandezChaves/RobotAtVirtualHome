using ROSUnityCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobotAtVirtualHome {

    [RequireComponent(typeof(ObjectManager))]
    [RequireComponent(typeof(OntologyManager))]

    public class GeneralManager : MonoBehaviour {

        public int verbose;
        public string ip = "192.168.0.12";
        public GameObject virtualRobotPrefab;

        private Transform virtualRobot;
        private OntologyManager ontologyManager;


        #region Unity Functions
        private void Awake() {
            ontologyManager = GetComponent<OntologyManager>();

        }

        void Start() {
            ontologyManager.LoadOntology();
        }        
        #endregion

        #region Public Functions
        public void VirtualEnviromentLoaded(GameObject house) {
            CreateVirtualRobot(house.GetComponent<House>());
        }
        #endregion

        #region Private Functions
        private void CreateVirtualRobot(House house) {            
            if (house.virtualObjects.ContainsKey("Station_0")) {
                virtualRobot = Instantiate(virtualRobotPrefab, house.virtualObjects["Station_0"].transform.position, Quaternion.identity, house.transform.parent).transform;
                virtualRobot.GetComponent<ROS>().Connect(ip);
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

