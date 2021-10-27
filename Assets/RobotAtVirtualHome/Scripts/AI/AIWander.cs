using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
            StringBuilder line = new StringBuilder();
            while (state != StatusMode.Finished)
            {

                yield return new WaitForEndOfFrame();
                
                agent.isStopped = true;
                if (captureRGB)
                {
                    File.WriteAllBytes(filePath + "/" + index2.ToString() + "_rgb.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.RGB).EncodeToJPG());
                    line = new StringBuilder();
                    line.Append(index2.ToString());
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
                yield return null;
                if (captureDepth)
                {
                    File.WriteAllBytes(filePath + "/" + index2.ToString() + "_depth.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.Depth).EncodeToJPG());
                    line = new StringBuilder();
                    line.Append(index2.ToString());
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
                yield return null;
                if (captureSemanticMask)
                {
                    File.WriteAllBytes(filePath + "/" + index2.ToString() + "_mask.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.InstanceMask).EncodeToJPG());
                    line = new StringBuilder();
                    line.Append(index2.ToString());
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
                yield return null;
                if (captureScan)
                {                   
                    line = new StringBuilder();
                    line.Append(index2.ToString());
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
                yield return null;
                if (captureLidar)
                {
                    File.WriteAllBytes(filePath + "/" + index2.ToString() + "_lidar.jpg", lidar.Scan().EncodeToJPG());
                    line = new StringBuilder();
                    line.Append(index2.ToString());
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

                agent.isStopped = false;
                index2++;
                yield return new WaitForSeconds(timeBetweenDataCapture);
            }
        }

        #endregion
    }
}
