using CameraToolsKatnissified.UI;
using CameraToolsKatnissified.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.CameraControllers.Behaviours
{
    /// <summary>
    /// A floating point circular buffer.
    /// </summary>
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
        public enum FrameOfReference
        {
            Camera,
            CameraGravUp,
            Self,
            SelfGravUp
        }

        /// <summary>
        /// Reference frame of the input.
        /// </summary>
        public FrameOfReference ReferenceFrame { get; set; } = FrameOfReference.CameraGravUp;

        /// How many frames to smooth over. More = smoother input, but less responsive.
        /// >1 - smoothing, ==1 - no smoothing, <=0 - invalid.
        public int SmoothLength { get; set; } = 20;

        /// Multiplier for scroll wheel input.
        public float ScrollSensitivity { get; set; } = 1.0f;
        /// Multiplier for mouse input.
        public float MouseSensitivity { get; set; } = 1.0f;

        /// Multiplier for keyboard input.
        public float KeyboardSensitivity { get; set; } = 1.0f;

        public float MaxAngularAcceleration { get; set; } = 5.0f;
        public float MaxAcceleration { get; set; } = 5.0f;
        /// Every frame, the _velocity and _angularVelocity are multiplied with this.
        public float Drag { get; set; } = 0.95f;
        public float AngularDrag { get; set; } = 0.95f;

        bool _isOrbiting;

        Vector3 _velocityWS;
        Vector3 _angularVelocityWS;
        float _scrollVelocity;

        Vector3 _gravityUp;

        InputBuffer _mouseXBuffer;
        InputBuffer _mouseYBuffer;
        InputBuffer _scrollWheelBuffer;
        InputBuffer _moveForwardBuffer;
        InputBuffer _moveSidewaysBuffer;
        InputBuffer _moveVerticalBuffer;

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
                _gravityUp = Vector3.up;
            }
            else
            {
                _gravityUp = -FlightGlobals.getGeeForceAtPosition( Ctm.ActiveVessel.GetWorldPos3D() ).normalized;
            }
            ApplySmoothLength();
            _velocityWS = Vector3.zero;
            _angularVelocityWS = Vector3.zero;
            _scrollVelocity = 0.0f;
            _isOrbiting = false;
        }

        void ApplySmoothLength()
        {
            _mouseXBuffer = new InputBuffer( this.SmoothLength );
            _mouseYBuffer = new InputBuffer( this.SmoothLength );
            _scrollWheelBuffer = new InputBuffer( this.SmoothLength );
            _moveForwardBuffer = new InputBuffer( this.SmoothLength );
            _moveSidewaysBuffer = new InputBuffer( this.SmoothLength );
            _moveVerticalBuffer = new InputBuffer( this.SmoothLength );
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
                float vert = Input.GetKey( KeyCode.RightShift ) ? 1.0f : 0.0f;
                vert += Input.GetKey( KeyCode.RightControl ) ? -1.0f : 0.0f;
                _moveForwardBuffer.Add( fwd );
                _moveSidewaysBuffer.Add( right );
                _moveVerticalBuffer.Add( vert );
            }
        }

        Vector3 GetVelocity( Vector3 angularVelocity, Vector3 where )
        {
            float anglePerSec = angularVelocity.magnitude;
            Vector3 axis = angularVelocity.normalized;
            // dir
            // tangential velocity magnitude per unit time is radius * angular displacement per unit time
            return Vector3.Cross( axis, where.normalized ) * where.magnitude * (anglePerSec * Mathf.Deg2Rad); // might need to be converted to radians.
        }

        /// Returns the angular velocity for a circular orbit at a distance from the origin and its tangent velocity.
        Vector3 GetAngularVelocity( Vector3 velocity, Vector3 where )
        {
            Vector3 whereDir = where.normalized;
            Vector3 velTangent = Vector3.ProjectOnPlane( velocity, whereDir ).normalized;
            Vector3 axis = Vector3.Cross( velTangent, whereDir ); // if you flip args, the direction flips.
                                                                  // if velocity is units per second, the rotation will be in angle of units per second.

            // v = r * theta
            // theta * r = v
            // theta = v / r
            float anglePerSec = velocity.magnitude / where.magnitude; // angle = velocity / radius
            return axis * (anglePerSec * Mathf.Rad2Deg); // angle possibly might need to be converted to degrees.
                                                         // vice versa on the other end.
        }

        public Vector3 GetUpDir()
        {
            if( ReferenceFrame == FrameOfReference.Camera )
            {
                return this.Ctm.FlightCamera.transform.up;
            }
            if( ReferenceFrame == FrameOfReference.CameraGravUp )
            {
                return _gravityUp;
            }
            if( ReferenceFrame == FrameOfReference.Self )
            {
                return this.Pivot.up;
            }
            if( ReferenceFrame == FrameOfReference.SelfGravUp )
            {
                return _gravityUp;
            }
            throw new InvalidOperationException( $"Unknown Reference Frame '{ReferenceFrame}'." );
        }

        public Vector3 GetRightDir()
        {
            // for GravUp, project it on the tangent plane.
            if( ReferenceFrame == FrameOfReference.Camera )
            {
                return this.Ctm.FlightCamera.transform.right;
            }
            if( ReferenceFrame == FrameOfReference.CameraGravUp )
            {
                return Vector3.ProjectOnPlane( this.Ctm.FlightCamera.transform.right, _gravityUp ).normalized;
            }
            if( ReferenceFrame == FrameOfReference.Self )
            {
                return this.Pivot.right;
            }
            if( ReferenceFrame == FrameOfReference.SelfGravUp )
            {
                return Vector3.ProjectOnPlane( this.Pivot.right, _gravityUp ).normalized;
            }
            throw new InvalidOperationException( $"Unknown Reference Frame '{ReferenceFrame}'." );
        }

        public Vector3 GetForwardDir()
        {
            // for GravUp, project it on the tangent plane.
            if( ReferenceFrame == FrameOfReference.Camera )
            {
                return this.Ctm.FlightCamera.transform.forward;
            }
            if( ReferenceFrame == FrameOfReference.CameraGravUp )
            {
                return Vector3.ProjectOnPlane( this.Ctm.FlightCamera.transform.forward, _gravityUp ).normalized;
            }
            if( ReferenceFrame == FrameOfReference.Self )
            {
                return this.Pivot.forward;
            }
            if( ReferenceFrame == FrameOfReference.SelfGravUp )
            {
                return Vector3.ProjectOnPlane( this.Pivot.forward, _gravityUp ).normalized;
            }
            throw new InvalidOperationException( $"Unknown Reference Frame '{ReferenceFrame}'." );
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
                float vert = _moveVerticalBuffer.GetAverage();
                // ...
                // If the player starts orbiting and lets go,
                // then the camera continues moving tangentially with the velocity it had before.
                if( Input.GetKey( KeyCode.Mouse1 ) ) // mouse1 - right
                {
                    if( Input.GetKey( KeyCode.Mouse0 ) ) // mouse0 - left
                    {
                        if( _isOrbiting )
                        {
                            // convert orbital angular velocity to normal velocity.
                            // rotational (angular) velocity remains so that it's not abruptly stopping.
                            this._velocityWS = GetVelocity( this._angularVelocityWS, this.Pivot.position - this.Controller.CameraTargetWorldSpace.Value );
                            this._angularVelocityWS = Vector3.zero;
                            _isOrbiting = false;
                        }

                        // First add the rotation in world space, and then rotate that by the leftover rotation.
                        this._angularVelocityWS += (mouseX * MaxAngularAcceleration) * this.GetUpDir();
                        this._angularVelocityWS += (-mouseY * MaxAngularAcceleration) * this.GetRightDir();

                        //_angularVelocityWS.ToAngleAxis( out float a, out Vector3 ax );
                        //this._angularVelocityWS = Quaternion.LookRotation( ax, _upDir );

                        // pan.
                    }
                    else
                    {
                        if( !_isOrbiting && this.Controller.CameraTargetWorldSpace != null ) // start orbiting
                        {
                            // get angular velocity from velocity and distance.
                            this._angularVelocityWS = GetAngularVelocity( this._velocityWS, this.Pivot.position - this.Controller.CameraTargetWorldSpace.Value );
                            this._velocityWS = Vector3.zero;
                            _isOrbiting = true;
                        }
                        // orbit.
                        this._angularVelocityWS += (mouseX * MaxAngularAcceleration) * this.GetUpDir();
                        this._angularVelocityWS += (-mouseY * MaxAngularAcceleration) * this.GetRightDir();

                        //_angularVelocityWS.ToAngleAxis( out float a, out Vector3 ax );
                        //this._angularVelocityWS = Quaternion.LookRotation( ax, _upDir );
                    }
                }
                else
                {
                    if( _isOrbiting )
                    {
                        //this._velocityWS = GetVelocity( this._angularVelocityWS, this.Pivot.position - this.Controller.CameraTargetWorldSpace.Value );
                        //this._angularVelocityWS = Vector3.zero;
                        _isOrbiting = false;
                    }
                }
                if( mouseScroll != 0f )
                {
                    this._scrollVelocity += mouseScroll * ScrollSensitivity;
                    this.Controller.Zoom += _scrollVelocity * 0.1f;
                }

                if( Input.GetKey( KeyCode.LeftArrow ) || Input.GetKey( KeyCode.RightArrow )
                 || Input.GetKey( KeyCode.UpArrow ) || Input.GetKey( KeyCode.DownArrow )
                 || Input.GetKey( KeyCode.RightShift ) || Input.GetKey( KeyCode.RightControl ) )
                {
                    const float MOVE_SCALE = 1.0f;
                    // xy movement.
                    Vector3 forwardAcceleration = this.GetForwardDir() * forward;
                    Vector3 rightAcceleration = this.GetRightDir() * sideways;
                    Vector3 upAcceleration = this.GetUpDir() * vert;

                    Vector3 sum = forwardAcceleration + rightAcceleration + upAcceleration;

                    sum *= MaxAcceleration * MOVE_SCALE;

                    this._velocityWS += sum;
                }

                float angle = _angularVelocityWS.magnitude;
                Vector3 axis = _angularVelocityWS.normalized;

                // Apply the accelerations based on avg inputs.
                if( _isOrbiting )
                {
                    this.Pivot.RotateAround( this.Controller.CameraTargetWorldSpace.Value, axis, angle * Time.fixedDeltaTime );
                }
                else
                {
                    this.Pivot.position += this._velocityWS * Time.fixedDeltaTime;
                    this.Pivot.rotation = Quaternion.AngleAxis( angle * Time.fixedDeltaTime, axis ) * this.Pivot.rotation; // Non-commutative
                }

                _velocityWS *= Drag;

                _angularVelocityWS *= AngularDrag;
            }
        }

        protected override void OnStopPlaying()
        {
        }

        public override void DrawGui( UILayout UILayout, ref int line )
        {
            GUI.Label( UILayout.GetRectX( line, 1, 9 ), $"Reference: {ReferenceFrame}" );
            if( GUI.Button( UILayout.GetRect( 10, line ), "<" ) )
            {
                ReferenceFrame = Misc.CycleEnum( ReferenceFrame, -1 );
            }
            if( GUI.Button( UILayout.GetRect( 11, line ), ">" ) )
            {
                ReferenceFrame = Misc.CycleEnum( ReferenceFrame, 1 );
            }
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "Smooth Length:" );
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

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "Scroll Sens.:" );
            ScrollSensitivity = GUI.HorizontalSlider( UILayout.GetRectX( line, 7, 11 ), ScrollSensitivity, 0.01f, 10.0f );
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "Mouse Sens.:" );
            MouseSensitivity = GUI.HorizontalSlider( UILayout.GetRectX( line, 7, 11 ), MouseSensitivity, 0.01f, 10.0f );
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "Keyboard Sens.:" );
            KeyboardSensitivity = GUI.HorizontalSlider( UILayout.GetRectX( line, 7, 11 ), KeyboardSensitivity, 0.01f, 10.0f );
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 5 ), "Accel. (L/A):" );
            MaxAcceleration = float.Parse( GUI.TextField( UILayout.GetRectX( line, 6, 8 ), MaxAcceleration.ToString( "0.0#########" ) ) );
            MaxAngularAcceleration = float.Parse( GUI.TextField( UILayout.GetRectX( line, 9, 11 ), MaxAngularAcceleration.ToString( "0.0#########" ) ) );
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 5 ), "Drag (L/A):" );
            Drag = float.Parse( GUI.TextField( UILayout.GetRectX( line, 6, 8 ), Drag.ToString( "0.0#########" ) ) );
            AngularDrag = float.Parse( GUI.TextField( UILayout.GetRectX( line, 9, 11 ), AngularDrag.ToString( "0.0#########" ) ) );
            line++;
        }
    }
}
