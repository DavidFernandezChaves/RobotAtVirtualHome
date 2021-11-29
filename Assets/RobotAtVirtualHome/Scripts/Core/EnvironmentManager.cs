using ROSUnityCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.AI.Navigation;
using UnityEngine;


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
        public string path { get; private set; }
        public House house { get; private set; }

        public List<GameObject> agents { get; private set; }

        private StreamWriter writer;
        private int houseSelected;


        #region Unity Functions
        private void Awake() {
            agents = new List<GameObject>();

            if (houses != null && houses.Count > 0) {
                houseSelected = m_simulationOptions.houseSelected;
                if (houseSelected == 0) {
                    houseSelected = UnityEngine.Random.Range(1, houses.Count);
                }              

                if (house = Instantiate(houses[houseSelected - 1], transform).GetComponent<House>()) {
                    path = Path.Combine(m_simulationOptions.path, "Home" + houseSelected.ToString("D2"));
                    if (recordEnvironmentDatas) {                        
                        if (!Directory.Exists(path)) {
                            Directory.CreateDirectory(path);
                        }
                        Log("The saving path is:" + path, LogLevel.Normal);
                    }

                } else {
                    Log("The gameObject " + (m_simulationOptions.houseSelected - 1) + " does not have the 'House' component.",LogLevel.Error,true);
                }

            } else { Log("There are no assigned houses in the virtual environment.",LogLevel.Error,true); }
        }

        private void Start()
        {            
            house.LoadHouse();
            house.SetTransparentRoof(transparentRoof);
            transform.GetChild(0).rotation = Quaternion.Euler(m_simulationOptions.SunRotation, 0, 0);
            StartCoroutine(LoadingEnvironment());            
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
        private void VirtualAgentsIntanciation()
        {
            VirtualObject station = house.virtualObjects.Find(obj => obj.tags.Contains(ObjectTag.Station));
            if (station != null)
            {
                var origin = station.transform.position;                

                foreach (Agent r in m_simulationOptions.agentsToInstantiate)
                {
                    Transform agent = Instantiate(r.prefab, origin, Quaternion.identity, house.transform.parent).transform;
                    agent.GetComponent<ROS>().robotName = r.name;
                    agent.name = r.name;
                    agent.GetComponent<ROS>().Connect(r.ip);
                    agents.Add(agent.gameObject);
                }
            }
            else { Log("This house don't have robot station", LogLevel.Error, true); }
        }

        private void UserInstanciation()
        {
            VirtualObject station = house.virtualObjects.Find(obj => obj.tags.Contains(ObjectTag.Station));
            if (m_simulationOptions.userPrefab != null && station != null)
            {
                var origin = station.transform.position + new Vector3(0,1,0);

                Transform agent = Instantiate(m_simulationOptions.userPrefab, origin, Quaternion.identity, house.transform.parent).transform;
            }
        }

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
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForEndOfFrame();
            transform.GetComponent<NavMeshSurface>().BuildNavMesh();

            if (m_simulationOptions.simulationLog != null)
            {
                using (StringReader sr = new StringReader(m_simulationOptions.simulationLog.text))
                {
                    string line;
                    string[] values;                    
                    while ((line = sr.ReadLine()) != null)
                    {
                        values = line.Split(',');
                        switch (values[0])
                        {
                            case "House Selected":
                                if (!values[1].Equals(houseSelected.ToString()))
                                {
                                    sr.ReadToEnd();
                                    Log("The selected house does not match the house in the entered log file.", LogLevel.Normal, true);
                                }
                                break;
                            case "Sun Rotation":
                                transform.GetChild(0).rotation = Quaternion.Euler(float.Parse(values[1]), 0, 0);
                                break;
                            case "Room":
                                Room room = GameObject.Find(values[1]).GetComponent<Room>();
                                if (room != null)
                                {
                                    room.PaintWall((Material)Resources.Load("Walls/" + values[2], typeof(Material)));
                                    room.PaintFloor((Material)Resources.Load("Floors/" + values[3], typeof(Material)));
                                }
                                else
                                {
                                    Log("Room " + values[1] + " not found.", LogLevel.Error, true);
                                }
                                break;
                            case "-":
                                try
                                {                                    
                                    VirtualObject vo = house.virtualObjects.Find(obj => obj.m_id.Equals(values[1]));
                                    vo.SetSeed(int.Parse(values[2]));
                                    if (vo.tags.Contains(ObjectTag.Light) || vo.tags.Contains(ObjectTag.Lamp) || vo.tags.Contains(ObjectTag.Lighter))
                                    {
                                        vo.GetComponentInChildren<Light>().enabled = Boolean.Parse(values[3]);
                                    }
                                    else if (vo.tags.Contains(ObjectTag.Door))
                                    {
                                        vo.GetComponent<Door>().SetDoor(Boolean.Parse(values[3]));
                                    }                                    
                                }
                                catch
                                {
                                    Log("Object " + values[1] + " not found.", LogLevel.Error, true);
                                }
                                break;
                        }
                    }

                }

            }

            if (recordEnvironmentDatas)
            {
                writer = new StreamWriter(path + "/EnviromentLog.csv", true);

                writer.WriteLine("House Selected" + "," + houseSelected.ToString());
                writer.WriteLine("Sun Rotation" + "," + m_simulationOptions.SunRotation.ToString());

                foreach (Room room in FindObjectsOfType<Room>())
                {
                    writer.WriteLine("Room,"+room.transform.name+","+room.wallMaterial.name + "," + room.floorMaterial.name);
                }

                writer.WriteLine(" ,Id,Seed,Mode,ColorR,ColorG,ColorB,Room,RoomType,XGlobalPosition,YGlobalPosition,ZGlobalPosition,XRotation,YRotation,ZRotation,Tags");
                StringBuilder line = new StringBuilder();
                foreach (VirtualObject obj in house.virtualObjects)
                {
                    if (obj.isActiveAndEnabled)
                    {
                        line = new StringBuilder();
                        line.Append("-");
                        line.Append(",");
                        line.Append(obj.m_id.ToString(CultureInfo.InvariantCulture));
                        line.Append(",");
                        line.Append(obj.m_seed.ToString(CultureInfo.InvariantCulture));
                        line.Append(",");

                        if (obj.tags.Contains(ObjectTag.Light))
                        {
                            line.Append(obj.GetComponentInChildren<Light>().enabled.ToString());
                        }
                        else if (obj.tags.Contains(ObjectTag.Lamp) || obj.tags.Contains(ObjectTag.Lighter))
                        {
                            line.Append(obj.GetComponentInChildren<Light>().enabled.ToString());
                        }
                        else if (obj.tags.Contains(ObjectTag.Door))
                        {
                            line.Append(obj.GetComponent<Door>().m_state.ToString());
                        }
                        else
                        {
                            line.Append("-");
                        }

                        line.Append(",");
                        line.Append(house.semanticColors[obj.m_id].r.ToString());
                        line.Append(",");
                        line.Append(house.semanticColors[obj.m_id].g.ToString());
                        line.Append(",");
                        line.Append(house.semanticColors[obj.m_id].b.ToString());
                        line.Append(",");
                        line.Append(obj.room.transform.name.ToString());
                        line.Append(",");
                        line.Append(obj.room.roomType.ToString());
                        line.Append(",");
                        line.Append(((double) obj.transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                        line.Append(",");
                        line.Append(((double)obj.transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                        line.Append(",");
                        line.Append(((double)obj.transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                        line.Append(",");
                        line.Append(((double)obj.transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                        line.Append(",");
                        line.Append(((double)obj.transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                        line.Append(",");
                        line.Append(((double)obj.transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                        foreach (ObjectTag ot in obj.tags)
                        {
                            line.Append(",");
                            line.Append(ot.ToString());
                        }
                        writer.WriteLine(line);
                    }
                }
                writer.Close();
            }

            VirtualAgentsIntanciation();
            UserInstanciation();
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