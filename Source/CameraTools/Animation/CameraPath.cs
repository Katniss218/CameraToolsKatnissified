using System;
using System.Collections.Generic;
using UnityEngine;

namespace CameraToolsKatnissified.Animation
{
    public class CameraPath
    {
        [Flags]
        public enum ReferenceFrame
        {
            Free = 0,
            FixPosition = 1,
            FixRotation = 2,
            FixPositionAndRotation = FixPosition | FixRotation
        }

        public string PathName { get; set; }

        public int keyframeCount => _keyframes.Count;

        public float LerpRate { get; set; } = 15;
        public float TimeScale { get; set; } = 1;

        public ReferenceFrame Frame { get; set; } = ReferenceFrame.FixPositionAndRotation;

        List<CameraKeyframe> _keyframes;

        // Internal variables for interpolation.
        Vector3Curve _pointCurve;
        QuaternionCurve _rotationCurve;
        AnimationCurve _zoomCurve;

        public CameraPath()
        {
            PathName = "New Path";

            _keyframes = new List<CameraKeyframe>();
        }

        /// <param name="node">A 'CAMERAPATH' node.</param>
        public static CameraPath LoadOld( ConfigNode node )
        {
            // This method should be able to load the pre-katnissification path.

            CameraPath newPath = new CameraPath();

            newPath.PathName = node.GetValue( "pathName" );

            // decompose keyframes for backwards compatibility.
            var points = Utils.ParseVectorList( node.GetValue( "points" ) );
            var rotations = Utils.ParseQuaternionList( node.GetValue( "rotations" ) );
            var zooms = Utils.ParseFloatList( node.GetValue( "zooms" ) );
            var times = Utils.ParseFloatList( node.GetValue( "times" ) );

            for( int i = 0; i < points.Count; i++ )
            {
                CameraKeyframe kf = new CameraKeyframe( points[i], rotations[i], zooms[i], times[i] );
                newPath._keyframes.Add( kf );
            }

            newPath.LerpRate = float.Parse( node.GetValue( "lerpRate" ) );
            newPath.TimeScale = float.Parse( node.GetValue( "timeScale" ) );
            if( node.HasValue( "frame" ) )
            {
                newPath.Frame = (ReferenceFrame)Enum.Parse( typeof( ReferenceFrame ), node.GetValue( "frame" ) );
            }

            newPath.SortAndUpdate();

            return newPath;
        }

        /// <param name="node">The node that contains the 'CAMERAPATH' node list.</param>
        public void Save( ConfigNode node )
        {
            Debug.Log( $"Saving path: {PathName}" );

            ConfigNode pathNode = node.AddNode( "CAMERAPATH" );
            pathNode.AddValue( "pathName", this.PathName );

            // decompose keyframes for backwards compatibility.
            List<Vector3> points = new List<Vector3>();
            List<Quaternion> rotations = new List<Quaternion>();
            List<float> zooms = new List<float>();
            List<float> times = new List<float>();

            foreach( var kf in _keyframes )
            {
                points.Add( kf.Position );
                rotations.Add( kf.Rotation );
                zooms.Add( kf.Zoom );
                times.Add( kf.Time );
            }

            pathNode.AddValue( "points", Utils.WriteVectorList( points ) );
            pathNode.AddValue( "rotations", Utils.WriteQuaternionList( rotations ) );
            pathNode.AddValue( "zooms", Utils.WriteFloatList( zooms ) );
            pathNode.AddValue( "times", Utils.WriteFloatList( times ) );

            pathNode.AddValue( "lerpRate", this.LerpRate );
            pathNode.AddValue( "timeScale", this.TimeScale );
            pathNode.AddValue( "frame", this.Frame );
        }

        public void AddTransform( Transform cameraTransform, float zoom, float time )
        {
            _keyframes.Add( new CameraKeyframe( cameraTransform.localPosition, cameraTransform.localRotation, zoom, time ) );

            SortAndUpdate();
        }

        public void SetTransform( CameraKeyframe keyframe, Transform cameraTransform, float zoom, float time )
        {
            keyframe.Position = cameraTransform.localPosition;
            keyframe.Rotation = cameraTransform.localRotation;
            keyframe.Zoom = zoom;
            keyframe.Time = time;

            SortAndUpdate();
        }

        /// <summary>
        /// Call this after adding a keyframe.
        /// </summary>
        void SortAndUpdate()
        {
            SortKeyframes();
            UpdateCurves();
        }

        public void RemoveKeyframe( CameraKeyframe kf )
        {
            _keyframes.Remove( kf );

            SortAndUpdate();
        }

        public void SortKeyframes()
        {
            _keyframes.Sort( new CameraKeyframeComparer() );
        }

        public CameraKeyframe GetKeyframe( int index )
        {
            return _keyframes[index];
        }

        public void UpdateCurves()
        {
            List<Vector3> points = new List<Vector3>();
            List<Quaternion> rotations = new List<Quaternion>();
            List<float> zooms = new List<float>();
            List<float> times = new List<float>();

            foreach( var kf in _keyframes )
            {
                points.Add( kf.Position );
                rotations.Add( kf.Rotation );
                zooms.Add( kf.Zoom );
                times.Add( kf.Time );
            }

            _pointCurve = new Vector3Curve( points.ToArray(), times.ToArray() );
            _rotationCurve = new QuaternionCurve( rotations.ToArray(), times.ToArray() );
            _zoomCurve = new AnimationCurve();
            for( int i = 0; i < zooms.Count; i++ )
            {
                _zoomCurve.AddKey( new Keyframe( times[i], zooms[i] ) );
            }
        }

        public CameraTransformation Evaulate( float time )
        {
            CameraTransformation tf = new CameraTransformation();
            tf.position = _pointCurve.Evaluate( time );
            tf.rotation = _rotationCurve.Evaluate( time );
            tf.zoom = _zoomCurve.Evaluate( time );

            return tf;
        }
    }
}