using CameraToolsKatnissified.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.CameraControllers.Behaviours
{
    public class FixedDistanceBehaviour : CameraBehaviour
    {
        public enum Mode
        {
            /// This mode will keep the pivot exactly at the specified distance.
            Fixed,
            /// This mode will try to keep the pivot exactly at the specified distance. Maximum speed of travel is defined.
            Ease
        }


        public Mode _Mode { get; set; }


        /// The preferred distance from target.
        public float Distance { get; set; } = 100.0f;
        /// The minimum distance from target, which will never be exceeded. Can be null.
        public float? MinDistance { get; set; }
        /// Maximum distance from target, which will never be exceeded. Can be null.
        public float? MaxDistance { get; set; }


        /// The behaviour will limit its movement speed to [0..Speed] per second (Speed * Time.fixedDeltaTime).
        public float Speed { get; set; }

        public FixedDistanceBehaviour( CameraPlayerController controller ) : base( controller )
        {

        }

        protected override void OnStartPlaying()
        {
        }

        protected override void OnStopPlaying()
        {
        }

        public override void FixedUpdate( bool isPlaying )
        {
            if( isPlaying )
            {
                if( this.Pivot.parent == null )
                {
                    return;
                }


                // project position onto vector from parent to target. (immediately snaps the camera inline).


                Vector3 dir;
                float desiredDisplacementFromParent;
                float currentDisplacementFromParent = this.Pivot.localPosition.magnitude;
                if( this.Controller.CameraTargetWorldSpace == null )
                {
                    desiredDisplacementFromParent = -Distance; // negative moves back.
                    dir = this.Pivot.forward;
                }
                else
                {
                    Vector3 targetPos = this.Controller.CameraTargetWorldSpace.Value;
                    float targetDist = Vector3.Distance( targetPos, this.Pivot.parent.position );
                    dir = targetPos - this.Pivot.parent.position;

                    desiredDisplacementFromParent = targetDist - Distance; // negative moves back.
                }
                // remaining = full - current as percentage of what is desired.
                //float remainingDisplacementPercentage = 1 - (currentDisplacementFromParent / desiredDisplacementFromParent);
#warning TODO - the camera goes away to infinity slowly when the parent lies too close.

                float remainingDisplacement = desiredDisplacementFromParent - currentDisplacementFromParent;
                //

                Debug.Log( $"desiredDisplacementFromParent {desiredDisplacementFromParent}\n currentDisplacementFromParent {currentDisplacementFromParent}\n remainingDisplacement {remainingDisplacement}" );

                dir.Normalize();

                Vector3 displaceWorld = dir * desiredDisplacementFromParent;

                this.Pivot.position = this.Pivot.parent.position + displaceWorld;



                // clamp in range of min to max distance.
            }
        }


        public override void DrawGui( UILayout UILayout, ref int line )
        {
        }




        // This behaviour is supposed to keep its transform at a constant distance from the camera target.


        // The axis of offset is determined by the parent's position and the target position.
        // If parent is null, it offsets along the forward axis.
    }

}
