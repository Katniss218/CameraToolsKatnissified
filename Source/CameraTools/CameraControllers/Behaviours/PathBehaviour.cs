using CameraToolsKatnissified.Animation;
using CameraToolsKatnissified.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CameraToolsKatnissified.CameraControllers.Behaviours
{
    public sealed class PathBehaviour : CameraBehaviour
    {
        [Flags]
        public enum FrameOfReferenceFrameUpdateMode
        {
            /// Doesn't update the reference frame.
            None = 0,
            /// Updates position every frame.
            Position = 1,
            /// Updates rotation every frame.
            Rotation = 2,
            /// Updates position and rotation every frame.
            PositionAndRotation = 3
        }

        public FrameOfReferenceFrameUpdateMode ReferenceFrameUpdateMode { get; set; }

        public CameraPath CurrentPath { get; private set; }

        /// If true, applies position using path.
        public bool UsePosition { get; set; }
        /// If true, applies rotation using path.
        public bool UseRotation { get; set; }
        /// If true, applies zoom using path.
        public bool UseZoom { get; set; }


        List<CameraPath> _availablePaths;

        // GUI - the location of the path scroll view.
        public Vector2 _pathSelectScrollPos;
        bool _pathWindowVisible = false;

        Vector3 _pathRootPosition;
        Quaternion _pathRootRotation;
        Matrix4x4 _pathSpaceL2W;
        Matrix4x4 _pathSpaceW2L;

        Vector3 _accumulatedOffset;

        float _startCameraTimestamp;
        float TimeSinceStart
        {
            get
            {
                return Time.time - _startCameraTimestamp;
            }
        }

        public PathBehaviour( CameraPlayerController controller ) : base( controller )
        {
            OnLoad( null );
        }

        void ReloadPaths()
        {
            // DeselectKeyframe();
            CurrentPath = null;
            _availablePaths = new List<CameraPath>();

            ConfigNode pathFileNode = ConfigNode.Load( CameraToolsManager.PATHS_FILE );

            foreach( var n in pathFileNode.GetNode( "CAMERAPATHS" ).GetNodes( "CAMERAPATH" ) )
            {
                _availablePaths.Add( CameraPath.LoadOld( n ) );
            }
        }

        public override void OnLoad( ConfigNode node )
        {
            base.OnLoad( node );

            ReloadPaths();
        }

        public override void OnSave( ConfigNode node )
        {
        }

        protected override void OnStartPlaying()
        {
            Debug.Log( $"Started playing {nameof( PathBehaviour )}" );

            _startCameraTimestamp = Time.time;
            if( FlightGlobals.ActiveVessel != null )
            {
                if( CurrentPath == null || CurrentPath.keyframeCount <= 0 )
                {
                    Debug.LogWarning( "[CameraToolsKatnissified] Path Camera didn't find current path" );
                    return;
                }

                // initialize the rotation on start, but don't update it so if the rocket rolls, the camera won't follow it.
                InitializePivot();

                CameraTransformation firstFrame = CurrentPath.Evaulate( 0 );
                this.Pivot.transform.position = _pathSpaceL2W.MultiplyPoint( firstFrame.position );
                this.Pivot.transform.rotation = _pathRootRotation * firstFrame.rotation;
                Controller.Zoom = firstFrame.zoom;
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }
        }

        protected override void OnStopPlaying()
        {
        }

        private void InitializePivot()
        {
            _pathRootPosition = Ctm.ActiveVessel.transform.position;
            _pathRootRotation = Ctm.ActiveVessel.transform.rotation;
            _pathSpaceL2W = Matrix4x4.TRS( _pathRootPosition, _pathRootRotation, new Vector3( 1, 1, 1 ) );
            _pathSpaceW2L = _pathSpaceL2W.inverse;
        }

        private void UpdateRecalculatePivot()
        {

            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixPosition )
            {
                _pathRootPosition = Ctm.ActiveVessel.transform.position;
            }
            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixRotation )
            {
                _pathRootRotation = Ctm.ActiveVessel.transform.rotation;
            }
            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixPositionAndRotation )
            {
                _pathRootPosition = Ctm.ActiveVessel.transform.position;
                _pathRootRotation = Ctm.ActiveVessel.transform.rotation;
            }
            _pathSpaceL2W = Matrix4x4.TRS( _pathRootPosition, _pathRootRotation, new Vector3( 1, 1, 1 ) );
            _pathSpaceW2L = _pathSpaceL2W.inverse;
        }

        public override void FixedUpdate( bool isPlaying )
        {
            if( !isPlaying )
            {
                return;
            }

            if( Ctm.ActiveVessel != null )
            {
                UpdateRecalculatePivot();

                // free camera - follow the vessel's position and apply accumulated offset.
                // fix position - follow the vessel's position.
                // fix rotation - follow the vessel's rotation and apply accumulated offset.
                // fix position and rotation - follow the vessel's position and rotation

                if( CurrentPath.Frame == CameraPath.ReferenceFrame.Free || CurrentPath.Frame == CameraPath.ReferenceFrame.FixRotation )
                {
                    _accumulatedOffset -= Ctm.ActiveVessel.srf_velocity * Time.fixedDeltaTime; // vessel.rb_velocity is 0 after switching to vessel-centric

                    // After flying high enough (~2500m), the space switches to be vessel-centric. we need to catch that. The rotation doesn't change I think.
                    _pathRootPosition = Ctm.ActiveVessel.transform.position;
                    _pathRootPosition += _accumulatedOffset;

                    _pathSpaceL2W = Matrix4x4.TRS( _pathRootPosition, _pathRootRotation, new Vector3( 1, 1, 1 ) );
                    _pathSpaceW2L = _pathSpaceL2W.inverse;
                }

                // camTransformPath is like the trolley mounted to the camerapath.
                // the camera is then fixed to a rubberband that is connected to the trolley on the other end (rotations work the same way).
                CameraTransformation camTransformPath = CurrentPath.Evaulate( TimeSinceStart );

                Vector3 pivotPositionPathSpace = _pathSpaceW2L.MultiplyPoint( this.Pivot.localPosition );
                Quaternion pivotRotationPathSpace = Quaternion.Inverse( _pathRootRotation ) * this.Pivot.localRotation;

                // Pivot "follows" the spline. the lower the constant, the more smooth it feels.
                // It is a lot like a B-spline in that it's completely inside the spline and doesn't pass through any of the conrol points (except the start and end).
                // whenever the frame switches to vessel-centric, it fucks itself and goes to space.
                this.Pivot.localPosition = _pathSpaceL2W.MultiplyPoint( Vector3.Lerp( pivotPositionPathSpace, camTransformPath.position, CurrentPath.LerpRate * Time.fixedDeltaTime ) ); // time deltatime because we're moving the position over time.
                this.Pivot.localRotation = _pathRootRotation * Quaternion.Slerp( pivotRotationPathSpace, camTransformPath.rotation, CurrentPath.LerpRate * Time.fixedDeltaTime );
                Controller.Zoom = Mathf.Lerp( Controller.Zoom, camTransformPath.zoom, CurrentPath.LerpRate * Time.fixedDeltaTime );

                //zoom
                //cameraBeh.ZoomFactor = Mathf.Exp( cameraBeh.Zoom ) / Mathf.Exp( 1 );
                //cameraBeh.ManualFov = 60 / cameraBeh.ZoomFactor;

                //if( cameraBeh.CurrentFov != cameraBeh.ManualFov )
                //{
                //   cameraBeh.CurrentFov = Mathf.Lerp( cameraBeh.CurrentFov, cameraBeh.ManualFov, 0.1f );
                //float fov = 60 / (Mathf.Exp( Controller.Zoom ) / Mathf.Exp( 1 ));
                //if( Ctm.FlightCamera.FieldOfView != fov )
                //{
                //    Ctm.FlightCamera.SetFoV( fov );
                //}
                //}
            }
        }

        void TogglePathList()
        {
            _pathWindowVisible = !_pathWindowVisible;
        }

        public override void OnGUI()
        {
            if( CameraToolsManager._guiWindowVisible && CameraToolsManager._uiVisible )
            {
                if( _pathWindowVisible )
                {
                    DrawPathSelectorWindow();
                }
            }
        }

        public override void DrawGui( UILayout UILayout, ref int line )
        {
            if( CurrentPath != null )
            {
                GUI.Label( UILayout.GetRectX( line, 1, 2 ), "Path:" );
                CurrentPath.PathName = GUI.TextField( UILayout.GetRectX( line, 3, 11 ), CurrentPath.PathName );
            }
            else
            {
                GUI.Label( UILayout.GetRectX( line, 1, 11 ), "Path: None" );
            }
            line++;

            if( GUI.Button( UILayout.GetRectX( line, 1, 11 ), "Open Path" ) )
            {
                ReloadPaths();
                TogglePathList();
            }
        }

        public void DrawPathSelectorWindow()
        {
            float width = 300;
            float height = 300;
            float indent = 5;
            float scrollRectSize = width - indent - indent;

            Rect pSelectRect = new Rect( Ctm._windowRect.x - width, Ctm._windowRect.y + 290, width, height );
            GUI.Box( pSelectRect, string.Empty );

            GUI.BeginGroup( pSelectRect );

            Rect scrollRect = new Rect( indent, indent, scrollRectSize, scrollRectSize );
            float scrollHeight = Mathf.Max( scrollRectSize, 20 * _availablePaths.Count );
            Rect scrollViewRect = new Rect( 0, 0, scrollRectSize - 20, scrollHeight );
            _pathSelectScrollPos = GUI.BeginScrollView( scrollRect, _pathSelectScrollPos, scrollViewRect );

            bool isAnyPathSelected = false;

            for( int i = 0; i < _availablePaths.Count; i++ )
            {
                if( GUI.Button( new Rect( 0, i * 20, scrollRectSize - 30, 20 ), _availablePaths[i].PathName ) )
                {
                    CurrentPath = _availablePaths[i];
                    isAnyPathSelected = true;
                }
            }

            GUI.EndScrollView();

            GUI.EndGroup();
            if( isAnyPathSelected )
            {
                _pathWindowVisible = false;
            }
        }
    }
}