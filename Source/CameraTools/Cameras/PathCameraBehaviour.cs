using CameraToolsKatnissified.Animation;
using UnityEngine;

namespace CameraToolsKatnissified.Cameras
{
    public sealed class PathCameraBehaviour : CameraBehaviour
    {
        /// <summary>
        /// If true, the camera is currently playing out a path, instead of setting it up in free mode.
        /// </summary>
        public bool IsPlayingPath { get; set; } = false;

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
            if( cameraBeh._currentCameraPathIndex < 0 || cameraBeh.CurrentCameraPath.keyframeCount <= 0 )
            {
                cameraBeh.EndCamera();
                return;
            }

            cameraBeh.DeselectKeyframe();

            //if( !cameraBeh.CameraToolsActive )
            //{
                OnStartPlaying();
           // }

            CameraTransformation firstFrame = cameraBeh.CurrentCameraPath.Evaulate( 0 );
            cameraBeh.FlightCamera.transform.localPosition = firstFrame.position;
            cameraBeh.FlightCamera.transform.localRotation = firstFrame.rotation;
            cameraBeh.Zoom = firstFrame.zoom;

            isPlaying = true;
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
                CameraTransformation tf = cameraBeh.CurrentCameraPath.Evaulate( cameraBeh.TimeSinceStart * cameraBeh.CurrentCameraPath.timeScale );
                cameraBeh.FlightCamera.transform.localPosition = Vector3.Lerp( cameraBeh.FlightCamera.transform.localPosition, tf.position, cameraBeh.CurrentCameraPath.lerpRate * Time.fixedDeltaTime );
                cameraBeh.FlightCamera.transform.localRotation = Quaternion.Slerp( cameraBeh.FlightCamera.transform.localRotation, tf.rotation, cameraBeh.CurrentCameraPath.lerpRate * Time.fixedDeltaTime );
                cameraBeh.Zoom = Mathf.Lerp( cameraBeh.Zoom, tf.zoom, cameraBeh.CurrentCameraPath.lerpRate * Time.fixedDeltaTime );
            }
            else // this is to set up the path.
            {
                //move
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
        }
    }
}