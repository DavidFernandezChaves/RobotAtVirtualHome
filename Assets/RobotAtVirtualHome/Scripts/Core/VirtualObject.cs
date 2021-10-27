using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobotAtVirtualHome {
    public class VirtualObject : MonoBehaviour {
        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;        

        [Tooltip("Specify the labels that are related to the represented object.")]
        public List<ObjectTag> tags;
        
        [Tooltip("Insert the object from which you want to inherit the seed of the model to be loaded.")]
        public VirtualObject inheritedSeed;

        [Header("Preloading models")]
        public GameObject[] models;

        public string m_id { get; private set; }
        public int m_seed { get; private set; }
        public Room room { get; private set; }

        public bool m_initialized;

        public event Action<int> OnObjectModelChanged;

        #region Unity Functions
        private void Awake() {
            var house = FindObjectOfType<House>();    

            if(models == null || models.Length == 0) {
                Log("Unassigned model", LogLevel.Normal);
            }

            m_id = FindObjectOfType<House>().RegisterVirtualObject(this);
            transform.name = m_id;
        }

        void Start() {
            if (tags == null || tags.Count == 0) {
                Log("Unassigned tag", LogLevel.Error, true);
            }

            if (inheritedSeed!=null)
            {
                if (inheritedSeed.m_initialized)
                {
                    SetSeed(inheritedSeed.m_seed);
                }
                inheritedSeed.OnObjectModelChanged += SetSeed;                              
            }
            else
            {
                m_seed = FindObjectOfType<EnvironmentManager>().m_simulationOptions.specifySeedByObjectType.Find(pair => tags.Contains(pair.objectTag)).seed;
                if (m_seed <= 0)
                {
                    m_seed = UnityEngine.Random.Range(1, models.Length + 1);
                }

                SetSeed(m_seed-1);
            }
            
            var renders = GetComponentsInChildren<Renderer>(true);
            foreach(Renderer r in renders) {
                r.material.SetColor("_UnlitColor", FindObjectOfType<House>().semanticColors[name]);
            }
            m_initialized = true;

            room = gameObject.GetComponentInParent<Room>();
        }

        private void OnDestroy()
        {
            if (inheritedSeed)
            {
                inheritedSeed.OnObjectModelChanged -= SetSeed;
            }
        }
        #endregion

        #region Public Functions
        public Transform GetModel() {
            return models[m_seed].transform;
        }

        public void SetSeed(int seed)
        {
            if (models.Length == 0)
                return;

            m_seed = Mathf.Clamp(seed,0, models.Length-1);
            
            foreach (GameObject go in models)
            {
                go.SetActive(false);
            }

            Log("Style selected: " + m_seed.ToString(), LogLevel.Developer);
            models[m_seed].SetActive(true);            
            OnObjectModelChanged?.Invoke(m_seed);
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