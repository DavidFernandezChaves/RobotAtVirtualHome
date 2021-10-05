using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RobotAtVirtualHome {

    public class Room : MonoBehaviour {
        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;

        [Tooltip("Room type")]
        public RoomType roomType;

        [Header("Customization")]        
        public LightLevel initialStateGeneralLight = LightLevel.On;
        public LightLevel initialStateLights = LightLevel.Radomly;
        public bool randomStateDoor = false;
        public bool randomWallPainting = true;
        public bool randomFloorPainting = true;
        public bool randomObjectModel = false;

        [Header("Preloading materials")]
        public Material[] wallPaints;
        public Material[] floorPaints;

        public List<Light> generalLights { get; private set; }
        public List<Door> doors { get; private set; }
        public List<Light> lamps { get; private set; }

        #region Unity Functions
        private void Awake() {
            LogLevel = FindObjectOfType<House>().LogLevel;
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

            if(randomStateDoor)
                SetDoors(Random.value >= 0.5f);

            switch (initialStateGeneralLight) {
                case LightLevel.On:
                    TurnGeneralLight(true);
                    break;
                case LightLevel.Off:
                    TurnGeneralLight(false);
                    break;
                case LightLevel.Radomly:
                    TurnGeneralLight(Random.value >= 0.5f);
                    break;
            }

            switch (initialStateLights) {
                case LightLevel.On:
                    TurnLight(true);
                    break;
                case LightLevel.Off:
                    TurnLight(false);
                    break;
                case LightLevel.Radomly:
                    TurnLight(Random.value >= 0.5f);
                    break;
            }

            if (randomWallPainting) {
                if (wallPaints.Length > 0)
                {
                    PaintWall(wallPaints[Random.Range(0, wallPaints.Length)]);
                }
                else
                {
                    Log("No wall painting added", LogLevel.Error, true);
                }
            }

            if (randomFloorPainting) {
                if (floorPaints.Length > 0)
                {
                    PaintFloor(floorPaints[0]);
                }
                else
                {
                    Log("No floor painting added", LogLevel.Error, true);
                }
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
        private void Log(string _msg, LogLevel lvl, bool Warning=false) {
            if (LogLevel <= lvl)
            {
                if (Warning)
                {
                    Debug.LogWarning("[Room " + roomType.ToString() + "]: " + _msg);
                }
                else
                {
                    Debug.Log("[Room " + roomType.ToString() + "]: " + _msg);
                }
            }
                
        }
        #endregion
    }
}

