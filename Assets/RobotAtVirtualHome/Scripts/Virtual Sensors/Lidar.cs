using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

        [Tooltip("Upper vertical viewing angle with respect to the horizontal.")]
        [Range(0,180)]
        public float upperViewingAngle;
        [Tooltip("Bottom vertical viewing angle with respect to the horizontal.")]
        [Range(0, -180)]
        public float bottomViewingAngle;

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
            ranges = new Texture2D(imageSize.x, imageSize.y, TextureFormat.R16, false);

            float distance;
            Quaternion angle;
            for (int hPx = 0; hPx < imageSize.x; hPx++)
            {
                for (int vPx = 0; vPx < imageSize.y; vPx++)
                {

                    angle = Quaternion.AngleAxis(-90f + hPx * (360f / imageSize.x), transform.up) 
                        * Quaternion.AngleAxis(-vPx * ((upperViewingAngle - bottomViewingAngle) / imageSize.y) - bottomViewingAngle, transform.right);

                    if (Physics.Raycast(transform.position, angle * transform.forward, out RaycastHit raycastHit, maximumDistance, layerMask))
                    {
                        distance = raycastHit.distance / maximumDistance;
                        ranges.SetPixel(hPx, vPx, new Color(distance, distance, distance, 1f));
                    }
                }
            }
            ranges.Apply();
            OnScanTaken?.Invoke(ranges);
            return ranges;
        }

        public string GetTransformString()
        {
            return ((double)transform.position.x).ToString("F15", CultureInfo.InvariantCulture) + "," +
                    ((double)transform.position.y).ToString("F15", CultureInfo.InvariantCulture) + "," +
                    ((double)transform.position.z).ToString("F15", CultureInfo.InvariantCulture) + "," +
                    ((double)transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture) + "," +
                    ((double)transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture) + "," +
                    ((double)transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture);
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