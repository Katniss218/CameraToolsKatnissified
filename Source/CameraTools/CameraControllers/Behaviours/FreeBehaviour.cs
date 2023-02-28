using CameraToolsKatnissified.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.CameraControllers.Behaviours
{
    public class InputBuffer
    {
        private float[] _values; // circular array, inputs are replaced oldest to latest wraparound.
        private int _oldest = 0;

        public InputBuffer( int length )
        {
            _values = new float[length]; // by default, inputs are 0's.
        }

        public void Add( float input )
        {
            _values[_oldest] = input;
            _oldest++;
            _oldest %= _values.Length;
        }

        public float GetAverage()
        {
            float agg = 0.0f;
            foreach( var v in _values )
            {
                agg += v;
            }
            return agg / _values.Length;
        }
    }

    public class FreeBehaviour : CameraBehaviour
    {
        /// How many frames to smooth over. More = smoother input, but less responsive.
        /// >1 - smoothing, ==1 - no smoothing, <=0 - invalid.
        public int SmoothLength { get; set; } = 10;

        /// Multiplier for scroll wheel input.
        public float ScrollSensitivity { get; set; } = 1.0f;
        /// Multiplier for mouse input.
        public float MouseSensitivity { get; set; } = 1.0f;

        /// Multiplier for keyboard input.
        public float KeyboardSensitivity { get; set; } = 1.0f;

        public float MaxAngularAcceleration { get; set; } = 1.0f;
        public float MaxAcceleration { get; set; } = 1.0f;
        /// Every frame, the _velocity and _angularVelocity are multiplied with this.
        public float Drag { get; set; } = 0.9f;
        public float AngularDrag { get; set; } = 0.9f;

        bool _isOrbiting;

        Vector3 _velocityWS;
        Quaternion _angularVelocityWS;
        float _scrollVelocity;

        Vector3 _upDir;

        InputBuffer _mouseXBuffer;
        InputBuffer _mouseYBuffer;
        InputBuffer _scrollWheelBuffer;
        InputBuffer _moveForwardBuffer;
        InputBuffer _moveSidewaysBuffer;

        public FreeBehaviour( CameraPlayerController controller ) : base( controller )
        {

        }

        // Behaviour controlled by user input.

        // Smoothing uses an array storing the user inputs from SmoothLength last frames.
        // Those inputs are then averaged to produce the final input value.

        // Supported Movement Types:
        // - Panning (mouse)
        // - Orbiting (around the cpc.CameraTargetWorldSpace)
        // - Translation (mouse)

        // - Translation (keyboard)
        // - Rotation (keyboard)

        protected override void OnStartPlaying()
        {
            if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( Ctm.ActiveVessel ) == FlightCamera.Modes.ORBITAL) )
            {
                _upDir = Vector3.up;
            }
            else
            {
                _upDir = -FlightGlobals.getGeeForceAtPosition( Ctm.ActiveVessel.GetWorldPos3D() ).normalized;
            }
            ApplySmoothLength();
            _velocityWS = Vector3.zero;
            _angularVelocityWS = Quaternion.identity;
        }

        void ApplySmoothLength()
        {
            _mouseXBuffer = new InputBuffer( this.SmoothLength );
            _mouseYBuffer = new InputBuffer( this.SmoothLength );
            _scrollWheelBuffer = new InputBuffer( this.SmoothLength );
            _moveForwardBuffer = new InputBuffer( this.SmoothLength );
            _moveSidewaysBuffer = new InputBuffer( this.SmoothLength );
        }

        public override void Update( bool isPlaying )
        {
            if( isPlaying )
            {
                _mouseXBuffer.Add( Input.GetAxis( "Mouse X" ) );
                _mouseYBuffer.Add( Input.GetAxis( "Mouse Y" ) );
                _scrollWheelBuffer.Add( Input.GetAxis( "Mouse ScrollWheel" ) );
                float right = Input.GetKey( KeyCode.LeftArrow ) ? -1.0f : 0.0f;
                right += Input.GetKey( KeyCode.RightArrow ) ? 1.0f : 0.0f;
                float fwd = Input.GetKey( KeyCode.UpArrow ) ? 1.0f : 0.0f;
                fwd += Input.GetKey( KeyCode.DownArrow ) ? -1.0f : 0.0f;
                _moveForwardBuffer.Add( fwd );
                _moveSidewaysBuffer.Add( right );
            }
        }

        Vector3 GetVelocity( Quaternion angularVelocity, Vector3 where )
        {
            angularVelocity.ToAngleAxis( out float anglePerSec, out Vector3 axis );
            // dir
            // tangential velocity magnitude per unit time is radius * angular displacement per unit time
            return Vector3.Cross( axis, where.normalized ) * where.magnitude * anglePerSec; // might need to be converted to radians.
        }

        /// Returns the angular velocity for a circular orbit at a distance from the origin and its tangent velocity.
        Quaternion GetAngularVelocity( Vector3 velocity, Vector3 where )
        {
            Vector3 whereDir = where.normalized;
            Vector3 velTangent = Vector3.ProjectOnPlane( velocity, whereDir );
            Vector3 axis = Vector3.Cross( velTangent.normalized, whereDir ); // if you flip args, the direction flips.
                                                                             // if velocity is units per second, the rotation will be in angle of units per second.

            // v = r * theta
            // theta * r = v
            // theta = v / r
            float anglePerSec = velocity.magnitude / where.magnitude; // angle = velocity / radius
            return Quaternion.AngleAxis( anglePerSec, axis ); // angle possibly might need to be converted to degrees.
                                                              // vice versa on the other end.
        }

        public override void FixedUpdate( bool isPlaying )
        {
            if( isPlaying )
            {
                float mouseX = _mouseXBuffer.GetAverage();
                float mouseY = _mouseYBuffer.GetAverage();
                float mouseScroll = _scrollWheelBuffer.GetAverage();
                float forward = _moveForwardBuffer.GetAverage();
                float sideways = _moveSidewaysBuffer.GetAverage();
                // ...
                // If the player starts orbiting and lets go,
                // then the camera continues moving tangentially with the velocity it had before.
                if( Input.GetKey( KeyCode.Mouse1 ) ) // mouse1 - right
                {
                    if( Input.GetKey( KeyCode.Mouse0 ) ) // mouse2 - middle
                    {
                        // x inputaxis, y inputaxis copy from other controller, but add to velocity instead.
                        // limit to max acceleration.
                        // how do we limit? degrees per second.

                        Debug.Log( "PAN" );
                        this._angularVelocityWS *= Quaternion.AngleAxis( mouseX * MaxAngularAcceleration * Time.fixedDeltaTime, Vector3.up );
                        this._angularVelocityWS *= Quaternion.AngleAxis( -mouseY * MaxAngularAcceleration * Time.fixedDeltaTime, Vector3.right );
                        //_angularVelocityWS.ToAngleAxis( out float a, out Vector3 ax );
                        //this._angularVelocityWS = Quaternion.LookRotation( ax, _upDir );

                        // pan.
                    }
                    else
                    {
                        Debug.Log( "ORBIT" );
                        if( !_isOrbiting )
                        {
                            Debug.Log( "ORBIT 2" );
                            // get angular velocity from velocity and distance.
                            this._angularVelocityWS = GetAngularVelocity( this._velocityWS, this.Pivot.position - this.Controller.CameraTargetWorldSpace.Value );
                            this._velocityWS = Vector3.zero;
                        }
                        else
                        {
                            _isOrbiting = true;
                        }
                        // orbit.

                        this._angularVelocityWS *= Quaternion.AngleAxis( mouseX * MaxAngularAcceleration * Time.fixedDeltaTime, Vector3.up );
                        this._angularVelocityWS *= Quaternion.AngleAxis( -mouseY * MaxAngularAcceleration * Time.fixedDeltaTime, Vector3.right );
                        //_angularVelocityWS.ToAngleAxis( out float a, out Vector3 ax );
                        //this._angularVelocityWS = Quaternion.LookRotation( ax, _upDir );

                    }
                }
                else
                {
                    if( _isOrbiting )
                    {
                        Debug.Log( "o3" );
                        // convert orbital angular velocity to normal velocity.
                        // rotational (angular) velocity remains so that it's not abruptly stopping.
                        this._velocityWS = GetVelocity( this._angularVelocityWS, this.Pivot.position - this.Controller.CameraTargetWorldSpace.Value );

                        _isOrbiting = false;
                    }
                }

                if( Input.GetKey( KeyCode.LeftArrow ) || Input.GetKey( KeyCode.RightArrow )
                 || Input.GetKey( KeyCode.UpArrow ) || Input.GetKey( KeyCode.DownArrow ) )
                {
                    const float MOVE_SCALE = 1.0f;
                    // xy movement.
                    Vector3 forwardAcceleration = this.Pivot.forward * forward;
                    Vector3 rightAcceleration = this.Pivot.right * sideways;

                    Vector3 sum = forwardAcceleration + rightAcceleration;

                    sum *= MaxAcceleration * MOVE_SCALE;

                    this._velocityWS += sum;
                }
#warning TODO - camera values spin out of control.

                _angularVelocityWS.ToAngleAxis( out float angle, out Vector3 axis );

                float scaledAngle = angle * Time.fixedDeltaTime;
                Quaternion scaledAngularVelocity = Quaternion.AngleAxis( scaledAngle, axis );

                // Apply the accelerations based on avg inputs.
                if( _isOrbiting )
                {

                }
                else
                {
                    this.Pivot.position += this._velocityWS * Time.fixedDeltaTime;
                    this.Pivot.rotation *= scaledAngularVelocity;
                }

                /*left + right
                left + middle
                middle + right
                left
                right
                middle*/

                // middle - xy movement camera.forward + camera.right
                // right - orbit
                // middle + right - pan

                // scroll wheel zooms you in and out.
                // - as a multiplier to the normal Zoom value.

                _velocityWS *= Drag;

                angle *= AngularDrag;
                _angularVelocityWS = Quaternion.AngleAxis( angle, axis );
            }
        }

        protected override void OnStopPlaying()
        {
        }

        public override void DrawGui( UILayout UILayout, ref int line )
        {
            GUI.Label( UILayout.GetRectX( line, 0, 6 ), "Smooth length:" );
            int oldSmooth = SmoothLength;
            SmoothLength = int.Parse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), SmoothLength.ToString() ) );
            if( SmoothLength <= 0 )
            {
                SmoothLength = oldSmooth;
            }
            if( oldSmooth != SmoothLength )
            {
                ApplySmoothLength();
            }
            line++;

            GUI.Label( UILayout.GetRectX( line, 0, 6 ), "Scroll sens.:" );
            ScrollSensitivity = GUI.HorizontalSlider( UILayout.GetRectX( line, 7, 11 ), ScrollSensitivity, 0.01f, 10.0f );
            line++;

            GUI.Label( UILayout.GetRectX( line, 0, 6 ), "Scroll sens.:" );
            ScrollSensitivity = GUI.HorizontalSlider( UILayout.GetRectX( line, 7, 11 ), ScrollSensitivity, 0.01f, 10.0f );
            line++;

            GUI.Label( UILayout.GetRectX( line, 0, 6 ), "Mouse sens.:" );
            MouseSensitivity = GUI.HorizontalSlider( UILayout.GetRectX( line, 7, 11 ), MouseSensitivity, 0.01f, 10.0f );
            line++;

            GUI.Label( UILayout.GetRectX( line, 0, 6 ), "Keyboard sens.:" );
            KeyboardSensitivity = GUI.HorizontalSlider( UILayout.GetRectX( line, 7, 11 ), KeyboardSensitivity, 0.01f, 10.0f );
            line++;

            GUI.Label( UILayout.GetRectX( line, 0, 6 ), "Max Acceleration:" );
            MaxAcceleration = float.Parse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), MaxAcceleration.ToString() ) );
            line++;

            GUI.Label( UILayout.GetRectX( line, 0, 6 ), "Max Angular Acceleration:" );
            MaxAngularAcceleration = float.Parse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), MaxAngularAcceleration.ToString() ) );
            line++;

            GUI.Label( UILayout.GetRectX( line, 0, 6 ), "Drag:" );
            Drag = float.Parse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), Drag.ToString() ) );
            line++;

            GUI.Label( UILayout.GetRectX( line, 0, 6 ), "Angular Drag:" );
            AngularDrag = float.Parse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), AngularDrag.ToString() ) );
            line++;
        }
    }
}
