using ROSUnityCore;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ViMantic;

namespace RobotAtVirtualHome {

    public class Initializer : MonoBehaviour {

        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;

        [Tooltip("Uses a virtual user object to visit the scene")]
        public GameObject userPrefab;

        private EnvironmentManager environmentManager;

        #region Unity Functions
        private void Awake()
        {
            environmentManager = FindObjectOfType<EnvironmentManager>();
            environmentManager.OnEnvironmentLoaded += VirtualEnviromentLoaded;
        }
        void Start() {
            var ontologyManager = GetComponent<OntologySystem>();
            if(ontologyManager != null)
                ontologyManager.LoadOntology();

            
        }

        private void OnDestroy()
        {
            environmentManager.OnEnvironmentLoaded -= VirtualEnviromentLoaded;
        }
        #endregion

        #region Public Functions
        public void VirtualEnviromentLoaded() {
            if (userPrefab != null)
            {
                userPrefab.SetActive(true);
                userPrefab.transform.position = new Vector3(0, 4, 0);
                userPrefab.transform.rotation = Quaternion.Euler(0, 0, -45);
            }
        }
        #endregion

        #region Private Functions

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

