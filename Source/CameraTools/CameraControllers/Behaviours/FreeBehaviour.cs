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
        public int SmoothLength { get; set; }


        /// Multiplier for scroll wheel input.
        public float ScrollSensitivity { get; set; }
        /// Multiplier for mouse input.
        public float MouseSensitivity { get; set; }


        /// Multiplier for keyboard input.
        public float KeyboardSensitivity { get; set; }




        public float MaxAcceleration { get; set; }
        /// Every frame, the _velocity and _angularVelocity are multiplied with this.
        public float Drag { get; set; }
        public float AngularDrag { get; set; }


        bool _isOrbiting;


        Vector3 _velocityWS;
        Quaternion _angularVelocityWS;
        float _scrollVelocity;


        InputBuffer _mouseXBuffer;
        InputBuffer _mouseYBuffer;
        InputBuffer _scrollWheelBuffer;
        InputBuffer _LeftArrowBuffer;
        InputBuffer _RightArrowBuffer;
        InputBuffer _UpArrowBuffer;
        InputBuffer _DownArrowBuffer;

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


        void ApplySmoothLength()
        {
            _mouseXBuffer = new InputBuffer( this.SmoothLength );
            _mouseYBuffer = new InputBuffer( this.SmoothLength );
            _scrollWheelBuffer = new InputBuffer( this.SmoothLength );
            _LeftArrowBuffer = new InputBuffer( this.SmoothLength );
            _RightArrowBuffer = new InputBuffer( this.SmoothLength );
            _UpArrowBuffer = new InputBuffer( this.SmoothLength );
            _DownArrowBuffer = new InputBuffer( this.SmoothLength );
        }


        public override void Update( bool onPlaying )
        {
            _mouseXBuffer.Add( Input.GetAxis( "Mouse Horizontal" ) );
            _mouseYBuffer.Add( Input.GetAxis( "Mouse Vertical" ) );
            // ...
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


        public override void FixedUpdate( bool onPlaying )
        {
            float mouseX = _mouseXBuffer.GetAverage();
            float mouseY = _mouseYBuffer.GetAverage();
            // ...


            // If the player starts orbiting and lets go,
            // then the camera continues moving tangentially with the velocity it had before.
            if( Input.GetKey( KeyCode.Mouse1 ) )
            {
                if( Input.GetKey( KeyCode.Mouse2 ) )
                {
                    // pan.
                }
                else
                {
                    if( this.Controller.CameraTargetWorldSpace != null )
                    {
                        if( !_isOrbiting )
                        {
                            // get angular velocity from velocity and distance.
                            this._angularVelocityWS = GetAngularVelocity( this._velocityWS, this.Pivot.position - this.Controller.CameraTargetWorldSpace.Value );
                            this._velocityWS = Vector3.zero;
                        }
                        // orbit.

                        _isOrbiting = true;
                    }
                }
            }
            else
            {
                if( _isOrbiting )
                {
                    if( this.Controller.CameraTargetWorldSpace != null )
                    {
                        // convert orbital angular velocity to normal velocity.
                        // rotational (angular) velocity remains so that it's not abruptly stopping.
                        this._velocityWS = GetVelocity( this._angularVelocityWS, this.Pivot.position - this.Controller.CameraTargetWorldSpace.Value );
                    }

                    _isOrbiting = false;
                }


                if( Input.GetKey( KeyCode.Mouse2 ) )
                {
                    // xy movement.




                }
            }


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

        protected override void OnStartPlaying()
        {
        }

        protected override void OnStopPlaying()
        {
        }

        public override void DrawGui( UILayout UILayout, ref int line )
        {
        }
    }

}
