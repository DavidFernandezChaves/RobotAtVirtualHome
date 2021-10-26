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

        #region Unity Functions
        void Start() {
            var ontologyManager = GetComponent<OntologySystem>();
            if(ontologyManager != null)
                ontologyManager.LoadOntology();

            
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

