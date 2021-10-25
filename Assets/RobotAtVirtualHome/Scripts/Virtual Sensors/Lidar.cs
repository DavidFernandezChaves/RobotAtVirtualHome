using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace RobotAtVirtualHome
{
    public class Lidar : MonoBehaviour
    {
        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;

        [Tooltip("Size of images to be captured")]
        public Vector2Int imageSize;

        [Tooltip("The angle above and below the horizontal will be half of the specified angle")]
        public float fieldOfViewAngle;

        [Tooltip("Maximum detection distance")]
        public float maximumDistance = 100000;

        [Tooltip("Scanning frequency in Hz")]
        [Range(1,100)]
        public float frecuency=10;

        public bool saveData;

        public LayerMask layerMask;

        public Action<Texture2D> OnScanTaken;
        public Texture2D ranges { get; private set; }

        public string filePath { get; private set; }

        //private int layerMask;
        private int index = 0;        

        #region Unity Functions
        private void Start()
        {
            ranges = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);
            if (saveData)
            {
                filePath = FindObjectOfType<EnvironmentManager>().path;
                string tempPath = Path.Combine(filePath, "Lidar");
                int i = 0;
                while (Directory.Exists(tempPath))
                {
                    i++;
                    tempPath = Path.Combine(filePath, "Lidar" + i);
                }

                filePath = tempPath;
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
            }
            StartCoroutine(Scan());
        }
        #endregion

        #region Private Functions

        private IEnumerator Scan()
        {
            Ray ray;
            Log("Scanning in "+filePath, LogLevel.Developer);
            while (isActiveAndEnabled)
            {
                for (int hPx= 0; hPx < imageSize.x; hPx++)
                {
                    for (int vPx = 0; vPx < imageSize.y; vPx++)
                    {
                        Quaternion angle = Quaternion.Euler(0, 
                                                            -90 + hPx*(360/ (float)imageSize.x),
                                                            vPx * (fieldOfViewAngle / (float)imageSize.y) - (fieldOfViewAngle / 2));

                        ray = new Ray(transform.position,  angle * transform.forward);
                        if (Physics.Raycast(ray, out RaycastHit raycastHit, maximumDistance, layerMask))
                        {
                            Color c = new Color(1f, 1f, 1f, raycastHit.distance/ (maximumDistance));
                            ranges.SetPixel(hPx, vPx, c);                            
                        }
                    }
                    if (hPx % (imageSize.x/5) == 0) {
                        Log(hPx.ToString() + "/"+imageSize.x.ToString(), LogLevel.Developer);
                        yield return new WaitForEndOfFrame();
                    }
                }

                ranges.Apply();

                if (saveData)
                {
                    File.WriteAllBytes(filePath + "/scan " + index.ToString() + ".png", ranges.EncodeToPNG());
                    index++;
                }

                OnScanTaken?.Invoke(ranges);
                yield return new WaitForSeconds(1/frecuency);
            }
        }

        private void Log(string _msg, LogLevel lvl, bool Warning = false)
        {
            if (LogLevel <= lvl && LogLevel != LogLevel.Nothing)
            {
                if (Warning)
                {
                    Debug.LogWarning("[Lidar]: " + _msg);
                }
                else
                {
                    Debug.Log("[Lidar]: " + _msg);
                }
            }
        }
        #endregion

    }
}