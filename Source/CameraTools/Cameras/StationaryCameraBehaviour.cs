using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.Cameras
{
    public sealed class StationaryCameraBehaviour : CameraBehaviour
    {

        public Vector3? StationaryCameraPosition { get; set; } = null;
        public bool HasPosition => StationaryCameraPosition != null;

        public Part StationaryCameraTarget { get; set; } = null;
        public bool HasTarget => StationaryCameraTarget != null;


        protected override void OnStartPlaying()
        {
            Debug.Log( "[CTK] Stationary Camera Active" );
            Debug.Log( "flightCamera position init: " + cameraBeh.FlightCamera.transform.position );

            if( FlightGlobals.ActiveVessel != null )
            {
                cameraBeh.FlightCamera.SetTargetNone();
                cameraBeh.FlightCamera.transform.parent = cameraBeh.CameraPivot.transform;
                cameraBeh.FlightCamera.DeactivateUpdate();

                cameraBeh.CameraPivot.transform.position = cameraBeh.ActiveVessel.transform.position + cameraBeh.ActiveVessel.rb_velocity * Time.fixedDeltaTime;
                cameraBeh.ManualPosition = Vector3.zero;

                if( HasPosition )
                {
                    cameraBeh.FlightCamera.transform.position = StationaryCameraPosition.Value;
                }

                cameraBeh.InitialVelocity = cameraBeh.ActiveVessel.srf_velocity;
                cameraBeh.InitialOrbit = new Orbit();
                cameraBeh.InitialOrbit.UpdateFromStateVectors( cameraBeh.ActiveVessel.orbit.pos, cameraBeh.ActiveVessel.orbit.vel, FlightGlobals.currentMainBody, Planetarium.GetUniversalTime() );
                //_initialUT = Planetarium.GetUniversalTime();
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }

            Debug.Log( "flightCamera position post init: " + cameraBeh.FlightCamera.transform.position );
        }

        protected override void OnPlaying()
        {
            if( cameraBeh.FlightCamera.Target != null )
            {
                cameraBeh.FlightCamera.SetTargetNone(); //dont go to next vessel if vessel is destroyed
            }

            if( HasTarget )
            {
                Vector3 toTargetDirection = (StationaryCameraTarget.transform.position - cameraBeh.FlightCamera.transform.position).normalized;

                cameraBeh.FlightCamera.transform.rotation = Quaternion.LookRotation( toTargetDirection, cameraBeh.UpDirection );
            }

            if( cameraBeh.ActiveVessel != null )
            {
                // Parent follows the vessel.
                cameraBeh.CameraPivot.transform.position = cameraBeh.ManualPosition + cameraBeh.ActiveVessel.transform.position;

                // Camera itself accumulates the inverse of the vessel movement.
                if( cameraBeh.CurrentReferenceMode == CameraReference.Surface )
                {
                    float magnitude = Mathf.Clamp( (float)cameraBeh.ActiveVessel.srf_velocity.magnitude, 0, cameraBeh.MaxRelativeVelocity );
                    cameraBeh.FlightCamera.transform.position -= Time.fixedDeltaTime * magnitude * cameraBeh.ActiveVessel.srf_velocity.normalized;
                }
                else if( cameraBeh.CurrentReferenceMode == CameraReference.Orbit )
                {
                    float magnitude = Mathf.Clamp( (float)cameraBeh.ActiveVessel.obt_velocity.magnitude, 0, cameraBeh.MaxRelativeVelocity );
                    cameraBeh.FlightCamera.transform.position -= Time.fixedDeltaTime * magnitude * cameraBeh.ActiveVessel.obt_velocity.normalized;
                }
                else if( cameraBeh.CurrentReferenceMode == CameraReference.InitialVelocity )
                {
                    Vector3 camVelocity;
                    if( cameraBeh.UseOrbitalInitialVelocity && cameraBeh.InitialOrbit != null )
                    {
                        camVelocity = cameraBeh.InitialOrbit.getOrbitalVelocityAtUT( Planetarium.GetUniversalTime() ).xzy - cameraBeh.ActiveVessel.GetObtVelocity();
                    }
                    else
                    {
                        camVelocity = cameraBeh.InitialVelocity - cameraBeh.ActiveVessel.srf_velocity;
                    }
                    cameraBeh.FlightCamera.transform.position += camVelocity * Time.fixedDeltaTime;
                }
#warning TODO - add the velocity direction mode here.
            }

            //mouse panning, moving
            Vector3 forwardLevelAxis = (Quaternion.AngleAxis( -90, cameraBeh.UpDirection ) * cameraBeh.FlightCamera.transform.right).normalized;

            if( Input.GetKey( KeyCode.Mouse1 ) ) // right mouse
            {
                // No target - should turn the camera like a tripod.
                // Has target - should orbit the target.
                if( !HasTarget )
                {
                    cameraBeh.FlightCamera.transform.rotation *= Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * 1.7f, Vector3.up );
                    cameraBeh.FlightCamera.transform.rotation *= Quaternion.AngleAxis( -Input.GetAxis( "Mouse Y" ) * 1.7f, Vector3.right );
                    cameraBeh.FlightCamera.transform.rotation = Quaternion.LookRotation( cameraBeh.FlightCamera.transform.forward, cameraBeh.UpDirection );
                }
                else
                {
                    var verticalaxis = cameraBeh.FlightCamera.transform.TransformDirection( Vector3.up );
                    var horizontalaxis = cameraBeh.FlightCamera.transform.TransformDirection( Vector3.right );
                    cameraBeh.FlightCamera.transform.RotateAround( StationaryCameraTarget.transform.position, verticalaxis, Input.GetAxis( "Mouse X" ) * 1.7f );
                    cameraBeh.FlightCamera.transform.RotateAround( StationaryCameraTarget.transform.position, horizontalaxis, -Input.GetAxis( "Mouse Y" ) * 1.7f );
                    cameraBeh.FlightCamera.transform.rotation = Quaternion.LookRotation( cameraBeh.FlightCamera.transform.forward, cameraBeh.UpDirection );
                }
            }

            if( Input.GetKey( KeyCode.Mouse2 ) ) // middle mouse
            {
                cameraBeh.ManualPosition += cameraBeh.FlightCamera.transform.right * Input.GetAxis( "Mouse X" ) * 2;
                cameraBeh.ManualPosition += forwardLevelAxis * Input.GetAxis( "Mouse Y" ) * 2;
            }

            cameraBeh.ManualPosition += cameraBeh.UpDirection * CameraToolsBehaviour.SCROLL_MULTIPLIER * Input.GetAxis( "Mouse ScrollWheel" );

            // autoFov
            if( HasTarget && cameraBeh.UseAutoZoom )
            {
                float cameraDistance = Vector3.Distance( StationaryCameraTarget.transform.position, cameraBeh.FlightCamera.transform.position );

                float targetFoV = Mathf.Clamp( (7000 / (cameraDistance + 100)) - 14 + cameraBeh.AutoZoomMargin, 2, 60 );

                cameraBeh.ManualFov = targetFoV;
            }

            //FOV
            if( !cameraBeh.UseAutoZoom )
            {
                cameraBeh.ZoomFactor = Mathf.Exp( cameraBeh.Zoom ) / Mathf.Exp( 1 );
                cameraBeh.ManualFov = 60 / cameraBeh.ZoomFactor;

                if( cameraBeh.CurrentFov != cameraBeh.ManualFov )
                {
                    cameraBeh.CurrentFov = Mathf.Lerp( cameraBeh.CurrentFov, cameraBeh.ManualFov, 0.1f );
                    cameraBeh.FlightCamera.SetFoV( cameraBeh.CurrentFov );
                }
            }
            else
            {
                cameraBeh.CurrentFov = Mathf.Lerp( cameraBeh.CurrentFov, cameraBeh.ManualFov, 0.1f );
                cameraBeh.FlightCamera.SetFoV( cameraBeh.CurrentFov );
                cameraBeh.ZoomFactor = 60 / cameraBeh.CurrentFov;
            }

            cameraBeh.LastCameraPosition = cameraBeh.FlightCamera.transform.position;
            cameraBeh.LastCameraRotation = cameraBeh.FlightCamera.transform.rotation;

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

        }
    }
}