using CameraToolsKatnissified.Animation;
using System.Collections.Generic;
using UnityEngine;

namespace CameraToolsKatnissified.Cameras
{
    public sealed class PathCameraBehaviour : CameraBehaviour
    {
        /// <summary>
        /// If true, the camera is currently playing out a path, instead of setting it up in free mode.
        /// </summary>
        public bool IsPlayingPath { get; private set; } = false;

        public CameraPath CurrentCameraPath { get; private set; }
        public List<CameraPath> AvailableCameraPaths { get; private set; }

        public CameraKeyframe currentKeyframe { get; private set; }

        public void TemporaryResetBeforeLoad()
        {
#warning TODO - move the load logic itself to here so we don't have to reset it from a different class.
            CurrentCameraPath = null;
            AvailableCameraPaths = new List<CameraPath>();
        }

        protected override void OnStartPlaying()
        {
            Debug.Log( "[CTK] Path Camera Active" );
            if( FlightGlobals.ActiveVessel != null )
            {
                cameraBeh.CameraPivot.transform.position = cameraBeh.ActiveVessel.transform.position + cameraBeh.ActiveVessel.rb_velocity * Time.fixedDeltaTime;
                cameraBeh.CameraPivot.transform.rotation = cameraBeh.ActiveVessel.transform.rotation;

                cameraBeh.FlightCamera.SetTargetNone();
                cameraBeh.FlightCamera.transform.parent = cameraBeh.CameraPivot.transform;
                cameraBeh.FlightCamera.DeactivateUpdate();
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }
        }

        public void StartPlayingPath()
        {
            Debug.Log( "Path Camera Now Playing Path" );
            if( CurrentCameraPath == null || CurrentCameraPath.keyframeCount <= 0 )
            {
                cameraBeh.EndCamera();
                return;
            }

            DeselectKeyframe();
            OnStartPlaying();

            CameraTransformation firstFrame = CurrentCameraPath.Evaulate( 0 );
            cameraBeh.FlightCamera.transform.localPosition = firstFrame.position;
            cameraBeh.FlightCamera.transform.localRotation = firstFrame.rotation;
            cameraBeh.Zoom = firstFrame.zoom;

            IsPlaying = true;
            IsPlayingPath = true;

            // initialize the rotation on start, but don't update it so if the rocket rolls, the camera won't follow it.
            cameraBeh.CameraPivot.transform.rotation = cameraBeh.ActiveVessel.transform.rotation;
        }

        protected override void OnPlaying()
        {
            // Update the frame of reference's position to follow the vessel.
            cameraBeh.CameraPivot.transform.position = cameraBeh.ActiveVessel.transform.position + cameraBeh.ActiveVessel.rb_velocity * Time.fixedDeltaTime;
            //_stationaryCameraParent.transform.rotation = _activeVessel.transform.rotation; // here to follow rotation.

            if( IsPlayingPath )
            {
                CameraTransformation tf = CurrentCameraPath.Evaulate( cameraBeh.TimeSinceStart * CurrentCameraPath.TimeScale );
                cameraBeh.FlightCamera.transform.localPosition = Vector3.Lerp( cameraBeh.FlightCamera.transform.localPosition, tf.position, CurrentCameraPath.LerpRate * Time.fixedDeltaTime );
                cameraBeh.FlightCamera.transform.localRotation = Quaternion.Slerp( cameraBeh.FlightCamera.transform.localRotation, tf.rotation, CurrentCameraPath.LerpRate * Time.fixedDeltaTime );
                cameraBeh.Zoom = Mathf.Lerp( cameraBeh.Zoom, tf.zoom, CurrentCameraPath.LerpRate * Time.fixedDeltaTime );
            }
            else // this is to set up the path.
            {
                //mouse panning, moving
                Vector3 forwardLevelAxis = cameraBeh.FlightCamera.transform.forward;

                if( Input.GetKey( KeyCode.Mouse1 ) && Input.GetKey( KeyCode.Mouse2 ) )
                {
                    cameraBeh.FlightCamera.transform.rotation = Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * -1.7f, cameraBeh.FlightCamera.transform.forward ) * cameraBeh.FlightCamera.transform.rotation;
                }
                else
                {
                    if( Input.GetKey( KeyCode.Mouse1 ) )
                    {
                        cameraBeh.FlightCamera.transform.rotation *= Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * 1.7f / (cameraBeh.Zoom * cameraBeh.Zoom), Vector3.up );
                        cameraBeh.FlightCamera.transform.rotation *= Quaternion.AngleAxis( -Input.GetAxis( "Mouse Y" ) * 1.7f / (cameraBeh.Zoom * cameraBeh.Zoom), Vector3.right );
                        cameraBeh.FlightCamera.transform.rotation = Quaternion.LookRotation( cameraBeh.FlightCamera.transform.forward, cameraBeh.FlightCamera.transform.up );
                    }
                    if( Input.GetKey( KeyCode.Mouse2 ) )
                    {
                        cameraBeh.FlightCamera.transform.position += cameraBeh.FlightCamera.transform.right * Input.GetAxis( "Mouse X" ) * 2;
                        cameraBeh.FlightCamera.transform.position += forwardLevelAxis * Input.GetAxis( "Mouse Y" ) * 2;
                    }
                }
                cameraBeh.FlightCamera.transform.position += cameraBeh.FlightCamera.transform.up * 10 * Input.GetAxis( "Mouse ScrollWheel" );

            }

