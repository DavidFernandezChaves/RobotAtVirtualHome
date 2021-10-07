using System.Collections;
using System.Collections.Generic;
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
        [Range(0.1f, 10)]
        public float frequencyCapture;
        public bool captureRGB;
        public bool captureDepth;
        public bool captureSemanticMask;
        public bool captureScan;

        public string currentRoom { get; private set; }
        public string filePath { get; private set; }
        private StreamWriter logImgWriter;
        private StreamWriter logScanWriter;

        private int index = 0;

        // Start is called before the first frame update
        void Start() {
            if (TryGetComponent<NavMeshAgent>(out NavMeshAgent navMesh))
                navMesh.enabled = false;

            if (captureRGB || captureDepth || captureSemanticMask || captureScan) {
                filePath = FindObjectOfType<EnvironmentManager>().m_simulationOptions.path;
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

                if (captureRGB || captureDepth || captureSemanticMask) {
                    logImgWriter = new StreamWriter(filePath + "/LogImg.csv", true);
                    logImgWriter.WriteLine("photoID;robotPosition;robotRotation;cameraPosition;cameraRotation;room");
                }

                if (captureScan) {
                    logScanWriter = new StreamWriter(filePath + "/LogScan.csv", true);
                    logScanWriter.WriteLine("scanID;robotPosition;robotRotation;data");
                }

                Log("The saving path is:" + filePath,LogLevel.Normal);
                StartCoroutine(Capture());
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
        }

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

        private IEnumerator Capture() {
            while (state != StatusMode.Finished) {

                yield return new WaitForEndOfFrame();
                agent.isStopped = true;
                yield return new WaitForSeconds(0.1f);
                byte[] itemBGBytes;
                if (captureSemanticMask) {
                    logImgWriter.WriteLine(index.ToString() + "_mask.png;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                        + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    itemBGBytes = smartCamera.ImageMask.EncodeToPNG();
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_mask.png", itemBGBytes);
                }

                if (captureRGB) {
                    logImgWriter.WriteLine(index.ToString() + "_rgb.png;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                        + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    itemBGBytes = smartCamera.ImageRGB.EncodeToPNG();
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_rgb.png", itemBGBytes);

                }

                if (captureDepth) {
                    logImgWriter.WriteLine(index.ToString() + "_depth.png;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                        + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    itemBGBytes = smartCamera.ImageDepth.EncodeToPNG();
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_depth.png", itemBGBytes);
                }

                if (captureScan) {
                    string data = "";
                    foreach (float d in laserScan.ranges) {
                        data += d.ToString() + ";";
                    }
                    logScanWriter.WriteLine(index.ToString() + transform.position + ";" + transform.rotation.eulerAngles + ";" + data);
                }

                agent.isStopped = false;
                index++;
                if (frequencyCapture != 0) {
                    yield return new WaitForSeconds(frequencyCapture);
                }

            }
        }

        #endregion
    }

}