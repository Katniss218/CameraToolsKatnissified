using UnityEngine;
using System.Collections.Generic;

namespace CameraToolsKatnissified.Animation
{
    public class CameraKeyframe
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public float Zoom { get; set; }
        public float Time { get; set; }

        public CameraKeyframe( Vector3 position, Quaternion rotation, float zoom, float time )
        {
            Position = position;
            Rotation = rotation;
            Zoom = zoom;
            Time = time;
        }
    }

    public class CameraKeyframeComparer : IComparer<CameraKeyframe>
    {
        public int Compare( CameraKeyframe a, CameraKeyframe b )
        {
            return a.Time.CompareTo( b.Time );
        }
    }
}