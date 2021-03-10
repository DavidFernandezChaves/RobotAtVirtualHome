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

    public class AIWander : VirtualRobots {

        public bool cyclicalBehaviour;
        public bool randomSecuence;
        
        public float frequencyCapture;
        
        public List<Vector3> VisitPoints { get; private set; }
        public string currentRoom { get; private set; }

        public bool captureRGB;
        public bool captureDepth;
        public bool captureSemanticMask;

        private int index = 0;    
        private int index2 = 0;

        #region Unity Functions
        private void Awake() {
            VisitPoints = new List<Vector3>();
            agent = GetComponent<NavMeshAgent>();
            smartCamera = GetComponentInChildren<SmartCamera>();
            if (smartCamera == null) {
                LogWarning("Smart camera not found");
            }
            state = StatusMode.Loading;
        }

        void Start() {

            var rooms = FindObjectsOfType<Room>();

            foreach (Room r in rooms) {
                foreach (Light l in r.generalLights) {
                    var point = l.transform.position;
                    point.y = 0;
                    VisitPoints.Add(point);
                }
            }

            if (record) {
                string tempPath = Path.Combine(filePath, "Wandering");
                int i = 0;
                while (Directory.Exists(tempPath)) {
                    i++;
                    tempPath = Path.Combine(filePath, "Wandering" + i);
                }

                filePath = tempPath;
                if (!Directory.Exists(filePath)) {
                    Directory.CreateDirectory(filePath);
                }

                Log("The saving path is:" + filePath);
                writer = new StreamWriter(filePath + "/Info.csv", true);
                writer.WriteLine("photoID;robotPosition;robotRotation;cameraPosition;cameraRotation;room");
                StartCoroutine("Record");
            }

            agent.SetDestination(VisitPoints[0]);
            agent.isStopped = false;
            state = StatusMode.Walking;

        }

        void Update() {
            switch (state) {
                case StatusMode.Walking:
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit)) {
                        currentRoom = hit.transform.name;
                    }

                    if (agent.remainingDistance <= agent.stoppingDistance &&
                        agent.velocity.sqrMagnitude == 0f) {
                        Vector3 nextGoal;
                        if (GetNextGoal(out nextGoal)) {
                            agent.SetDestination(nextGoal);
                            agent.isStopped = false;
                            Log("Next goal:" + nextGoal.ToString());
                            state = StatusMode.Loading;
                            StartCoroutine(DoOnGoal());
                        } else {
                            state = StatusMode.Finished;
                            Log("Finish");
                            GetComponent<AudioSource>().Play();
                        }
                    }
                    break;
            }
        }

        private void OnDestroy() {
            if (this.enabled && record) {
                writer.Close();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (Application.isPlaying && this.enabled && verbose > 0) {
                Gizmos.color = Color.green;
                foreach (Vector3 point in VisitPoints) {
                    Gizmos.DrawSphere(point, 0.1f);
                }
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(agent.destination, 0.2f);
            }
        }
#endif

        #endregion

        #region Public Functions

        #endregion

        #region Private Functions
        private IEnumerator DoOnGoal() {
            yield return new WaitForSeconds(3);
            state = StatusMode.Walking;
        }

        private bool GetNextGoal(out Vector3 result) {
            result = Vector3.zero;
            if (cyclicalBehaviour) {
                if (randomSecuence) {
                    result = VisitPoints[UnityEngine.Random.Range(0, VisitPoints.Count)];
                } else {
                    index++;
                    if (index >= VisitPoints.Count) {
                        index = 0;
                    }
                    result = VisitPoints[index];
                }
            } else {
                VisitPoints.RemoveAt(index);
                if (VisitPoints.Count == 0) {
                    return false;
                }

                if (randomSecuence) {
                    result = VisitPoints[UnityEngine.Random.Range(0, VisitPoints.Count)];
                } else {
                    result = VisitPoints[index];
                }
            }
            return true;
        }

        private IEnumerator Record() {
            while (state != StatusMode.Finished) {

                yield return new WaitForEndOfFrame();
                agent.isStopped = true;
                yield return new WaitForSeconds(0.1f);
                byte[] itemBGBytes;
                if (captureSemanticMask) {
                    writer.WriteLine(index2.ToString() + "_mask.png;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                        + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    itemBGBytes = smartCamera.GetImageMask().EncodeToPNG();
                    File.WriteAllBytes(filePath + "/" + index2.ToString() + "_mask.png", itemBGBytes);
                }

                if (captureRGB) {
                    writer.WriteLine(index2.ToString() + "_rgb.png;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                        + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    itemBGBytes = smartCamera.ImageRGB.EncodeToPNG();
                    File.WriteAllBytes(filePath + "/" + index2.ToString() + "_rgb.png", itemBGBytes);

                }

                if (captureDepth) {
                    writer.WriteLine(index2.ToString() + "_depth.png;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                        + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    itemBGBytes = smartCamera.ImageDepth.EncodeToPNG();
                    File.WriteAllBytes(filePath + "/" + index2.ToString() + "_depth.png", itemBGBytes);
                }

                agent.isStopped = false;
                index2++;
                if (frequencyCapture != 0) {
                    yield return new WaitForSeconds(frequencyCapture);
                }

            }
        }

        #endregion
    }
}
