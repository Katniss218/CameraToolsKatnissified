using CameraToolsKatnissified.Animation;
using CameraToolsKatnissified.UI;
using System.Collections.Generic;
using UnityEngine;

namespace CameraToolsKatnissified.Cameras
{
    [DisallowMultipleComponent]
    public sealed class PathCameraController : CameraController
    {
        public static string PATHS_FILE = $"GameData/{CameraToolsManager.DIRECTORY_NAME}/paths.cfg";

        public List<CameraPath> AvailablePaths { get; private set; }
        public CameraPath CurrentPath { get; private set; }

        // GUI - the location of the path scroll view.
        public Vector2 _pathSelectScrollPos;
        bool _pathWindowVisible = false;

        Vector3 _pathRootPosition;
        Quaternion _pathRootRotation;
        Matrix4x4 _pathSpaceL2W;
        Matrix4x4 _pathSpaceW2L;

        Vector3 _accumulatedOffset;

        public PathCameraController( CameraToolsManager ctm ) : base( ctm )
        {
            OnLoad( null );
        }

        void ReloadPaths()
        {
            // DeselectKeyframe();
            CurrentPath = null;
            AvailablePaths = new List<CameraPath>();

            ConfigNode pathFileNode = ConfigNode.Load( PATHS_FILE );

            foreach( var n in pathFileNode.GetNode( "CAMERAPATHS" ).GetNodes( "CAMERAPATH" ) )
            {
                AvailablePaths.Add( CameraPath.LoadOld( n ) );
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
            Debug.Log( "[CTK] Path Camera Active" );
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
                cameraBeh.Zoom = firstFrame.zoom;
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }
        }

        private void InitializePivot()
        {
            _pathRootPosition = cameraBeh.ActiveVessel.transform.position;
            _pathRootRotation = cameraBeh.ActiveVessel.transform.rotation;
            _pathSpaceL2W = Matrix4x4.TRS( _pathRootPosition, _pathRootRotation, new Vector3( 1, 1, 1 ) );
            _pathSpaceW2L = _pathSpaceL2W.inverse;
        }

        private void UpdateRecalculatePivot()
        {

            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixPosition )
            {
                _pathRootPosition = cameraBeh.ActiveVessel.transform.position;
            }
            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixRotation )
            {
                _pathRootRotation = cameraBeh.ActiveVessel.transform.rotation;
            }
            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixPositionAndRotation )
            {
                _pathRootPosition = cameraBeh.ActiveVessel.transform.position;
                _pathRootRotation = cameraBeh.ActiveVessel.transform.rotation;
            }
            _pathSpaceL2W = Matrix4x4.TRS( _pathRootPosition, _pathRootRotation, new Vector3( 1, 1, 1 ) );
            _pathSpaceW2L = _pathSpaceL2W.inverse;
        }

        protected override void OnPlayingFixedUpdate()
        {
            if( cameraBeh.ActiveVessel != null )
            {
                UpdateRecalculatePivot();

                // free camera - follow the vessel's position and apply accumulated offset.
                // fix position - follow the vessel's position.
                // fix rotation - follow the vessel's rotation and apply accumulated offset.
                // fix position and rotation - follow the vessel's position and rotation

                if( CurrentPath.Frame == CameraPath.ReferenceFrame.Free || CurrentPath.Frame == CameraPath.ReferenceFrame.FixRotation )
                {
                    _accumulatedOffset -= cameraBeh.ActiveVessel.srf_velocity * Time.fixedDeltaTime; // vessel.rb_velocity is 0 after switching to vessel-centric

                    // After flying high enough (~2500m), the space switches to be vessel-centric. we need to catch that. The rotation doesn't change I think.
                    _pathRootPosition = cameraBeh.ActiveVessel.transform.position;
                    _pathRootPosition += _accumulatedOffset;

                    _pathSpaceL2W = Matrix4x4.TRS( _pathRootPosition, _pathRootRotation, new Vector3( 1, 1, 1 ) );
                    _pathSpaceW2L = _pathSpaceL2W.inverse;
                }

                // camTransformPath is like the trolley mounted to the camerapath.
                // the camera is then fixed to a rubberband that is connected to the trolley on the other end (rotations work the same way).
                CameraTransformation camTransformPath = CurrentPath.Evaulate( cameraBeh.TimeSinceStart );

                Vector3 pivotPositionPathSpace = _pathSpaceW2L.MultiplyPoint( this.Pivot.transform.localPosition );
                Quaternion pivotRotationPathSpace = Quaternion.Inverse( _pathRootRotation ) * this.Pivot.transform.localRotation;

                // Pivot "follows" the spline. the lower the constant, the more smooth it feels.
                // It is a lot like a B-spline in that it's completely inside the spline and doesn't pass through any of the conrol points (except the start and end).
                // whenever the frame switches to vessel-centric, it fucks itself and goes to space.
                this.Pivot.transform.localPosition = _pathSpaceL2W.MultiplyPoint( Vector3.Lerp( pivotPositionPathSpace, camTransformPath.position, CurrentPath.LerpRate * Time.fixedDeltaTime ) ); // time deltatime because we're moving the position over time.
                this.Pivot.transform.localRotation = _pathRootRotation * Quaternion.Slerp( pivotRotationPathSpace, camTransformPath.rotation, CurrentPath.LerpRate * Time.fixedDeltaTime );
                cameraBeh.Zoom = Mathf.Lerp( cameraBeh.Zoom, camTransformPath.zoom, CurrentPath.LerpRate * Time.fixedDeltaTime );

                //zoom
                //cameraBeh.ZoomFactor = Mathf.Exp( cameraBeh.Zoom ) / Mathf.Exp( 1 );
                //cameraBeh.ManualFov = 60 / cameraBeh.ZoomFactor;

                //if( cameraBeh.CurrentFov != cameraBeh.ManualFov )
                //{
                //   cameraBeh.CurrentFov = Mathf.Lerp( cameraBeh.CurrentFov, cameraBeh.ManualFov, 0.1f );
                float fov = 60 / (Mathf.Exp( cameraBeh.Zoom ) / Mathf.Exp( 1 ));
                if( cameraBeh.FlightCamera.FieldOfView != fov )
                {
                    cameraBeh.FlightCamera.SetFoV( fov );
                }
                //}
            }
        }

        protected override void OnStopPlaying()
        {
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

            Rect pSelectRect = new Rect( cameraBeh._windowRect.x - width, cameraBeh._windowRect.y + 290, width, height );
            GUI.Box( pSelectRect, string.Empty );

            GUI.BeginGroup( pSelectRect );

            Rect scrollRect = new Rect( indent, indent, scrollRectSize, scrollRectSize );
            float scrollHeight = Mathf.Max( scrollRectSize, 20 * AvailablePaths.Count );
            Rect scrollViewRect = new Rect( 0, 0, scrollRectSize - 20, scrollHeight );
            _pathSelectScrollPos = GUI.BeginScrollView( scrollRect, _pathSelectScrollPos, scrollViewRect );

            bool isAnyPathSelected = false;

            for( int i = 0; i < AvailablePaths.Count; i++ )
            {
                if( GUI.Button( new Rect( 0, i * 20, scrollRectSize - 30, 20 ), AvailablePaths[i].PathName ) )
                {
                    CurrentPath = AvailablePaths[i];
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