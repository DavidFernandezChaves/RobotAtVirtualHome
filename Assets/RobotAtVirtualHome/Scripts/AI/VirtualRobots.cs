using ROSUnityCore;
using ROSUnityCore.ROSBridgeLib.geometry_msgs;
using ROSUnityCore.ROSBridgeLib.nav_msgs;
using ROSUnityCore.ROSBridgeLib.std_msgs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace RobotAtVirtualHome {

    [RequireComponent(typeof(NavMeshAgent))]

    public class VirtualRobots : MonoBehaviour {

        public enum StatusMode { Loading, Walking, Turning, Finished }
        public enum PathType { Beacons, Interpolated }

        public int verbose;
        public StatusMode state { get; protected set; }

        public bool record;

        public bool sendPathToROS;
        public PathType pathType = PathType.Beacons;
        public float rOSFrecuency = 1;

        public bool sendMapToROS;

        public string filePath { get; protected set; }
        protected SmartCamera smartCamera;
        protected NavMeshAgent agent;
        protected StreamWriter writer;
        protected ROS ros;

        #region Unity Functions
        private void Awake() {
            agent = GetComponent<NavMeshAgent>();
            smartCamera = GetComponentInChildren<SmartCamera>();
            if (smartCamera == null) {
                LogWarning("Smart camera not found");
            }
        }

        void Start() {
            filePath = FindObjectOfType<VirtualEnvironment>().path;

            if (ros == null) {
                ros = transform.root.GetComponentInChildren<ROS>();
            }

            if (sendPathToROS && ros != null) {
                Log("Send path to ros: Ok");
                ros.RegisterPubPackage("Path_pub");
                if(pathType == PathType.Beacons) {
                    StartCoroutine(SendPathToROS());
                } else {
                    StartCoroutine(SendInterpolatedPathToROS());
                }                
            } else {
                Log("Send path to ros: False");
            }

            if (sendMapToROS) {
                Log("Send map to ros: Ok");
                ros.RegisterPubPackage("Map_pub");
            }
        }

        #endregion

        #region Public Functions

        public void Connected() {
            if (sendMapToROS) {
                int house = FindObjectOfType<VirtualEnvironment>().houseSelected;
                TextAsset map = (TextAsset) Resources.Load("RobotAtVirtualHome/Maps/House" + house);
                OccupancyGridMsg occupancyGrid = new OccupancyGridMsg(new HeaderMsg(0, new TimeMsg(DateTime.Now.Second, 0), ""),map,0.05f,new Vector3(-20,-20,0));
                if (ros == null) {
                    ros = transform.root.GetComponentInChildren<ROS>();
                }
                Debug.Log(occupancyGrid.GetData().Length);
                ros.Publish(Map_pub.GetMessageTopic(), occupancyGrid);
            }
        }

        #endregion

        #region Private Functions      
        private IEnumerator SendPathToROS() {
            while (Application.isPlaying) {
                if (ros.IsConnected()) {
                    Vector3[] points = agent.path.corners;
                    PoseStampedMsg[] poses = new PoseStampedMsg[points.Length];
                    HeaderMsg head = new HeaderMsg(0, new TimeMsg(DateTime.Now.Second, 0), "map");
                    Quaternion rotation = transform.rotation;
                    for (int i = 0; i < points.Length; i++) {
                        head.SetSeq(i);
                        if (i > 0) {
                            rotation = Quaternion.FromToRotation(points[i - 1], points[i]);
                        }

                        poses[i] = new PoseStampedMsg(head, new PoseMsg(points[i], rotation, true));
                    }

                    HeaderMsg globalHead = new HeaderMsg(0, new TimeMsg(DateTime.Now.Second, 0), "map");
                    PathMsg pathmsg = new PathMsg(globalHead, poses);
                    ros.Publish(Path_pub.GetMessageTopic(), pathmsg);
                }
                yield return new WaitForSeconds(rOSFrecuency);
            }
        }

        // This function was design by: Jose Luiz Matez
        private IEnumerator SendInterpolatedPathToROS() {
            while (Application.isPlaying) {
                if (ros.IsConnected()) {
                    Vector3[] points = agent.path.corners;
                    Vector3[] path = new Vector3[6];
                    float[] distances = new float[points.Length];
                    float[] angles = new float[points.Length];
                    PoseStampedMsg[] poses = new PoseStampedMsg[path.Length];
                    for (int i = 0; i < (points.Length - 1); i++) {
                        if (i == 0) {
                            distances[i] = Vector3.Distance(transform.position, points[i + 1]);
                        } else {
                            distances[i] = Vector3.Distance(points[i], points[i + 1]);
                        }
                        angles[i] = Mathf.Atan2(points[i + 1].z - points[i].z, points[i + 1].x - points[i].x);
                    }
                    path[0] = transform.position;
                    HeaderMsg head = new HeaderMsg(0, new TimeMsg(DateTime.Now.AddSeconds(0).Second, 0), "map");
                    Quaternion rotation = transform.rotation;
                    poses[0] = new PoseStampedMsg(head, new PoseMsg(path[0], rotation, true));
                    head.SetSeq(0);
                    for (int j = 1; j < 6; j++) {
                        float accumulatedDistance = 0.0f;
                        int idx = 0;
                        while (accumulatedDistance < agent.velocity.magnitude && idx < distances.Length) {
                            if (idx < distances.Length) {
                                if (distances[idx] >= (agent.velocity.magnitude - accumulatedDistance)) {
                                    distances[idx] = distances[idx] - (agent.velocity.magnitude - accumulatedDistance);
                                    accumulatedDistance = accumulatedDistance + (agent.velocity.magnitude - accumulatedDistance);
                                } else {
                                    accumulatedDistance = accumulatedDistance + distances[idx];
                                    distances[idx] = 0.0f;
                                    if (idx == (distances.Length - 1)) {
                                        HeaderMsg head_path = new HeaderMsg(0, new TimeMsg(DateTime.Now.AddSeconds(j).Second, 0), "map");
                                        Quaternion add_rotation = Quaternion.Euler(0.0f, angles[idx], 0.0f);
                                        rotation = rotation * add_rotation;
                                        head.SetSeq(j);
                                        path[j] = new Vector3(path[j - 1].x + accumulatedDistance * Mathf.Cos(angles[idx]), 0.0f, path[j - 1].z + accumulatedDistance * Mathf.Sin(angles[idx]));
                                        poses[j] = new PoseStampedMsg(head_path, new PoseMsg(path[j], rotation, true));
                                    }
                                }
                                if (accumulatedDistance == agent.velocity.magnitude) {
                                    HeaderMsg head_path = new HeaderMsg(0, new TimeMsg(DateTime.Now.AddSeconds(j).Second, 0), "map");
                                    Quaternion add_rotation = Quaternion.Euler(0.0f, angles[idx], 0.0f);
                                    rotation = rotation * add_rotation;
                                    head.SetSeq(j);
                                    path[j] = new Vector3(path[j - 1].x + accumulatedDistance * Mathf.Cos(angles[idx]), 0.0f, path[j - 1].z + accumulatedDistance * Mathf.Sin(angles[idx]));
                                    poses[j] = new PoseStampedMsg(head_path, new PoseMsg(path[j], rotation, true));
                                }
                                idx = idx + 1;
                            } else {
                                accumulatedDistance = agent.velocity.magnitude;
                                path[j] = path[j - 1];
                                HeaderMsg head_path = new HeaderMsg(0, new TimeMsg(DateTime.Now.AddSeconds(j).Second, 0), "map");
                                head.SetSeq(j);
                                path[j] = new Vector3(path[j - 1].x + accumulatedDistance * Mathf.Cos(angles[idx]), 0.0f, path[j - 1].z + accumulatedDistance * Mathf.Sin(angles[idx]));
                                poses[j] = new PoseStampedMsg(head_path, new PoseMsg(path[j], rotation, true));
                            }
                        }
                    }
                    if (poses[1] != null) {
                        HeaderMsg globalHead = new HeaderMsg(0, new TimeMsg(DateTime.Now.Second, 0), "map");
                        PathMsg pathmsg = new PathMsg(globalHead, poses);
                        ros.Publish(Path_pub.GetMessageTopic(), pathmsg);
                    }
                }
                yield return new WaitForSeconds(rOSFrecuency);
            }
        }

        protected void Log(string _msg) {
            if (verbose > 1)
                Debug.Log("[VirtualRobot]: " + _msg);
        }

        protected void LogWarning(string _msg) {
            if (verbose > 0)
                Debug.LogWarning("[VirtualRobot]: " + _msg);
        }
        #endregion
    }
}