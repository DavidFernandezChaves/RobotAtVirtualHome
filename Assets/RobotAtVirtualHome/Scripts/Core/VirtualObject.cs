using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobotAtVirtualHome {
    public class VirtualObject : MonoBehaviour {
        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;        

        [Tooltip("Specify the labels that are related to the represented object.")]
        public ObjectTag[] tags;
        
        [Tooltip("Insert the object from which you want to inherit the seed of the model to be loaded.")]
        public VirtualObject inheritedSeed;
        public bool radomModel;

        [Header("Preloading models")]
        public GameObject[] models;

        public int seed { get; private set; }
        public Room room { get; private set; }

        #region Unity Functions
        private void Awake() {
            var house = FindObjectOfType<House>();
            
            seed = Random.Range(0, models.Length);

            foreach (GameObject go in models) {
                go.SetActive(false);
            }

            if(models == null || models.Length == 0) {
                Log("Unassigned model", LogLevel.Normal);
            }
        }

        void Start() {
            if (tags == null || tags.Length == 0) {
                Log("Unassigned tag", LogLevel.Error, true);
            }

            Transform t = FindObjectOfType<EnvironmentManager>().FindObjectUPWithClass(typeof(Room), transform);
            if (t != FindObjectOfType<EnvironmentManager>()) {
                radomModel = t.GetComponent<Room>().randomObjectModel;                
            } else {
                Log("Room not found", LogLevel.Error, true);
            }

            if(inheritedSeed != null){
                seed = inheritedSeed.seed;
            }

            if(seed < models.Length) {
                Log("Selected style: " + seed.ToString(), LogLevel.Developer);
                models[seed].SetActive(true);
            } else {
                if (models != null && models.Length > 0) {
                    Log("The model closest to: " + seed.ToString() + "was selected", LogLevel.Developer);
                    models[models.Length-1].SetActive(true);
                }
            }

            transform.name = FindObjectOfType<House>().RegistVirtualObject(this);
            var renders = GetComponentsInChildren<Renderer>();
            foreach(Renderer r in renders) {
                r.material.SetColor("_UnlitColor", FindObjectOfType<House>().semanticColors[name]);
            }
            
        }
        #endregion

        #region Public Functions
        public Transform GetModel() {
            return models[seed].transform;
        }
        #endregion

        #region Private Functions
        private void Log(string _msg, LogLevel lvl, bool Warning = false)
        {
            if (LogLevel <= lvl && LogLevel != LogLevel.Nothing)
            {
                if (Warning)
                {
                    Debug.LogWarning("[VirtualObject-" + transform.name + "]: " + _msg);
                }
                else
                {
                    Debug.Log("[VirtualObject-" + transform.name + "]: " + _msg);
                }
            }

        }
        #endregion
    }
}