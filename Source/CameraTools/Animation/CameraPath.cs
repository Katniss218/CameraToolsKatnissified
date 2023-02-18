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

        public float LerpRate { get; set; } = 2.0f;
        public float TimeScale { get; set; } = 1.0f;

        public ReferenceFrame Frame { get; set; } = ReferenceFrame.FixPositionAndRotation;

        List<CameraKeyframe> _keyframes;

        // Internal variables for interpolation.
        Curve<Vector3> _pointCurve;
        Curve<Quaternion> _rotationCurve;
        Curve<float> _zoomCurve;

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

            _pointCurve = new Curve<Vector3>( points.ToArray(), times.ToArray() );
            _rotationCurve = new Curve<Quaternion>( rotations.ToArray(), times.ToArray() );
            _zoomCurve = new Curve<float>( zooms.ToArray(), times.ToArray() );
        }

        public CameraTransformation Evaulate( float time )
        {
            const float TimeOffset = 0.5f;
            float scaledTime = (time) * TimeScale;
            float scaledTime1 = (time - TimeOffset) * TimeScale;
            float scaledTime2 = (time + TimeOffset) * TimeScale;

            /*CameraTransformation tf = new CameraTransformation();
            Vector3 pos1 = _pointCurve.EvaluateLinearBezier( Vector3.Lerp, scaledTime1 );
            Vector3 pos2 = _pointCurve.EvaluateLinearBezier( Vector3.Lerp, scaledTime2 );
            tf.position = Vector3.Lerp( pos1, pos2, 0.5f );

            Quaternion rot1 = _rotationCurve.EvaluateLinearBezier( Quaternion.Slerp, scaledTime1 );
            Quaternion rot2 = _rotationCurve.EvaluateLinearBezier( Quaternion.Slerp, scaledTime2 );
            tf.rotation = Quaternion.Slerp( rot1, rot2, 0.5f );

            float zoom1 = _zoomCurve.EvaluateLinearBezier( Mathf.Lerp, scaledTime1 );
            float zoom2 = _zoomCurve.EvaluateLinearBezier( Mathf.Lerp, scaledTime2 );
            tf.zoom = Mathf.Lerp( zoom1, zoom2, 0.5f );*/

            CameraTransformation tf = new CameraTransformation();
            tf.position = _pointCurve.EvaluateLinearBezier( Vector3.Lerp, scaledTime );

            tf.rotation = _rotationCurve.EvaluateLinearBezier( Quaternion.Slerp, scaledTime );

            tf.zoom = _zoomCurve.EvaluateLinearBezier( Mathf.Lerp, scaledTime );

            return tf;
        }
    }
}