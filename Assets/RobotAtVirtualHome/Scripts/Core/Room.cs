using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RobotAtVirtualHome {

    public enum mode { On, Off, Radomly, None }

    public class Room : MonoBehaviour {

        public int verbose;

        public RoomType roomType;

        public mode initialStateDoor = mode.Off;
        public mode initialStateGeneralLight = mode.On;
        public mode initialStateLights = mode.Radomly;
        public mode wallPainting = mode.Radomly;
        public mode floorPainting = mode.Radomly;
        public mode forceSeed = mode.None;
        public int seedForced = 0;

        public Material[] wallPaints;
        public Material[] floorPaints;

        public List<Light> generalLights { get; private set; }
        public List<Door> doors { get; private set; }
        public List<Light> lamps { get; private set; }

        #region Unity Functions
        private void Awake() {
            verbose = FindObjectOfType<House>().verbose;
            generalLights = new List<Light>();
            lamps = new List<Light>();
            doors = new List<Door>();


            for (int i = 0; i < transform.childCount; i++) {
                Transform editingTransform = transform.GetChild(i);
                
                //General Light Case
                Light light = editingTransform.GetComponent<Light>();
                if (light != null) {
                    generalLights.Add(light);
                    editingTransform.name = "Light_" + generalLights.Count();
                }
            }

            lamps = GetComponentsInChildren<Light>(true).ToList();
            generalLights.ForEach(l => lamps.Remove(l));
            doors = GetComponentsInChildren<Door>().ToList();
            
        }

        private void Start() {
            //If you are using Vimantic architecture
            gameObject.AddComponent<SemanticRoom>();

            SendMessage("SetRoomID", transform.name,SendMessageOptions.DontRequireReceiver);
            SendMessage("SetRoomType", roomType.ToString(),SendMessageOptions.DontRequireReceiver);

            switch (initialStateDoor) {
                case mode.On:
                    SetDoors(true);
                    break;
                case mode.Off:
                    SetDoors(false);
                    break;
                case mode.Radomly:
                    SetDoors(Random.value >= 0.5f);
                    break;
            }

            switch (initialStateGeneralLight) {
                case mode.On:
                    TurnGeneralLight(true);
                    break;
                case mode.Off:
                    TurnGeneralLight(false);
                    break;
                case mode.Radomly:
                    TurnGeneralLight(Random.value >= 0.5f);
                    break;
            }

            switch (initialStateLights) {
                case mode.On:
                    TurnLight(true);
                    break;
                case mode.Off:
                    TurnLight(false);
                    break;
                case mode.Radomly:
                    TurnLight(Random.value >= 0.5f);
                    break;
            }

            switch (wallPainting) {
                case mode.On:
                    if (wallPaints.Length > 0) {
                        PaintWall(wallPaints[Random.Range(0, wallPaints.Length)]);
                    } else {
                        LogWarning("No wall painting added");
                    }
                    break;
                case mode.Radomly:
                    Material[] mts = Resources.LoadAll("RobotAtVirtualHome/Materials/Walls", typeof(Material)).Cast<Material>().ToArray();
                    PaintWall(mts[Random.Range(0, mts.Length)]);
                    break;
            }

            switch (floorPainting) {
                case mode.On:
                    if (floorPaints.Length > 0) {
                        PaintFloor(floorPaints[Random.Range(0, floorPaints.Length)]);
                    } else {
                        LogWarning("No floor painting added");
                    }
                    break;
                case mode.Radomly:
                    Material[] mts = Resources.LoadAll("RobotAtVirtualHome/Materials/Floors", typeof(Material)).Cast<Material>().ToArray();
                    PaintFloor(mts[Random.Range(0, mts.Length)]);
                    break;
            }
        }
        #endregion

        #region Public Functions
        public void SetDoors(bool _state) {
            doors.ForEach(d=> d.SetDoor(_state));
        }

        public void TurnGeneralLight(bool _state) {
            generalLights.ForEach(l => l.enabled = _state);
        }

        public void TurnLight(bool _state) {
            lamps.ForEach(l => l.enabled = _state);
        }

        public void PaintWall(Material paint) {
            var mats = GetComponent<MeshRenderer>().materials;
            if (mats[0].name.ToLower().Contains("wall")) {
                mats[0] = paint;
            } else {
                mats[1] = paint;
            }            
            GetComponent<MeshRenderer>().materials = mats;
        }

        public void PaintFloor(Material paint) {
            var mats = GetComponent<MeshRenderer>().materials;
            if (mats[0].name.ToLower().Contains("floor")) {
                mats[0] = paint;
            } else {
                mats[1] = paint;
            }
            GetComponent<MeshRenderer>().materials = mats;
        }
        #endregion

        #region Private Functions
        private void Log(string _msg) {
            if (verbose > 1)
                Debug.Log("[Room " + roomType.ToString() + "]: " + _msg);
        }

        private void LogWarning(string _msg) {
            if (verbose > 0)
                Debug.LogWarning("[Room " + roomType.ToString() + "]: " + _msg);
        }
        #endregion
    }
}

