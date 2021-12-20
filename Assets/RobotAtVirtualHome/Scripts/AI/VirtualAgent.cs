using ROSUnityCore;
using ROSUnityCore.ROSBridgeLib.geometry_msgs;
using ROSUnityCore.ROSBridgeLib.nav_msgs;
using ROSUnityCore.ROSBridgeLib.std_msgs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace RobotAtVirtualHome {

    [RequireComponent(typeof(NavMeshAgent))]

    public class VirtualAgent : MonoBehaviour {

        public enum StatusMode { Loading, Walking, Turning, Finished }
        public enum PathType { Beacons, Interpolated }

        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;

        [SerializeField]
        protected StatusMode state;


        [Header("Path to ROS")]
        public bool sendPathToROS;
        public PathType pathType = PathType.Beacons;
        public float rOSFrecuencyPath = 1;

        [Header("Odometry to ROS")]
        public bool sendOdometryToROS;
        public float rOSFrecuencyOdometry = 1;

        protected NavMeshAgent agent;

        #region Unity Functions
        protected void Awake() {
            agent = GetComponent<NavMeshAgent>();
        }
        #endregion

        #region Public Functions
        public void Connected(ROS ros) {
            if (enabled && sendPathToROS) {
                Log("Sending path to ROS.",LogLevel.Normal);
                ros.RegisterPubPackage("Path_pub");
                if (pathType == PathType.Beacons) {
                    StartCoroutine(SendPathToROS(ros));
                } else {
                    StartCoroutine(SendInterpolatedPathToROS(ros));
                }
            }
            if (enabled && sendOdometryToROS) {
                Log("Sending odometry to ROS.",LogLevel.Normal);
                ros.RegisterPubPackage("Odometry_pub");
                StartCoroutine(SendOdometryToROS(ros));
            }
        }

        public string GetTransformString()
        {
            return ((double)transform.position.x).ToString("F15", CultureInfo.InvariantCulture) + "," +
                    ((double)transform.position.y).ToString("F15", CultureInfo.InvariantCulture) + "," +
                    ((double)transform.position.z).ToString("F15", CultureInfo.InvariantCulture) + "," +
                    ((double)transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture) + "," +
                    ((double)transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture) + "," +
                    ((double)transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture);
        }
        #endregion

        #region Private Functions  
        private IEnumerator SendPathToROS(ROS ros) {
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
                yield return new WaitForSeconds(rOSFrecuencyPath);
            }
        }

        // This function was design by: Jose Luiz Matez
        private IEnumerator SendInterpolatedPathToROS(ROS ros) {
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
                yield return new WaitForSeconds(rOSFrecuencyPath);
            }
        }

        // Send Odometry to ROS
        private IEnumerator SendOdometryToROS(ROS ros) {
            while (Application.isPlaying) {
                if (ros.IsConnected()) {

                    double[] covariance_pose = new double[36];

					for (int i = 0; i < covariance_pose.Length; i++) {
						covariance_pose[i] = 0.0f;
					}
                    
                    HeaderMsg globalHead = new HeaderMsg(0, new TimeMsg(DateTime.Now.Second, 0), "map");
                    PoseWithCovarianceMsg pose = new PoseWithCovarianceMsg(new PoseMsg(new PointMsg(transform.position, true), new QuaternionMsg(transform.rotation, true)),
                                                                            covariance_pose);
                    TwistWithCovarianceMsg twist = new TwistWithCovarianceMsg(new TwistMsg(new Vector3Msg(agent.velocity, true), new Vector3Msg(0.0f, 0.0f, 0.0f)), covariance_pose);
                    
                    OdometryMsg odometrymsg = new OdometryMsg(globalHead, "odom", pose, twist);
                    ros.Publish(Odometry_pub.GetMessageTopic(), odometrymsg);
                }
                yield return new WaitForSeconds(rOSFrecuencyOdometry);
            }
        }

        protected void Log(string _msg, LogLevel lvl, bool Warning = false)
        {
            if (LogLevel <= lvl && LogLevel != LogLevel.Nothing)
            {
                if (Warning)
                {
                    Debug.LogWarning("[VirtualRobot]: " + _msg);
                }
                else
                {
                    Debug.Log("[VirtualRobot]: " + _msg);
                }
            }
        }
        #endregion
    }
}