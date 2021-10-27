﻿using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace RobotAtVirtualHome {

    [RequireComponent(typeof(NavMeshAgent))]

    public class AIGrid : VirtualAgent {

        [Header("Behaviour")]
        public Vector2 minRange;
        public Vector2 maxRange;
        [Range(0.1f,10)]
        public float cellSize = 0.5f;
        public bool exactRoute = false;
        public int photosPerNode = 10;

        [Header("Capture Data")]
        public bool captureRGB;
        public bool captureDepth;
        public bool captureSemanticMask;
        public bool captureScan;
        public bool captureLidar;

        public string filePath { get; private set; }
        private StreamWriter logImgWriter;
        private StreamWriter logScanWriter;
        private StreamWriter logLidarWriter;

        public string currentRoom { get; private set; }        

        private List<Vector3> grid;
        private int index = 0;

        private SmartCamera smartCamera;
        private LaserScanner laserScan;
        private Lidar lidar;

        #region Unity Functions
        private new void Awake()
        {
            base.Awake();
            smartCamera = GetComponentInChildren<SmartCamera>();
            if ((captureRGB || captureDepth || captureSemanticMask) && smartCamera == null)
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
            if (captureLidar && lidar == null)
            {
                captureLidar = false;
                Log("Lidar not found", LogLevel.Error, true);
            }
        }

        void Start() {

            if (minRange[0] >= maxRange[0] || minRange[1] >= maxRange[1]) {
                Log("Incorrect ranges",LogLevel.Error,true);
            }

            if (captureRGB || captureDepth || captureSemanticMask || captureScan) {
                filePath = FindObjectOfType<EnvironmentManager>().path;
                string tempPath = Path.Combine(filePath, "Grid");
                int i = 0;
                while (Directory.Exists(tempPath)) {
                    i++;
                    tempPath = Path.Combine(filePath, "Grid" + i);
                }

                filePath = tempPath;
                if (!Directory.Exists(filePath)) {
                    Directory.CreateDirectory(filePath);
                }

                if (captureRGB || captureDepth || captureSemanticMask)
                {
                    logImgWriter = new StreamWriter(filePath + "/LogImg.csv", true);
                    logImgWriter.WriteLine("PhotoID,XRobotPosition,YRobotPosition,ZRobotPosition,YRobotRotation,ZRobotRotation,XCameraPosition,YCameraPosition,ZCameraPosition,XCameraRotation,YCameraRotation,ZCameraRotation,Room");
                }

                if (captureScan)
                {
                    logScanWriter = new StreamWriter(filePath + "/LogScan.csv", true);
                    logScanWriter.WriteLine("ScanID,XRobotPosition,YRobotPosition,ZRobotPosition,YRobotRotation,ZRobotRotation,XLaserPosition,YLaserPosition,ZLaserPosition,XLaserRotation,YLaserRotation,ZLaserRotation,Room,Measures");
                }

                if (captureLidar)
                {
                    logLidarWriter = new StreamWriter(filePath + "/LogLidar.csv", true);
                    logLidarWriter.WriteLine("ScanID,XRobotPosition,YRobotPosition,ZRobotPosition,YRobotRotation,ZRobotRotation,XLidarPosition,YLidarPosition,ZLidarPosition,XLidarRotation,YLidarRotation,ZLidarRotation,Room");
                }

                Log("The saving path is:" + filePath,LogLevel.Normal);
            }
            state = StatusMode.Loading;
            grid = new List<Vector3>();
            StartCoroutine(CalculateGrid());           
        }

        private void Update() {
            switch (state) {
                case StatusMode.Walking:

                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit)) {
                        currentRoom = hit.transform.name;
                    }

                    if (agent.remainingDistance <= agent.stoppingDistance &&
                        agent.velocity.sqrMagnitude == 0f) {
                        state = StatusMode.Turning;
                        StartCoroutine(Capture());
                        Log("Change state to Capture",LogLevel.Developer);
                    }
                    break;
            }
        }

        private void OnDestroy() {
            if (logImgWriter != null) {
                logImgWriter.Close();
            }
            if (logScanWriter != null) {
                logScanWriter.Close();
            }
            if (logLidarWriter != null)
            {
                logLidarWriter.Close();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (Application.isPlaying && this.enabled && LogLevel >= LogLevel.Normal) {
                Gizmos.color = new Color(0, 0.9f, 1, 0.3f);
                foreach (Vector3 point in grid) {
                    Gizmos.DrawSphere(point, 0.1f);
                }
                Gizmos.color = new Color(1, 0, 0.9f, 0.8f);
                Gizmos.DrawSphere(agent.destination, 0.1f);
            }
        }
#endif

        #endregion

        #region Private Functions
        private IEnumerator CalculateGrid() {
            NavMeshPath path = new NavMeshPath();
            for (float i = minRange[0]; i <= maxRange[0]; i += cellSize) {
                for (float j = minRange[1]; j <= maxRange[1]; j += cellSize) {
                    Vector3 point = new Vector3(i, transform.position.y, j);
                    agent.CalculatePath(point, path);
                    if (exactRoute) {
                        if (path.status == NavMeshPathStatus.PathComplete && Vector3.Distance(path.corners[path.corners.Length - 1], point) < 0.04f) {
                            grid.Add(point);
                        }
                    } else {
                        if (path.status == NavMeshPathStatus.PathComplete) {
                            grid.Add(point);
                        }
                    }
                }
            }
            Debug.Log("Nodos: " + grid.Count);
            yield return new WaitForEndOfFrame();
            agent.SetDestination(grid[index]);
            agent.isStopped = false;
            yield return new WaitForEndOfFrame();
            state = StatusMode.Walking;
            Log("Start",LogLevel.Normal);

            yield return null;
        }

        private IEnumerator Capture() {
            transform.rotation = Quaternion.identity;
            yield return new WaitForEndOfFrame();

            StringBuilder line = new StringBuilder();
            for (int i = 1; i <= photosPerNode; i++)
            {
                yield return new WaitForEndOfFrame();
                
                if (captureRGB)
                {
                    File.WriteAllBytes(filePath + "/" + index.ToString()+"_"+ i.ToString() + "_rgb.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.RGB).EncodeToJPG());
                    line = new StringBuilder();
                    line.Append(index.ToString());
                    line.Append("_");
                    line.Append(i);
                    line.Append("_rgb.jpg,");
                    line.Append(((double)transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(currentRoom);
                    logImgWriter.WriteLine(line);
                }
                if (captureDepth)
                {
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_" + i.ToString() + "_depth.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.Depth).EncodeToJPG());
                    line = new StringBuilder();
                    line.Append(index.ToString());
                    line.Append("_");
                    line.Append(i);
                    line.Append("_depth.jpg,");
                    line.Append(((double)transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(currentRoom);
                    logImgWriter.WriteLine(line);
                }
                if (captureSemanticMask)
                {
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_" + i.ToString() + "_mask.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.InstanceMask).EncodeToJPG());
                    line = new StringBuilder();
                    line.Append(index.ToString());
                    line.Append("_");
                    line.Append(i);
                    line.Append("_mask.jpg,");
                    line.Append(((double)transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(((double)smartCamera.transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                    line.Append(currentRoom);
                    logImgWriter.WriteLine(line);
                }

                transform.rotation = Quaternion.Euler(0, i * (360 / photosPerNode), 0);
            }

            if (captureScan)
            {
                line = new StringBuilder();
                line.Append(index.ToString());
                line.Append("_mask.jpg,");
                line.Append(((double)transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)laserScan.transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)laserScan.transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)laserScan.transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)laserScan.transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)laserScan.transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)laserScan.transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(currentRoom);
                foreach (float d in laserScan.Scan())
                {
                    line.Append(d.ToString("F15", CultureInfo.InvariantCulture));
                    line.Append(",");
                }
                logScanWriter.WriteLine(line);
            }
            if (captureLidar)
            {
                File.WriteAllBytes(filePath + "/" + index.ToString() + "_lidar.jpg", lidar.Scan().EncodeToJPG());
                line = new StringBuilder();
                line.Append(index.ToString());
                line.Append("_lidar.jpg,");
                line.Append(((double)transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)lidar.transform.position.x).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)lidar.transform.position.y).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)lidar.transform.position.z).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)lidar.transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)lidar.transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(((double)lidar.transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture));
                line.Append(",");
                line.Append(currentRoom);
                logLidarWriter.WriteLine(line);
            }

            Log(index.ToString() + "/" + grid.Count + " - " + (index / (float)grid.Count) * 100 + "%",LogLevel.Normal);
            index++;
            if (index >= grid.Count) {
                state = StatusMode.Finished;
                Log("Finished",LogLevel.Normal);
                GetComponent<AudioSource>().Play();
            } else {
                agent.SetDestination(grid[index]);
                agent.isStopped = false;
                state = StatusMode.Walking;
                Log(grid[index].ToString(),LogLevel.Normal);
            }

            yield return null;
        }

        #endregion
    }
}