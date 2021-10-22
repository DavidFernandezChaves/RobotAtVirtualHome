using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public string currentRoom { get; private set; }
        public string filePath { get; private set; }

        private StreamWriter logImgWriter;
        private StreamWriter logScanWriter;
        private int index = 0;

        #region Unity Functions
        void Start()
        {
            if (captureRGB || captureDepth || captureSemanticMask || captureScan)
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
                    logImgWriter.WriteLine("photoID;robotPosition;robotRotation;cameraPosition;cameraRotation;room");
                }

                if (captureScan)
                {
                    logScanWriter = new StreamWriter(filePath + "/LogScan.csv", true);
                    logScanWriter.WriteLine("scanID;robotPosition;robotRotation;data");
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

                yield return new WaitForEndOfFrame();

                agent.isStopped = true;
                if (captureRGB)
                {
                    logImgWriter.WriteLine(index.ToString() + "_rgb.jpg;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                            + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_rgb.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.RGB).EncodeToJPG());
                }
                yield return null;
                if (captureDepth)
                {
                    logImgWriter.WriteLine(index.ToString() + "_depth.jpg;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                    + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_depth.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.Depth).EncodeToJPG());
                }
                yield return null;
                if (captureSemanticMask)
                {
                    logImgWriter.WriteLine(index.ToString() + "_mask.jpg;" + transform.position + ";" + transform.rotation.eulerAngles + ";"
                    + smartCamera.transform.position + ";" + smartCamera.transform.rotation.eulerAngles + ";" + currentRoom);
                    File.WriteAllBytes(filePath + "/" + index.ToString() + "_mask.jpg", smartCamera.CaptureImage(SmartCamera.ImageType.InstanceMask).EncodeToJPG());
                }
                if (captureScan)
                {
                    string data = "";
                    foreach (float d in laserScan.Scan())
                    {
                        data += d.ToString() + ";";
                    }
                    logScanWriter.WriteLine(index.ToString() + transform.position + ";" + transform.rotation.eulerAngles + ";" + data);
                }

                agent.isStopped = false;
                index++;
                yield return new WaitForSeconds(timeBetweenDataCapture);
            }
        }

        #endregion
    }
}
