using CameraToolsKatnissified.Animation;
using System.Collections.Generic;
using UnityEngine;

namespace CameraToolsKatnissified.Cameras
{
    [DisallowMultipleComponent]
    public sealed class PathCameraBehaviour : CameraBehaviour
    {
        public static string PATHS_FILE = $"GameData/{CameraToolsManager.DIRECTORY_NAME}/paths.cfg";

        /// <summary>
        /// If true, the camera is currently playing out a path, instead of setting it up in free mode.
        /// </summary>
        public bool IsPlayingPath { get; private set; } = false;

        public List<CameraPath> AvailablePaths { get; private set; }
        public CameraPath CurrentPath { get; private set; }

        public CameraKeyframe CurrentKeyframe { get; private set; }

        public override void OnLoad( ConfigNode node )
        {
            base.OnLoad( node );

            DeselectKeyframe();
            CurrentPath = null;
            AvailablePaths = new List<CameraPath>();

            ConfigNode pathFileNode = ConfigNode.Load( PATHS_FILE );

            foreach( var n in pathFileNode.GetNode( "CAMERAPATHS" ).GetNodes( "CAMERAPATH" ) )
            {
                AvailablePaths.Add( CameraPath.LoadOld( n ) );
            }
        }

        public override void OnSave( ConfigNode node )
        {
            base.OnSave( node );

            ConfigNode pathFileNode = ConfigNode.Load( PATHS_FILE );

            ConfigNode pathsNode = pathFileNode.GetNode( "CAMERAPATHS" );
            pathsNode.RemoveNodes( "CAMERAPATH" );

            foreach( var path in AvailablePaths )
            {
                path.Save( pathsNode );
            }

            pathFileNode.Save( PATHS_FILE );
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
            if( CurrentPath == null || CurrentPath.keyframeCount <= 0 )
            {
                cameraBeh.EndCamera();
                return;
            }

            DeselectKeyframe();
            OnStartPlaying();

            CameraTransformation firstFrame = CurrentPath.Evaulate( 0 );
            cameraBeh.FlightCamera.transform.localPosition = firstFrame.position;
            cameraBeh.FlightCamera.transform.localRotation = firstFrame.rotation;
            cameraBeh.Zoom = firstFrame.zoom;

            IsPlaying = true;
            IsPlayingPath = true;

            // initialize the rotation on start, but don't update it so if the rocket rolls, the camera won't follow it.
            cameraBeh.CameraPivot.transform.position = cameraBeh.ActiveVessel.transform.position;
            cameraBeh.CameraPivot.transform.rotation = cameraBeh.ActiveVessel.transform.rotation;
        }

