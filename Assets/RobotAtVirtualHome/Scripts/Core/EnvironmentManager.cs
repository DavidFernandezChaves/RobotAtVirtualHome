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
        [Tooltip("House to be loaded. 0 for random.")]
        [Range(0,30)]
        public int houseSelected = 0;
        [Tooltip("Do you want to save a file with information about the loaded environment?")]
        public bool recordEnvironmentDatas;
        [Tooltip("Path where you want to save the collected data.")]
        public string path = @"D:\";
        [Tooltip("Do you want the roof to be transparent for easy viewing?")]
        public bool transparentRoof;       

        [Header("Customization")]
        public LightLevel initialStateGeneralLight;
        public LightLevel initialStateLights;
        public bool randomStateDoor;
        public bool randomWallPainting;
        public bool randomFloorPainting;
        public bool randomObjectModel;

        [Header("Preload prefabs")]
        [SerializeField]
        private List<GameObject> houses;

        private House house;
        
        private StreamWriter writer;

        #region Unity Functions
        private void Awake() {
            if (houses != null && houses.Count > 0) {

                if (houseSelected == 0) {
                    houseSelected = UnityEngine.Random.Range(1, houses.Count);
                }              

                if (house = Instantiate(houses[houseSelected-1], transform).GetComponent<House>()) {
                    path = Path.Combine(path, "Home" + houseSelected.ToString("D2"));
                    if (recordEnvironmentDatas) {                        
                        if (!Directory.Exists(path)) {
                            Directory.CreateDirectory(path);
                        }
                        Log("The saving path is:" + path,LogLevel.Normal);
                    }

                    house.SetTransparentRoof(transparentRoof);
                    foreach(Room room in house.transform.GetComponentsInChildren<Room>()) {
                        room.randomStateDoor = randomStateDoor;
                        room.initialStateGeneralLight = initialStateGeneralLight;
                        room.initialStateLights = initialStateLights;
                        room.randomWallPainting = randomWallPainting;
                        room.randomFloorPainting = randomFloorPainting;
                        room.randomObjectModel = randomObjectModel;
                    }
                    Invoke("StartSimulation", 0.5f);
                } else {
                    Log("The gameObject " + (houseSelected-1) + " does not have the 'House' component.",LogLevel.Error,true);
                }

            } else { Log("There are no assigned houses in the virtual environment.",LogLevel.Error,true); }
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
        private void StartSimulation() {
            if (recordEnvironmentDatas) {
                writer = new StreamWriter(path + "/VirtualObjects.csv", true);
                writer.WriteLine("id;color;room;roomType;type;globalPosition;rotation;seed");
                foreach (KeyValuePair<string, VirtualObject> obj in house.virtualObjects) {
                    writer.WriteLine(obj.Key.ToString() + ";"
                        + house.semanticColors[obj.Key].ToString() + ";"
                        + obj.Value.room.transform.name.ToString() + ";"
                        + obj.Value.room.roomType.ToString() + ";"
                        + obj.Value.tags[0].ToString() + ";"
                        + obj.Value.transform.position.ToString() + ";"
                        + obj.Value.transform.rotation.eulerAngles.ToString() + ";"
                        + obj.Value.seed.ToString());
                }
                writer.Close();
            }
            transform.GetComponent<NavMeshSurface>().BuildNavMesh();

            GameObject.Find("General Scripts").SendMessage("VirtualEnviromentLoaded", house.gameObject, SendMessageOptions.DontRequireReceiver);


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