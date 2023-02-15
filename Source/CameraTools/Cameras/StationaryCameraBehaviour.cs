using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.Cameras
{
    [DisallowMultipleComponent]
    public sealed class StationaryCameraBehaviour : CameraBehaviour
    {
        public Vector3? CameraPosition { get; private set; } = null;

        public Part Target { get; private set; } = null;

        public CameraReference CurrentReferenceMode { get; private set; } = CameraReference.Surface;

        public Vector3 ManualOffset { get; set; } = Vector3.zero; // offset from moving the camera manually.

        public Vector3 UpDirection { get; set; } = Vector3.up;

        /// <summary>
        /// Whether or not to use orbital velocity as reference. True - uses orbital velocity, False - uses surface velocity.
        /// </summary>
        public bool UseOrbitalInitialVelocity { get; set; } = false;

        /// <summary>
        /// Maximum velocity of the target relative to the camera. Can be negative to reverse the camera direction.
        /// </summary>
        public float MaxRelativeVelocity { get; set; } = 250.0f;

        // Used for the Initial Velocity camera mode.
        Vector3 _initialVelocity;
        Orbit _initialOrbit;

        bool _settingPositionEnabled;
        bool _settingTargetEnabled;

        Vector3 _initialOffset = Vector3.zero;
        Vector3 _accumulatedPosition = Vector3.zero;

        public StationaryCameraBehaviour( CameraToolsManager ctm ) : base( ctm )
        {

        }

        public override void Update()
        {
            if( Input.GetMouseButtonUp( 0 ) || !Input.GetMouseButton( 0 ) )
            {
                cameraBeh._wasMouseUp = true;
            }

            // Set target from a mouse raycast.
            if( _settingTargetEnabled && cameraBeh._wasMouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                _settingTargetEnabled = false;

                Part newTarget = Utils.GetPartFromMouse();
                if( newTarget != null )
                {
                    Target = newTarget;
                }
            }

            // Set position from a mouse raycast
            if( _settingPositionEnabled && cameraBeh._wasMouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                _settingPositionEnabled = false;

                Vector3? newPosition = Utils.GetPosFromMouse();
                if( newPosition != null )
                {
                    CameraPosition = newPosition;
                }
            }
        }

        protected override void OnStartPlaying()
        {
            Debug.Log( "[CTK] Stationary Camera Active" );
            Debug.Log( "flightCamera position init: " + cameraBeh.FlightCamera.transform.position );

            if( FlightGlobals.ActiveVessel != null )
            {
                if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( cameraBeh.ActiveVessel ) == FlightCamera.Modes.ORBITAL) )
                {
                    UpDirection = Vector3.up;
                }
                else
                {
                    UpDirection = -FlightGlobals.getGeeForceAtPosition( cameraBeh.ActiveVessel.GetWorldPos3D() ).normalized;
                }

                _initialOffset = cameraBeh.FlightCamera.transform.position - cameraBeh.ActiveVessel.transform.position;

                cameraBeh.FlightCamera.SetTargetNone();
                cameraBeh.FlightCamera.transform.SetParent( cameraBeh.CameraPivot.transform ); // Apparently this is not assigned by the global when starting playing.
                cameraBeh.FlightCamera.DeactivateUpdate();

                ManualOffset = Vector3.zero;

                if( CameraPosition != null )
                {
                    cameraBeh.FlightCamera.transform.position = CameraPosition.Value;
                }

                _accumulatedPosition = Vector3.zero;
                _initialVelocity = cameraBeh.ActiveVessel.srf_velocity;
                _initialOrbit = new Orbit();
                _initialOrbit.UpdateFromStateVectors( cameraBeh.ActiveVessel.orbit.pos, cameraBeh.ActiveVessel.orbit.vel, FlightGlobals.currentMainBody, Planetarium.GetUniversalTime() );
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

            if( Target != null )
            {
                Vector3 toTargetDirection = (Target.transform.position - cameraBeh.FlightCamera.transform.position).normalized;

                cameraBeh.FlightCamera.transform.rotation = Quaternion.LookRotation( toTargetDirection, UpDirection );
            }

            if( cameraBeh.ActiveVessel != null )
            {
                // Parent follows the vessel.
                cameraBeh.FlightCamera.transform.position = ManualOffset + cameraBeh.ActiveVessel.transform.position + _initialOffset;

                // Camera itself accumulates the inverse of the vessel movement.
                if( CurrentReferenceMode == CameraReference.Surface )
                {
                    float magnitude = Mathf.Clamp( (float)cameraBeh.ActiveVessel.srf_velocity.magnitude, 0, MaxRelativeVelocity );
                    _accumulatedPosition -= (magnitude * cameraBeh.ActiveVessel.srf_velocity.normalized) * Time.fixedDeltaTime;
                }
                else if( CurrentReferenceMode == CameraReference.Orbit )
                {
                    float magnitude = Mathf.Clamp( (float)cameraBeh.ActiveVessel.obt_velocity.magnitude, 0, MaxRelativeVelocity );
                    _accumulatedPosition -= (magnitude * cameraBeh.ActiveVessel.obt_velocity.normalized) * Time.fixedDeltaTime;
                }
                else if( CurrentReferenceMode == CameraReference.InitialVelocity )
                {
                    Vector3 cameraVelocity;
                    if( UseOrbitalInitialVelocity && _initialOrbit != null )
                    {
                        cameraVelocity = _initialOrbit.getOrbitalVelocityAtUT( Planetarium.GetUniversalTime() ).xzy - cameraBeh.ActiveVessel.GetObtVelocity();
                    }
                    else
                    {
                        cameraVelocity = _initialVelocity - cameraBeh.ActiveVessel.srf_velocity;
                    }

                    _accumulatedPosition += cameraVelocity * Time.fixedDeltaTime;
                }
                cameraBeh.FlightCamera.transform.position += _accumulatedPosition;
            }

            //mouse panning, moving
            Vector3 forwardLevelAxis = (Quaternion.AngleAxis( -90, UpDirection ) * cameraBeh.FlightCamera.transform.right).normalized;


#warning TODO - this panning and stuff would be a separate controller, playercamera, which could be locked from the playing behaviour, e.g. preventing you from moving a path camera.
            // setting a path would use its own controls likely. you can also input the numbers by hand into the gui (rotation is eulers), but they are also inputted by moving the camera around
            // add the velocity direction mode there too.
            // this user controller would interact with a dedicated user offset gameobject.
            if( Input.GetKey( KeyCode.Mouse1 ) ) // right mouse
            {
                // No target - should turn the camera like a tripod.
                // Has target - should orbit the target.
                if( Target == null )
                {
                    cameraBeh.FlightCamera.transform.rotation *= Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * 1.7f, Vector3.up );
                    cameraBeh.FlightCamera.transform.rotation *= Quaternion.AngleAxis( -Input.GetAxis( "Mouse Y" ) * 1.7f, Vector3.right );
                    cameraBeh.FlightCamera.transform.rotation = Quaternion.LookRotation( cameraBeh.FlightCamera.transform.forward, UpDirection );
                }
                else
                {
                    Vector3 cachePos = cameraBeh.FlightCamera.transform.position;

                    var verticalaxis = cameraBeh.FlightCamera.transform.TransformDirection( Vector3.up );
                    var horizontalaxis = cameraBeh.FlightCamera.transform.TransformDirection( Vector3.right );
                    cameraBeh.FlightCamera.transform.RotateAround( Target.transform.position, verticalaxis, Input.GetAxis( "Mouse X" ) * 1.7f );
                    cameraBeh.FlightCamera.transform.RotateAround( Target.transform.position, horizontalaxis, -Input.GetAxis( "Mouse Y" ) * 1.7f );
                    cameraBeh.FlightCamera.transform.rotation = Quaternion.LookRotation( cameraBeh.FlightCamera.transform.forward, UpDirection );

                    ManualOffset += (cameraBeh.FlightCamera.transform.position - cachePos); // allow movement (temporary until separate controllers).
                    cameraBeh.FlightCamera.transform.position = cachePos; // stop flickering (sortof).
                }
            }

            if( Input.GetKey( KeyCode.Mouse2 ) ) // middle mouse
            {
                ManualOffset += cameraBeh.FlightCamera.transform.right * Input.GetAxis( "Mouse X" ) * 2;
                ManualOffset += forwardLevelAxis * Input.GetAxis( "Mouse Y" ) * 2;
            }

            ManualOffset += UpDirection * CameraToolsManager.SCROLL_MULTIPLIER * Input.GetAxis( "Mouse ScrollWheel" );

            // autoFov
            if( Target != null && cameraBeh.UseAutoZoom )
            {
#warning TODO - change this to use the equation for constant angular size, possibly go through the parts in the vessel to determine its longest axis, or maybe there are bounds.

                float cameraDistance = Vector3.Distance( Target.transform.position, cameraBeh.FlightCamera.transform.position );

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

        public override void DrawGui( ref float line )
        {
            GUI.Label( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, CameraToolsManager.ENTRY_HEIGHT ), $"Frame of Reference: {CurrentReferenceMode}" );
            line++;
            if( GUI.Button( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), 25, CameraToolsManager.ENTRY_HEIGHT - 2 ), "<" ) )
            {
                CurrentReferenceMode = Utils.CycleEnum( CurrentReferenceMode, -1 );
            }
            if( GUI.Button( new Rect( CameraToolsManager.GUI_MARGIN + 25 + 4, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), 25, CameraToolsManager.ENTRY_HEIGHT - 2 ), ">" ) )
            {
                CurrentReferenceMode = Utils.CycleEnum( CurrentReferenceMode, 1 );
            }

            line++;

            if( CurrentReferenceMode == CameraReference.Surface || CurrentReferenceMode == CameraReference.Orbit )
            {
                GUI.Label( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH / 2, CameraToolsManager.ENTRY_HEIGHT ), "Max Rel. V:" );
                MaxRelativeVelocity = float.Parse( GUI.TextField( new Rect( CameraToolsManager.GUI_MARGIN + CameraToolsManager.CONTENT_WIDTH / 2, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH / 2, CameraToolsManager.ENTRY_HEIGHT ), MaxRelativeVelocity.ToString() ) );
            }
            else if( CurrentReferenceMode == CameraReference.InitialVelocity )
            {
                UseOrbitalInitialVelocity = GUI.Toggle( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, CameraToolsManager.ENTRY_HEIGHT ), UseOrbitalInitialVelocity, " Orbital" );
            }
            line++;
            line++;

            // Draw position buttons.

            string positionText = CameraPosition == null ? "None" : CameraPosition.Value.ToString();
            GUI.Label( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, CameraToolsManager.ENTRY_HEIGHT ), $"Camera Position: {positionText}" );
            line++;

            positionText = _settingPositionEnabled ? "waiting..." : "Set Position";
            if( GUI.Button( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH / 2, CameraToolsManager.ENTRY_HEIGHT - 2 ), positionText ) )
            {
                _settingPositionEnabled = true;
                cameraBeh._wasMouseUp = false;
            }
            if( GUI.Button( new Rect( 2 + CameraToolsManager.GUI_MARGIN + CameraToolsManager.CONTENT_WIDTH / 2, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), (CameraToolsManager.CONTENT_WIDTH / 2) - 2, CameraToolsManager.ENTRY_HEIGHT - 2 ), "Clear Position" ) )
            {
                CameraPosition = null;
            }
            line++;
            line++;

            // Draw target buttons.

            string targetText = Target == null ? "None" : Target.gameObject.name;
#warning TODO - When the part name is too long, it cuts the word off.
            GUI.Label( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, CameraToolsManager.ENTRY_HEIGHT ), $"Camera Target: {targetText}" );
            line++;

            targetText = _settingTargetEnabled ? "waiting..." : "Set Target";
            if( GUI.Button( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH / 2, CameraToolsManager.ENTRY_HEIGHT - 2 ), targetText ) )
            {
                _settingTargetEnabled = true;
                cameraBeh._wasMouseUp = false;
            }
            if( GUI.Button( new Rect( 2 + CameraToolsManager.GUI_MARGIN + CameraToolsManager.CONTENT_WIDTH / 2, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), (CameraToolsManager.CONTENT_WIDTH / 2) - 2, CameraToolsManager.ENTRY_HEIGHT - 2 ), "Clear Target" ) )
            {
                Target = null;
            }
        }
    }
}