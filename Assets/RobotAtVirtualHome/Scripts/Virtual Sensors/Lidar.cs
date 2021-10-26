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

        public LayerMask layerMask;

        public Action<Texture2D> OnScanTaken;
        public Texture2D ranges { get; private set; }

        #region Unity Functions
        private void Start()
        {
            ranges = new Texture2D(imageSize.x, imageSize.y, TextureFormat.R16, false);
        }
        #endregion

        #region Public Functions
        public Texture2D Scan()
        {
            Ray ray;
            for (int hPx = 0; hPx < imageSize.x; hPx++)
            {
                for (int vPx = 0; vPx < imageSize.y; vPx++)
                {
                    Quaternion angle = Quaternion.Euler(0,
                                                        -90 + hPx * (360 / (float)imageSize.x),
                                                        vPx * (fieldOfViewAngle / (float)imageSize.y) - (fieldOfViewAngle / 2));

                    ray = new Ray(transform.position, angle * transform.forward);
                    if (Physics.Raycast(ray, out RaycastHit raycastHit, maximumDistance, layerMask))
                    {                        
                        Color c = new Color(raycastHit.distance / maximumDistance, raycastHit.distance / maximumDistance, raycastHit.distance / maximumDistance, 1f);
                        ranges.SetPixel(hPx, vPx, c);
                    }
                }
            }
            ranges.Apply();
            OnScanTaken?.Invoke(ranges);
            return ranges;
        }
        #endregion

        #region Private Functions
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