            //zoom
            cameraBeh.ZoomFactor = Mathf.Exp( cameraBeh.Zoom ) / Mathf.Exp( 1 );
            cameraBeh.ManualFov = 60 / cameraBeh.ZoomFactor;

            if( cameraBeh.CurrentFov != cameraBeh.ManualFov )
            {
                cameraBeh.CurrentFov = Mathf.Lerp( cameraBeh.CurrentFov, cameraBeh.ManualFov, 0.1f );
                cameraBeh.FlightCamera.SetFoV( cameraBeh.CurrentFov );
            }
        }

        protected override void OnStopPlaying()
        {
            IsPlayingPath = false;
            DeselectKeyframe();
        }

        public void CreateNewPath()
        {
            cameraBeh.PathKeyframeWindowVisible = false;
            CameraPath path = new CameraPath();
            AvailableCameraPaths.Add( path );
            CurrentCameraPath = path;
        }

        public void DeletePath( CameraPath path )
        {
            AvailableCameraPaths.Remove( path );
            CurrentCameraPath = null;
        }

        public void SelectKeyframe( CameraKeyframe kf )
        {
            cameraBeh.Behaviours[cameraBeh.CurrentCameraMode].StopPlaying();

            currentKeyframe = kf;
            cameraBeh.PathKeyframeWindowVisible = true;
            ViewKeyframe( currentKeyframe );
        }

        public void DeselectKeyframe()
        {
            currentKeyframe = null;
            cameraBeh.PathKeyframeWindowVisible = false;
        }

        public void CreateNewKeyframe()
        {
            cameraBeh.Behaviours[cameraBeh.CurrentCameraMode].StopPlaying();
            cameraBeh.CurrentCameraMode = CameraMode.PathCamera;
            cameraBeh.Behaviours[cameraBeh.CurrentCameraMode].StartPlaying();

            cameraBeh.PathWindowVisible = false;

            float time = CurrentCameraPath.keyframeCount > 0 ? CurrentCameraPath.GetKeyframe( CurrentCameraPath.keyframeCount - 1 ).Time + 1 : 0;
            CurrentCameraPath.AddTransform( cameraBeh.FlightCamera.transform, cameraBeh.Zoom, time );
            SelectKeyframe( CurrentCameraPath.GetKeyframe(CurrentCameraPath.keyframeCount - 1) );

            if( CurrentCameraPath.keyframeCount > 6 )
            {
                cameraBeh._pathScrollPosition.y += CameraToolsManager.ENTRY_HEIGHT;
            }
        }

        public void DeleteKeyframe( CameraKeyframe keyframe )
        {
            CurrentCameraPath.RemoveKeyframe( keyframe );
            if( currentKeyframe == keyframe )
            {
                DeselectKeyframe();
                SelectKeyframe( CurrentCameraPath.GetKeyframe( 0 ) );
            }
        }

        /// <summary>
        /// Positions the camera at a keyframe.
        /// </summary>
        public void ViewKeyframe( CameraKeyframe keyframe )
        {
            cameraBeh.Behaviours[cameraBeh.CurrentCameraMode].StopPlaying();
            cameraBeh.CurrentCameraMode = CameraMode.PathCamera;
            cameraBeh.Behaviours[cameraBeh.CurrentCameraMode].StartPlaying();

            cameraBeh.FlightCamera.transform.localPosition = keyframe.Position;
            cameraBeh.FlightCamera.transform.localRotation = keyframe.Rotation;
            cameraBeh.Zoom = keyframe.Zoom;
        }


        public void DrawKeyframeEditorWindow()
        {
            float width = 300;
            float height = 130;

            Rect kWindowRect = new Rect( cameraBeh._windowRect.x - width, cameraBeh._windowRect.y + 365, width, height );
            GUI.Box( kWindowRect, string.Empty );

            GUI.BeginGroup( kWindowRect );

            GUI.Label( new Rect( 5, 5, 100, 25 ), $"Keyframe t={currentKeyframe.Time}" );

            if( GUI.Button( new Rect( 105, 5, 180, 25 ), "Revert Pos" ) )
            {
                ViewKeyframe( currentKeyframe );
            }

            GUI.Label( new Rect( 5, 35, 80, 25 ), "Time: " );
            string s = GUI.TextField( new Rect( 100, 35, 195, 25 ), currentKeyframe.Time.ToString(), 16 );

            if( float.TryParse( s, out float parsed ) )
            {
                currentKeyframe.Time = parsed;
            }

            bool isApplied = false;

            if( GUI.Button( new Rect( 100, 65, 195, 25 ), "Apply" ) )
            {
                Debug.Log( $"Applying keyframe at time: {currentKeyframe.Time}" );
                CurrentCameraPath.SetTransform( currentKeyframe, cameraBeh.FlightCamera.transform, cameraBeh.Zoom, currentKeyframe.Time );
                isApplied = true;
            }

            if( GUI.Button( new Rect( 100, 105, 195, 20 ), "Cancel" ) )
            {
                isApplied = true;
            }

            GUI.EndGroup();

            if( isApplied )
            {
                DeselectKeyframe();
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
            float scrollHeight = Mathf.Max( scrollRectSize, CameraToolsManager.ENTRY_HEIGHT * AvailableCameraPaths.Count );
            Rect scrollViewRect = new Rect( 0, 0, scrollRectSize - 20, scrollHeight );
            cameraBeh._pathSelectScrollPos = GUI.BeginScrollView( scrollRect, cameraBeh._pathSelectScrollPos, scrollViewRect );

            bool isAnyPathSelected = false;

            for( int i = 0; i < AvailableCameraPaths.Count; i++ )
            {
                if( GUI.Button( new Rect( 0, i * CameraToolsManager.ENTRY_HEIGHT, scrollRectSize - 90, CameraToolsManager.ENTRY_HEIGHT ), AvailableCameraPaths[i].PathName ) )
                {
                    CurrentCameraPath = AvailableCameraPaths[i];
                    isAnyPathSelected = true;
                }
                if( GUI.Button( new Rect( scrollRectSize - 80, i * CameraToolsManager.ENTRY_HEIGHT, 60, CameraToolsManager.ENTRY_HEIGHT ), "Delete" ) )
                {
                    DeletePath( AvailableCameraPaths[i] );
                    break;
                }
            }

            GUI.EndScrollView();

            GUI.EndGroup();
            if( isAnyPathSelected )
            {
                cameraBeh.PathWindowVisible = false;
            }
        }
    }
}