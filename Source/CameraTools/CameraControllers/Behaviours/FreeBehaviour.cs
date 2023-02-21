using CameraToolsKatnissified.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.CameraControllers.Behaviours
{
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


        /// Every frame, the _velocity and _angularVelocity are multiplied with this.
        public float DragMultiplier { get; set; }




        Vector3 _velocityWS;
        Quaternion _angularVelocityWS;

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


        public override void FixedUpdate( bool onPlaying )
        {
            /*if( Input.GetKey( KeyCode.RightShift ) )
            {
                // left = translate
                // right = pan
            }
            else // no key
            {
                // left = move along forward and backward.
                // right = orbit
            }*/


            // scroll wheel zooms you in and out.
            // - as a multiplier to the normal Zoom value.
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
