using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobotAtVirtualHome {

    [RequireComponent(typeof(ObjectManager))]
    [RequireComponent(typeof(OntologyManager))]

    public class GeneralManager : MonoBehaviour {

        public int verbose;
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
        public void OnVirtualEnviromentLoaded() {

        }
        #endregion

        #region Private Functions
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

