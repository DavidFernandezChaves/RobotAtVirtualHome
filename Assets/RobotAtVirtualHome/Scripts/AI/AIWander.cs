using RobotAtVirtualHome.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace RobotAtVirtualHome {

    [RequireComponent(typeof(NavMeshAgent))]

    public class AIWander : VirtualAgent {

        [Header("Behaviour")]
        [Range(1, 100)]
        public int nCycles;
        public bool random;

        [Header("Capture Data")]
        [Tooltip("Time between data capture")]
        [Range(0.1f, 10)]
        public float timeBetweenDataCapture = 0.5f;
        public bool captureRGB;
        public bool captureDepth;
        public bool captureSemanticMask;
        public bool captureScan;
        public bool captureLidar;

        public event Action OnEndRute;

        public List<Vector3> VisitPoints { get; private set; }
        public string currentRoom { get; private set; }
        public string filePath { get; private set; }

        private StreamWriter logImgWriter;
        private StreamWriter logScanWriter;
        private StreamWriter logLidarWriter;
        private int index = 0;    
        private int index2 = 0;

        private SmartCamera smartCamera;
        private LaserScanner laserScan;
        private Lidar lidar;

        #region Unity Functions
        private new void Awake()
        {
            base.Awake();
            smartCamera = GetComponentInChildren<SmartCamera>();
            if((captureRGB|| captureDepth || captureSemanticMask) && smartCamera == null)
            {
                captureRGB = false;
                captureDepth = false;
                captureSemanticMask = false;
                Log("Smart camera not found", LogLevel.Error, true);
            }
            laserScan = GetComponentInChildren<LaserScanner>();
            if (captureScan && laserScan == false)
            {
                captureScan = false;
                Log("Laser not found", LogLevel.Error, true);
            }
            lidar = GetComponentInChildren<Lidar>();
            if(captureLidar && lidar == null)
            {
                captureLidar = false;
                Log("Lidar not found", LogLevel.Error, true);
            }
        }

        void Start() {
            VisitPoints = new List<Vector3>();

            var house = FindObjectOfType<House>();

            foreach (VirtualObject light in house.virtualObjects.FindAll(obj => obj.tags.Contains(ObjectTag.Light))) {
                var point = light.transform.position;
                point.y = 0;
                VisitPoints.Add(point);
            }
            
            if (captureRGB || captureDepth || captureSemanticMask || captureScan || captureLidar) {
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
                    logImgWriter.WriteLine("PhotoID,XRobotPosition,YRobotPosition,ZRobotPosition,YRobotRotation,ZRobotRotation,XCameraPosition,YCameraPosition,ZCameraPosition,XCameraRotation,YCameraRotation,ZCameraRotation,Room");
                }

                if (captureScan) {
                    logScanWriter = new StreamWriter(filePath + "/LogScan.csv", true);
                    logScanWriter.WriteLine("ScanID,XRobotPosition,YRobotPosition,ZRobotPosition,YRobotRotation,ZRobotRotation,XLaserPosition,YLaserPosition,ZLaserPosition,XLaserRotation,YLaserRotation,ZLaserRotation,Room,Measures");
                }

                if (captureLidar)
                {
                    var lidarConfigWriter = new StreamWriter(filePath + "/LidarCfg.txt", true);
                    lidarConfigWriter.WriteLine("Resolution (width, height) [px], Vertical FOV (upper, lower) [º], Max distance [m]");
                    lidarConfigWriter.WriteLine(lidar.imageSize[0] + ", " + lidar.imageSize[1] + ", " + lidar.upperViewingAngle + ", " + lidar.bottomViewingAngle + ", " + lidar.maximumDistance);
                    lidarConfigWriter.Close();
                    logLidarWriter = new StreamWriter(filePath + "/LogLidar.csv", true);
                    logLidarWriter.WriteLine("ScanID,XRobotPosition,YRobotPosition,ZRobotPosition,YRobotRotation,ZRobotRotation,XLidarPosition,YLidarPosition,ZLidarPosition,XLidarRotation,YLidarRotation,ZLidarRotation,Room");
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
            if (logLidarWriter != null)
            {
                logLidarWriter.Close();
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
                            if (nCycles > 0)
                            {
                                Debug.Break();
                                nCycles--;
                                index2 = -1;
                                GetComponent<AudioSource>().Play();
                                FindObjectOfType<DetectionResults>().CalculateResults();
                                
                            }
                            else
                            {
                                state = StatusMode.Finished;
                                Log("Finish", LogLevel.Normal);
                                GetComponent<AudioSource>().Play();
                                FindObjectOfType<DetectionResults>().CalculateResults();
                                OnEndRute?.Invoke();
                            }                            
                        }
                    }
                    break;
                case StatusMode.Starting:
                    Start();
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
            if (random)
            {
                result = VisitPoints[UnityEngine.Random.Range(0, VisitPoints.Count)];
                return true;
            }
            else
            {
                index2++;
                if (index2 >= VisitPoints.Count)
                {
                    return false;
                }
                result = VisitPoints[index2];
            }

            return true;
        }

        private IEnumerator CaptureData()
        {            
            while (state != StatusMode.Finished)
            {
                agent.isStopped = true;
                yield return new WaitForEndOfFrame();
                string robotTransform = GetTransformString();
                if (captureRGB)
                {
                    logImgWriter.WriteLine(index.ToString()+ "_rgb.jpg,"+
                                robotTransform + "," +
                                smartCamera.GetTransformString() + "," +
                                currentRoom);
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_rgb.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.RGB).EncodeToJPG());                    
                }
                if (captureDepth)
                {
                    logImgWriter.WriteLine(index.ToString() + "_depth.png," +
                                robotTransform + "," +
                                smartCamera.GetTransformString() + "," +
                                currentRoom);
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_depth.png", smartCamera.CaptureImage(SmartCamera.ImageType.Depth).EncodeToPNG());
                }
                if (captureSemanticMask)
                {
                    logImgWriter.WriteLine(index.ToString() + "_mask.jpg," +
                                robotTransform + "," +
                                smartCamera.GetTransformString() + "," +
                                currentRoom);
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_mask.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.InstanceMask).EncodeToJPG());                   
                }
                if (captureScan)
                {
                    string data = "";
                    foreach (float d in laserScan.Scan())
                    {
                        data += "," + d.ToString("F15", CultureInfo.InvariantCulture);
                    }
                    logScanWriter.WriteLine(index.ToString() +
                                robotTransform + "," +
                                laserScan.GetTransformString() + "," +
                                currentRoom +
                                data);
                }
                if (captureLidar)
                {
                    logLidarWriter.WriteLine(index.ToString() + "_lidar.png," +
                                robotTransform + "," +
                                lidar.GetTransformString() + "," +
                                currentRoom);
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_lidar.png", lidar.Scan().EncodeToPNG());
                }

                agent.isStopped = false;
                index++;
                yield return new WaitForSeconds(timeBetweenDataCapture);
            }
        }

        #endregion
    }
}
