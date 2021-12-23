using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace RobotAtVirtualHome {
    public class ManualAgent : VirtualAgent {

        [Header("Behaviour")]
        [Range(0.1f, 10)]
        public float mainSpeed = 0.5f;
        [Range(0.1f, 10)]
        public float angularSpeed = 0.5f;
        [Range(1f, 3)]
        public float factorSpeed = 2f;

        [Header("Capture Data")]
        [Tooltip("Time between data capture")]
        [Range(0.1f, 10)]
        public float timeBetweenDataCapture=0.5f;
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

        // Start is called before the first frame update
        void Start() {
            if (TryGetComponent<NavMeshAgent>(out NavMeshAgent navMesh))
                navMesh.enabled = false;

            if (captureRGB || captureDepth || captureSemanticMask || captureScan || captureLidar) {
                filePath = FindObjectOfType<EnvironmentManager>().path;
                string tempPath = Path.Combine(filePath, "ManualAgent");
                int i = 0;
                while (Directory.Exists(tempPath)) {
                    i++;
                    tempPath = Path.Combine(filePath, "ManualAgent" + i);
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
                    var lidarConfigWriter = new StreamWriter(filePath + "/LidarCfg.txt", true);
                    lidarConfigWriter.WriteLine("Resolution (width, height) [px], Vertical FOV (upper, lower) [º], Max distance [m]");
                    lidarConfigWriter.WriteLine(lidar.imageSize[0] + ", " + lidar.imageSize[1] + ", " + lidar.upperViewingAngle + ", " + lidar.bottomViewingAngle + ", " + lidar.maximumDistance);
                    lidarConfigWriter.Close();
                    logLidarWriter = new StreamWriter(filePath + "/LogLidar.csv", true);
                    logLidarWriter.WriteLine("ScanID,XRobotPosition,YRobotPosition,ZRobotPosition,YRobotRotation,ZRobotRotation,XLidarPosition,YLidarPosition,ZLidarPosition,XLidarRotation,YLidarRotation,ZLidarRotation,Room");
                }

                Log("The saving path is:" + filePath,LogLevel.Normal);
                StartCoroutine(CaptureData());
            }
        }

        // Update is called once per frame
        void Update() {
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out RaycastHit hit)) {
                currentRoom = hit.transform.name;
            }

            Vector3 p = getDirection();
            p = p * mainSpeed * Time.deltaTime;

            float newangle = angularSpeed;

            if (Input.GetKey(KeyCode.LeftShift)) {
                p *= factorSpeed;
                newangle *= factorSpeed;
            }

            transform.Translate(p);            

            if (Input.GetKey(KeyCode.Q)) {
                transform.Rotate(0, -newangle, 0);
            }
            if (Input.GetKey(KeyCode.E)) {
                transform.Rotate(0, newangle, 0);
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

        #endregion

        #region Private Functions
        private Vector3 getDirection() {
            Vector3 p_Velocity = new Vector3();
            if (Input.GetKey(KeyCode.W)) {
                p_Velocity += new Vector3(0, 0, 1);
            }
            if (Input.GetKey(KeyCode.S)) {
                p_Velocity += new Vector3(0, 0, -1);
            }
            if (Input.GetKey(KeyCode.A)) {
                p_Velocity += new Vector3(-1, 0, 0);
            }
            if (Input.GetKey(KeyCode.D)) {
                p_Velocity += new Vector3(1, 0, 0);
            }
            return p_Velocity;
        }

        private IEnumerator CaptureData()
        {
            while (state != StatusMode.Finished)
            {
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

                index++;
                yield return new WaitForSeconds(timeBetweenDataCapture);
            }
        }
       
        #endregion
    }

}