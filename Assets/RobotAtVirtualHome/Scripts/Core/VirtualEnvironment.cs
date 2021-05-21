﻿using ROSUnityCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;


namespace RobotAtVirtualHome {
    public class VirtualEnvironment : MonoBehaviour {

        public int verbose;
        public int houseSelected = 0;
        
        public bool recordHierarchy;
        public bool transparentRoof;
        public mode initialStateDoor;
        public mode initialStateGeneralLight;
        public mode initialStateLights;
        public mode wallPainting;
        public mode floorPainting;
        public mode forceSeed;
        public int seedForced;

        public string path = @"D:\";
        private House house;
        
        private StreamWriter writer;

        #region Unity Functions
        private void Awake() {
            var houses = Resources.LoadAll("RobotAtVirtualHome/Houses", typeof(GameObject)).Cast<GameObject>().ToList();
            if (houses != null && houses.Count > 0) {

                if (houseSelected == 0) {
                    houseSelected = UnityEngine.Random.Range(1, houses.Count);
                }              

                if (house = Instantiate(houses[houseSelected-1], transform).GetComponent<House>()) {
                    house.transform.position = house.transform.position + new Vector3(0, 1.1f, 0);
                    path = Path.Combine(path, "House" + (houseSelected));
                    if (recordHierarchy) {                        
                        if (!Directory.Exists(path)) {
                            Directory.CreateDirectory(path);
                        }
                        Log("The saving path is:" + path);
                    }

                    house.SetTransparentRoof(transparentRoof);
                    foreach(Room room in house.transform.GetComponentsInChildren<Room>()) {
                        if (initialStateDoor != mode.None)
                            room.initialStateDoor = initialStateDoor;
                        if(initialStateGeneralLight != mode.None)
                            room.initialStateGeneralLight = initialStateGeneralLight;
                        if(initialStateLights != mode.None)
                            room.initialStateLights = initialStateLights;
                        if(wallPainting != mode.None)
                            room.wallPainting = wallPainting;
                        if(floorPainting != mode.None)
                        room.floorPainting = floorPainting;
                        if (forceSeed != mode.None) {
                            room.forceSeed = forceSeed;
                            room.seedForced = seedForced;
                        }
                            
                    }
                    Invoke("StartSimulation", 0.2f);
                } else {
                    LogWarning("The gameObject " + (houseSelected-1) + " does not have the 'House' component.");
                }

            } else { LogWarning("There are no assigned houses in the virtual environment."); }
        }        

        private void StartSimulation() {
            if (recordHierarchy) {
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
            GameObject.FindGameObjectsWithTag("GeneralScripts").ToList().ForEach(G=>G.SendMessage("VirtualEnviromentLoaded", house.gameObject, SendMessageOptions.DontRequireReceiver));

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
        private void Log(string _msg) {
            if (verbose > 1)
                Debug.Log("[Virtual Environment]: " + _msg);
        }

        private void LogWarning(string _msg) {
            if (verbose > 0)
                Debug.LogWarning("[Virtual Environment]: " + _msg);
        }
        #endregion
    }
}