using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RobotAtVirtualHome {
    public class VirtualObject : MonoBehaviour {

        public int verbose;
        public ObjectTag[] tags;
        public VirtualObject inheritedSeed;
        public bool forceSeed;
        public int seedForced=0;

        public GameObject[] models;

        public int seed { get; private set; }
        public Room room { get; private set; }

        #region Unity Functions
        private void Awake() {
            verbose = FindObjectOfType<House>().verbose;
            seed = Random.Range(0, models.Length);

            foreach (GameObject go in models) {
                go.SetActive(false);
            }

            if(models == null || models.Length == 0) {
                Log("Unassigned model");
            }
        }

        void Start() {
            if (tags == null || tags.Length == 0) {
                LogWarning("Unassigned tag");
            }

            Transform t = FindObjectOfType<VirtualEnvironment>().FindObjectUPWithClass(typeof(Room), transform);
            if (t != FindObjectOfType<VirtualEnvironment>()) {
                room = t.GetComponent<Room>();
                switch (room.forceSeed) {
                    case mode.On:
                        forceSeed = true;
                        seedForced = room.seedForced;
                        break;
                    case mode.Off: forceSeed = false; break;
                    case mode.Radomly: forceSeed = false; break;
                }
            } else {
                LogWarning("Room not found");
            }

            if (forceSeed) {
                seed = seedForced;
            }else if(inheritedSeed != null){
                seed = inheritedSeed.seed;
            }

            if(seed < models.Length) {
                Log("Selected style: " + seed.ToString());
                models[seed].SetActive(true);
            } else {
                if (models != null && models.Length > 0) {
                    Log("The model closest to: " + seed.ToString() + "was selected");
                    models[models.Length-1].SetActive(true);
                }
            }

            transform.name = FindObjectOfType<House>().RegistVirtualObject(this);
        }
        #endregion

        #region Public Functions
        public Transform GetModel() {
            return models[seed].transform;
        }
        #endregion

        #region Private Functions
        private void Log(string _msg) {
            if (verbose > 1)
                Debug.Log("[VirtualObject-" + transform.name + "]: " + _msg);
        }

        private void LogWarning(string _msg) {
            if (verbose > 0)
                Debug.LogWarning("[VirtualObject-" + transform.name + "]: " + _msg);
        }
        #endregion
    }
}