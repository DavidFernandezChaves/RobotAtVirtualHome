using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace RobotAtVirtualHome {
    public class ManualAgent : VirtualAgent {

        public float mainSpeed = 1f;        
        public float angularSpeed = 1f;
        public float factorSpeed = 2f;

        public float frequencyCapture;
        public bool captureRGB;
        public bool captureDepth;
        public bool captureSemanticMask;

        public string currentRoom { get; private set; }

        private int index2 = 0;

        // Start is called before the first frame update
        void Start() {
            if (TryGetComponent<NavMeshAgent>(out NavMeshAgent navMesh))
                navMesh.enabled = false;

            if (record) {
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

                Log("The saving path is:" + filePath);
                writer = new StreamWriter(filePath + "/Info.csv", true);
                writer.WriteLine("photoID;robotPosition;robotRotation;cameraPosition;cameraRotation;room");
                StartCoroutine("Record");
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
                transform.Rotate(0, newangle, 0);
            }
            if (Input.GetKey(KeyCode.E)) {
                transform.Rotate(0, -newangle, 0);
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

        private IEnumerator Record() {
            while (state != StatusMode.Finished) {

                yield return new WaitForEndOfFrame();
                agent.isStopped = true;
                yield return new WaitForSeconds(0.1f);
                byte[] itemBGBytes;
                if (captureSemanticMask) {
                    writer.WriteLine(index2.ToString() + "_mask.png;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                        + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    itemBGBytes = smartCamera.ImageMask.EncodeToPNG();
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