﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace RobotAtVirtualHome {

    [RequireComponent(typeof(NavMeshAgent))]

    public class AIWander : VirtualAgent {

        [Header("Behaviour")]
        public bool cyclical;
        public bool randomSecuence;

        [Header("Capture Data")]
        [Tooltip("Time between data capture")]
        [Range(0.1f, 10)]
        public float timeBetweenDataCapture = 0.5f;
        public bool captureRGB;
        public bool captureDepth;
        public bool captureSemanticMask;
        public bool captureScan;

        public event Action OnEndRute;

        public List<Vector3> VisitPoints { get; private set; }
        public string currentRoom { get; private set; }
        public string filePath { get; private set; }

        private StreamWriter logImgWriter;
        private StreamWriter logScanWriter;
        private int index = 0;    
        private int index2 = 0;

        #region Unity Functions
        void Start() {
            VisitPoints = new List<Vector3>();

            var house = FindObjectOfType<House>();

            foreach (VirtualObject light in house.virtualObjects.FindAll(obj => obj.tags.Contains(ObjectTag.Light))) {
                var point = light.transform.position;
                point.y = 0;
                VisitPoints.Add(point);
            }
            
            if (captureRGB || captureDepth || captureSemanticMask || captureScan) {
                filePath = FindObjectOfType<EnvironmentManager>().path;
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

                if(captureRGB || captureDepth || captureSemanticMask) {
                    logImgWriter = new StreamWriter(filePath + "/LogImg.csv", true);
                    logImgWriter.WriteLine("photoID;robotPosition;robotRotation;cameraPosition;cameraRotation;room");
                }

                if (captureScan) {
                    logScanWriter = new StreamWriter(filePath + "/LogScan.csv", true);
                    logScanWriter.WriteLine("scanID;robotPosition;robotRotation;data");
                }                

                Log("The saving path is:" + filePath,LogLevel.Normal);
                

            }

            agent.SetDestination(VisitPoints[0]);
            agent.isStopped = false;
            state = StatusMode.Walking;

            StartCoroutine(CaptureData());

        }

        private void OnDestroy()
        {
            if (logImgWriter != null)
            {
                logImgWriter.Close();
            }
            if (logScanWriter != null)
            {
                logScanWriter.Close();
            }
        }

        void Update() {
            switch (state) {
                case StatusMode.Walking:
                    if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out RaycastHit hit))
                    {
                        currentRoom = hit.transform.name;
                    }
                    
                    if (agent.remainingDistance <= agent.stoppingDistance &&
                        agent.velocity.sqrMagnitude == 0f) {
                        Vector3 nextGoal;
                        if (GetNextGoal(out nextGoal)) {
                            agent.SetDestination(nextGoal);
                            agent.isStopped = false;
                            Log("Next goal:" + nextGoal.ToString(),LogLevel.Normal);
                            state = StatusMode.Loading;
                            StartCoroutine(DoOnGoal());
                        } else {
                            state = StatusMode.Finished;
                            Log("Finish", LogLevel.Normal);
                            GetComponent<AudioSource>().Play();
                            OnEndRute?.Invoke();
                        }
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (Application.isPlaying && this.enabled && LogLevel >= LogLevel.Normal) {
                Gizmos.color = new Color(0, 0.9f, 1, 0.3f);
                foreach (Vector3 point in VisitPoints) {
                    Gizmos.DrawSphere(point, 0.1f);
                }
                Gizmos.color = new Color(1, 0, 0.9f, 0.8f);
                Gizmos.DrawSphere(agent.destination, 0.1f);
            }
        }
#endif

        #endregion


        #region Private Functions
        private IEnumerator DoOnGoal() {
            yield return new WaitForSeconds(1);
            state = StatusMode.Walking;
            agent.isStopped = false;
        }

        private bool GetNextGoal(out Vector3 result) {
            result = Vector3.zero;
            if (cyclical) {
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

        private IEnumerator CaptureData()
        {
            while (state != StatusMode.Finished)
            {

                yield return new WaitForEndOfFrame();

                agent.isStopped = true;
                if (captureRGB)
                {
                    logImgWriter.WriteLine(index2.ToString() + "_rgb.jpg;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                            + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    File.WriteAllBytes(filePath + "/" + index2.ToString() + "_rgb.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.RGB).EncodeToJPG());
                }
                yield return null;
                if (captureDepth)
                {
                    logImgWriter.WriteLine(index2.ToString() + "_depth.jpg;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                    + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    File.WriteAllBytes(filePath + "/" + index2.ToString() + "_depth.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.Depth).EncodeToJPG());
                }
                yield return null;
                if (captureSemanticMask)
                {
                    logImgWriter.WriteLine(index2.ToString() + "_mask.jpg;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                    + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    File.WriteAllBytes(filePath + "/" + index2.ToString() + "_mask.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.InstanceMask).EncodeToJPG());
                }
                if (captureScan)
                {
                    string data = "";
                    foreach (float d in laserScan.Scan())
                    {
                        data += d.ToString() + ";";
                    }
                    logScanWriter.WriteLine(index2.ToString() + transform.position + ";" + transform.rotation.eulerAngles + ";" + data);
                }

                agent.isStopped = false;
                index2++;
                yield return new WaitForSeconds(timeBetweenDataCapture);
            }
        }

        #endregion
    }
}
