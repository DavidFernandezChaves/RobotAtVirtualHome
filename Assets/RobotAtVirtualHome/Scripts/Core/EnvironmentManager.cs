using ROSUnityCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;


namespace RobotAtVirtualHome {
    public class EnvironmentManager : MonoBehaviour {

        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;

        [Tooltip("Do you want to save a file with information about the loaded environment?")]
        public bool recordEnvironmentDatas;
        [Tooltip("Do you want the roof to be transparent for easy viewing?")]
        public bool transparentRoof;

        [SerializeField]
        [Header("Customization")]
        public SimulationOptions m_simulationOptions;

        [Header("Preload prefabs")]
        [SerializeField]
        private List<GameObject> houses;

        public event Action OnEnvironmentLoaded;

        private House house;        
        private StreamWriter writer;

        #region Unity Functions
        private void Awake() {
            if (houses != null && houses.Count > 0) {

                if (m_simulationOptions.houseSelected == 0) {
                    m_simulationOptions.houseSelected = UnityEngine.Random.Range(1, houses.Count);
                }              

                if (house = Instantiate(houses[m_simulationOptions.houseSelected - 1], transform).GetComponent<House>()) {
                    m_simulationOptions.path = Path.Combine(m_simulationOptions.path, "Home" + m_simulationOptions.houseSelected.ToString("D2"));
                    if (recordEnvironmentDatas) {                        
                        if (!Directory.Exists(m_simulationOptions.path)) {
                            Directory.CreateDirectory(m_simulationOptions.path);
                        }
                        Log("The saving path is:" + m_simulationOptions.path, LogLevel.Normal);
                    }

                } else {
                    Log("The gameObject " + (m_simulationOptions.houseSelected - 1) + " does not have the 'House' component.",LogLevel.Error,true);
                }

            } else { Log("There are no assigned houses in the virtual environment.",LogLevel.Error,true); }
        }

        private void Start()
        {
            house.SetTransparentRoof(transparentRoof);
            house.LoadHouse(m_simulationOptions);

            if (recordEnvironmentDatas)
            {
                writer = new StreamWriter(m_simulationOptions.path + "/VirtualObjects.csv", true);
                writer.WriteLine("id;color;room;roomType;type;globalPosition;rotation;seed");
                foreach (KeyValuePair<string, VirtualObject> obj in house.virtualObjects)
                {
                    writer.WriteLine(obj.Key.ToString() + ";"
                        + house.semanticColors[obj.Key].ToString() + ";"
                        + obj.Value.room.transform.name.ToString() + ";"
                        + obj.Value.room.roomType.ToString() + ";"
                        + obj.Value.tags[0].ToString() + ";"
                        + obj.Value.transform.position.ToString() + ";"
                        + obj.Value.transform.rotation.eulerAngles.ToString() + ";"
                        + obj.Value.m_seed.ToString());
                }
                writer.Close();
            }
            transform.GetComponent<NavMeshSurface>().BuildNavMesh();

            StartCoroutine(LoadingEnvironment());
            
            //GameObject.Find("General Scripts").SendMessage("VirtualEnviromentLoaded", house.gameObject, SendMessageOptions.DontRequireReceiver);


        }


        #endregion

        #region Public Functions
        public Transform FindObjectUPWithClass(Type component, Transform ini ) {
            do {
                ini = ini.parent;
            } while ( ini != transform && ini.GetComponent(component) == null && ini != transform.root);
            return ini;
        }

        #endregion

        #region Private Functions
        private IEnumerator LoadingEnvironment()
        {
            bool isLoading = true;
            while (isLoading)
            {
                isLoading = false;
                var objs = Resources.FindObjectsOfTypeAll(typeof(VirtualObject)) as VirtualObject[];
                foreach (VirtualObject virtualObject in objs)
                {                    
                    if (virtualObject.isActiveAndEnabled && virtualObject.m_initialized == false)
                    {
                        isLoading = true;
                    }
                }
                if (objs.Length == 0)
                {
                    isLoading = true;
                }
                yield return null;
            }
            
            OnEnvironmentLoaded?.Invoke();
        }

        private void Log(string _msg, LogLevel lvl, bool Warning = false)
        {
            if (LogLevel <= lvl && LogLevel != LogLevel.Nothing)
            {
                if (Warning)
                {
                    Debug.LogWarning("[Virtual Environment]: " + _msg);
                }
                else
                {
                    Debug.Log("[Virtual Environment]: " + _msg);
                }
            }

        }
        #endregion
    }
}