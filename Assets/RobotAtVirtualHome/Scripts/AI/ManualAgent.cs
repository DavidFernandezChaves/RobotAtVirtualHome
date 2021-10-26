using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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

            if (captureRGB || captureDepth || captureSemanticMask || captureScan) {
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
            StringBuilder line = new StringBuilder();
            while (state != StatusMode.Finished)
            {
                yield return new WaitForEndOfFrame();
                
                if (captureRGB)
                {
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_rgb.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.RGB).EncodeToJPG());
                    line = new StringBuilder();
                    line.Append(index.ToString());
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
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_depth.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.Depth).EncodeToJPG());
                    line = new StringBuilder();
                    line.Append(index.ToString());
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
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_mask.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.InstanceMask).EncodeToJPG());
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
                yield return null;
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

                index++;
                yield return new WaitForSeconds(timeBetweenDataCapture);
            }
        }
       
        #endregion
    }

}