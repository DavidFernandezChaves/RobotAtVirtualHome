using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace RobotAtVirtualHome
{

    [RequireComponent(typeof(NavMeshAgent))]

    public class AIRandom : VirtualAgent
    {

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

        public string currentRoom { get; private set; }
        public string filePath { get; private set; }

        private StreamWriter logImgWriter;
        private StreamWriter logScanWriter;
        private StreamWriter logLidarWriter;
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

        void Start()
        {
            if (captureRGB || captureDepth || captureSemanticMask || captureScan || captureLidar)
            {
                filePath = FindObjectOfType<EnvironmentManager>().path;
                string tempPath = Path.Combine(filePath, "Wandering");
                int i = 0;
                while (Directory.Exists(tempPath))
                {
                    i++;
                    tempPath = Path.Combine(filePath, "Wandering" + i);
                }

                filePath = tempPath;
                if (!Directory.Exists(filePath))
                {
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

                Log("The saving path is:" + filePath, LogLevel.Normal);


            }

            agent.SetDestination(GetNextGoal());
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

        void Update()
        {
            switch (state)
            {
                case StatusMode.Walking:
                    if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out RaycastHit hit))
                    {
                        currentRoom = hit.transform.name;
                    }

                    if (agent.remainingDistance <= agent.stoppingDistance &&
                        agent.velocity.sqrMagnitude == 0f)
                    {                        
                        agent.SetDestination(GetNextGoal());
                        agent.isStopped = false;
                        state = StatusMode.Loading;
                        StartCoroutine(DoOnGoal());
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && this.enabled && LogLevel >= LogLevel.Normal)
            {
                Gizmos.color = new Color(1, 0, 0.9f, 0.8f);
                Gizmos.DrawSphere(agent.destination, 0.1f);
            }
        }
#endif

        #endregion


        #region Private Functions
        private IEnumerator DoOnGoal()
        {
            yield return new WaitForSeconds(1);
            state = StatusMode.Walking;
            agent.isStopped = false;
        }

        private Vector3 GetNextGoal()
        {
            Vector3 result = new Vector3(UnityEngine.Random.Range(-100f,100f),0.1f, UnityEngine.Random.Range(-100f, 100f));
            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(result, path);
            while (path.status != NavMeshPathStatus.PathComplete)
            {
                result = new Vector3(UnityEngine.Random.Range(-100f, 100f), 0.1f, UnityEngine.Random.Range(-100f, 100f));
                agent.CalculatePath(result, path);
            }
            return result;
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
                    logImgWriter.WriteLine(index.ToString() + "_rgb.jpg," +
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
