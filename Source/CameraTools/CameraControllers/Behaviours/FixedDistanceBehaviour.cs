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
    public class FixedDistanceBehaviour : CameraBehaviour
    {
        public enum Mode
        {
            /// This mode will keep the pivot exactly at the specified distance.
            Constant,
            /// This mode will try to keep the pivot exactly at the specified distance. Maximum speed of travel is defined.
            Velocity
        }

        public Mode _Mode { get; set; }

        /// The preferred distance from target.
        public float Distance { get; set; } = 100.0f;
        /// The minimum distance from target, which will never be exceeded. Can be null.
        public float? MinDistance { get; set; } = null;
        /// Maximum distance from target, which will never be exceeded. Can be null.
        public float? MaxDistance { get; set; } = null;

        /// The behaviour will limit its movement speed to [0..Speed] per second (Speed * Time.fixedDeltaTime).
        public float Speed { get; set; } = 20.0f;

        public FixedDistanceBehaviour( CameraPlayerController controller ) : base( controller )
        {

        }

        private float GetPosition1D( Vector3 dir, Vector3 position )
        {
            // Returns the distance along a ray of a position projected onto the ray.

            Vector3 newVector = Vector3.Project( position, dir );
            if( Vector3.Dot( dir, newVector.normalized ) < 0 )
            {
                return -newVector.magnitude;
            }
            return newVector.magnitude;
        }

        private Vector3 GetPosition3D( Vector3 dir, float position )
        {
            // Returns the position at a certain distance along a ray.
            return dir * position;
        }

        private float Step( float position, float desiredPosition, float deltaTime )
        {
            // positive => closer to target than pivot.
            // negative => closer to pivot than target.
            // zero => at pivot.

            // returns the new position.
            if( _Mode == Mode.Constant )
            {
                return desiredPosition;
            }

            Debug.Log( $"desiredPosition: {desiredPosition}" );
            if( _Mode == Mode.Velocity )
            {
                float DistanceRemaining = desiredPosition - position;
                if( DistanceRemaining > 0 ) // current position too high.
                {
                    Debug.Log( $"remaining: {DistanceRemaining}" );
                    float newPos = position - (Speed * deltaTime);
                    if( newPos < desiredPosition ) // don't overshoot below the desired pos.
                    {
                        newPos = desiredPosition;
                    }
                    Debug.Log( $"newPos: {newPos}" );
                    return newPos;
                }
                if( DistanceRemaining < 0 ) // current position too low.
                {
                    Debug.Log( $"remaining: {DistanceRemaining}" );
                    float newPos = position + (Speed * deltaTime);
                    if( newPos > desiredPosition ) // don't overshoot above the desired pos.
                    {
                        newPos = desiredPosition;
                    }
                    Debug.Log( $"newPos: {newPos}" );
                    return newPos;
                }
                return desiredPosition; // Already at the target.
            }
            throw new InvalidOperationException( "Invalid mode" );
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

                Vector3 directionToTarget = this.Controller.CameraTargetWorldSpace == null ? this.Pivot.forward : (this.Controller.CameraTargetWorldSpace.Value - this.Pivot.parent.position).normalized;
                // origin at target, away from pivot.
                // project position onto vector from parent to target. (immediately snaps the camera inline).

                // Position = distance along the axis from the target.
                //  - Positive - target in front of camera.
                //  - Negative - target behind camera.
#warning TODO - distance seems to overshoot

                Vector3 posRelativeToTarget = this.Pivot.parent.position - this.Controller.CameraTargetWorldSpace.Value;

                float position = -GetPosition1D( directionToTarget, posRelativeToTarget ); // swap signs because we want distance to be positive behind the ray
                float desiredPosition = Distance;

                float newPosition = Step( position, desiredPosition, Time.fixedDeltaTime );

                // Never be further/closer from target than min/max distance.
                newPosition = Mathf.Clamp( newPosition, MinDistance == null ? float.MinValue : MinDistance.Value, MaxDistance == null ? float.MaxValue : MaxDistance.Value );

                Vector3 newPosRelativeToTarget = GetPosition3D( directionToTarget, -newPosition ); // swap signs because the method wants the position to be negative behind the ray.

                this.Pivot.position = newPosRelativeToTarget + this.Controller.CameraTargetWorldSpace.Value;
            }
        }

        public override void DrawGui( UILayout UILayout, ref int line )
        {
            GUI.Label( UILayout.GetRectX( line, 1, 9 ), $"_Mode: {_Mode}" );
            if( GUI.Button( UILayout.GetRect( 10, line ), "<" ) )
            {
                _Mode = Misc.CycleEnum( _Mode, -1 );
            }
            if( GUI.Button( UILayout.GetRect( 11, line ), ">" ) )
            {
                _Mode = Misc.CycleEnum( _Mode, 1 );
            }
            line++;
            // button cycleenum. Mode

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "Distance:" );
            Distance = float.Parse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), Distance.ToString() ) );
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "MinDistance:" );
            if( float.TryParse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), MinDistance.ToString() ), out float minDist ) )
            {
                MinDistance = minDist;
            }
            else
            {
                MinDistance = null;
            }
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "MaxDistance:" );
            if( float.TryParse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), MaxDistance.ToString() ), out float maxDist ) )
            {
                MaxDistance = maxDist;
            }
            else
            {
                MinDistance = null;
            }
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "Speed:" );
            Speed = float.Parse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), Speed.ToString() ) );
            line++;
        }

        // This behaviour is supposed to keep its transform at a constant distance from the camera target.


        // The axis of offset is determined by the parent's position and the target position.
        // If parent is null, it offsets along the forward axis.
    }
}