        protected override void OnPlaying()
        {
            // Update the frame of reference's position to follow the vessel.
            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixPosition )
            {
                cameraBeh.CameraPivot.transform.position = cameraBeh.ActiveVessel.transform.position;
            }
            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixRotation )
            {
                cameraBeh.CameraPivot.transform.rotation = cameraBeh.ActiveVessel.transform.rotation;
            }
            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixPositionAndRotation )
            {
                cameraBeh.CameraPivot.transform.position = cameraBeh.ActiveVessel.transform.position;
                cameraBeh.CameraPivot.transform.rotation = cameraBeh.ActiveVessel.transform.rotation;
            }

            if( IsPlayingPath )
            {
                CameraTransformation tf = CurrentPath.Evaulate( cameraBeh.TimeSinceStart * CurrentPath.TimeScale );
                cameraBeh.FlightCamera.transform.localPosition = Vector3.Lerp( cameraBeh.FlightCamera.transform.localPosition, tf.position, CurrentPath.LerpRate * Time.fixedDeltaTime );
                cameraBeh.FlightCamera.transform.localRotation = Quaternion.Slerp( cameraBeh.FlightCamera.transform.localRotation, tf.rotation, CurrentPath.LerpRate * Time.fixedDeltaTime );
                cameraBeh.Zoom = Mathf.Lerp( cameraBeh.Zoom, tf.zoom, CurrentPath.LerpRate * Time.fixedDeltaTime );
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

            //vessel camera shake
            if( cameraBeh.ShakeMultiplier > 0 )
            {
                foreach( var vessel in FlightGlobals.Vessels )
                {
                    if( !vessel || !vessel.loaded || vessel.packed )
                    {
                        continue;
                    }

                    cameraBeh.DoCameraShake( vessel );
                }

                cameraBeh.UpdateCameraShakeMagnitude();
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
            AvailablePaths.Add( path );
            CurrentPath = path;
        }

        public void DeletePath( CameraPath path )
        {
            AvailablePaths.Remove( path );
            CurrentPath = null;
        }

        public void SelectKeyframe( CameraKeyframe kf )
        {
            cameraBeh.CurrentBehaviour.StopPlaying();

            CurrentKeyframe = kf;
            cameraBeh.PathKeyframeWindowVisible = true;
            ViewKeyframe( CurrentKeyframe );
        }

        public void DeselectKeyframe()
        {
            CurrentKeyframe = null;
            cameraBeh.PathKeyframeWindowVisible = false;
        }

        public void CreateNewKeyframe()
        {
            cameraBeh.CurrentBehaviour.StopPlaying();
            cameraBeh.CurrentCameraMode = CameraMode.PathCamera;
            cameraBeh.CurrentBehaviour.StartPlaying();

            cameraBeh.PathWindowVisible = false;

            float time = CurrentPath.keyframeCount > 0 ? CurrentPath.GetKeyframe( CurrentPath.keyframeCount - 1 ).Time + 1 : 0;
            CurrentPath.AddTransform( cameraBeh.FlightCamera.transform, cameraBeh.Zoom, time );
            SelectKeyframe( CurrentPath.GetKeyframe( CurrentPath.keyframeCount - 1 ) );

            if( CurrentPath.keyframeCount > 6 )
            {
                cameraBeh._pathScrollPosition.y += CameraToolsManager.ENTRY_HEIGHT;
            }
        }

        public void DeleteKeyframe( CameraKeyframe keyframe )
        {
            CurrentPath.RemoveKeyframe( keyframe );
            if( CurrentKeyframe == keyframe )
            {
                DeselectKeyframe();
                SelectKeyframe( CurrentPath.GetKeyframe( 0 ) );
            }
        }

        /// <summary>
        /// Positions the camera at a keyframe.
        /// </summary>
        public void ViewKeyframe( CameraKeyframe keyframe )
        {
            cameraBeh.CurrentBehaviour.StopPlaying();
            cameraBeh.CurrentCameraMode = CameraMode.PathCamera;
            cameraBeh.CurrentBehaviour.StartPlaying();

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

            GUI.Label( new Rect( 5, 5, 100, 25 ), $"Keyframe t={CurrentKeyframe.Time}" );

            if( GUI.Button( new Rect( 105, 5, 180, 25 ), "Revert Pos" ) )
            {
                ViewKeyframe( CurrentKeyframe );
            }

            GUI.Label( new Rect( 5, 35, 80, 25 ), "Time: " );
            string s = GUI.TextField( new Rect( 100, 35, 195, 25 ), CurrentKeyframe.Time.ToString(), 16 );

            if( float.TryParse( s, out float parsed ) )
            {
                CurrentKeyframe.Time = parsed;
            }

            bool isApplied = false;

            if( GUI.Button( new Rect( 100, 65, 195, 25 ), "Apply" ) )
            {
                Debug.Log( $"Applying keyframe at time: {CurrentKeyframe.Time}" );
                CurrentPath.SetTransform( CurrentKeyframe, cameraBeh.FlightCamera.transform, cameraBeh.Zoom, CurrentKeyframe.Time );
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
            float scrollHeight = Mathf.Max( scrollRectSize, CameraToolsManager.ENTRY_HEIGHT * AvailablePaths.Count );
            Rect scrollViewRect = new Rect( 0, 0, scrollRectSize - 20, scrollHeight );
            cameraBeh._pathSelectScrollPos = GUI.BeginScrollView( scrollRect, cameraBeh._pathSelectScrollPos, scrollViewRect );

            bool isAnyPathSelected = false;

            for( int i = 0; i < AvailablePaths.Count; i++ )
            {
                if( GUI.Button( new Rect( 0, i * CameraToolsManager.ENTRY_HEIGHT, scrollRectSize - 90, CameraToolsManager.ENTRY_HEIGHT ), AvailablePaths[i].PathName ) )
                {
                    CurrentPath = AvailablePaths[i];
                    isAnyPathSelected = true;
                }
                if( GUI.Button( new Rect( scrollRectSize - 80, i * CameraToolsManager.ENTRY_HEIGHT, 60, CameraToolsManager.ENTRY_HEIGHT ), "Delete" ) )
                {
                    DeletePath( AvailablePaths[i] );
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