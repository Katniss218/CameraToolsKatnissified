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
            Instant,
            /// This mode will try to keep the pivot exactly at the specified distance. Maximum speed of travel is defined.
            Velocity
        }

        public Mode _Mode { get; set; } = Mode.Instant;

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
            if( _Mode == Mode.Instant )
            {
                return desiredPosition;
            }

            if( _Mode == Mode.Velocity )
            {
                if( position > desiredPosition )
                {
                    float newPos = position - (Speed * deltaTime);
                    if( newPos < desiredPosition ) // don't overshoot below the desired pos.
                    {
                        newPos = desiredPosition;
                    }
                    return newPos;
                }
                if( position < desiredPosition )
                {
                    float newPos = position + (Speed * deltaTime);
                    if( newPos > desiredPosition ) // don't overshoot above the desired pos.
                    {
                        newPos = desiredPosition;
                    }
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

                Vector3 tgt = this.Controller.CameraTargetWorldSpace == null ? this.Pivot.position : this.Controller.CameraTargetWorldSpace.Value;
                Vector3 posRelativeToTarget = this.Pivot.position - tgt;

                float position = -GetPosition1D( directionToTarget, posRelativeToTarget ); // swap signs because we want distance to be positive behind the ray
                float desiredPosition = Distance;

                float newPosition = Step( position, desiredPosition, Time.fixedDeltaTime );

                // Never be further/closer from target than min/max distance.
                newPosition = Mathf.Clamp( newPosition, MinDistance == null ? float.MinValue : MinDistance.Value, MaxDistance == null ? float.MaxValue : MaxDistance.Value );

                Vector3 newPosRelativeToTarget = GetPosition3D( directionToTarget, -newPosition ); // swap signs because the method wants the position to be negative behind the ray.

                this.Pivot.position = newPosRelativeToTarget + tgt;
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
            Distance = float.Parse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), Distance.ToString( "0.0#########" ) ) );
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "MinDistance:" );
            if( float.TryParse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), MinDistance == null ? null : MinDistance.Value.ToString( "0.0#########" ) ), out float minDist ) )
            {
                MinDistance = minDist;
            }
            else
            {
                MinDistance = null;
            }
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "MaxDistance:" );
            if( float.TryParse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), MaxDistance == null ? null : MaxDistance.Value.ToString( "0.0#########" ) ), out float maxDist ) )
            {
                MaxDistance = maxDist;
            }
            else
            {
                MaxDistance = null;
            }
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 6 ), "Speed:" );
            Speed = float.Parse( GUI.TextField( UILayout.GetRectX( line, 7, 11 ), Speed.ToString( "0.##########" ) ) );
            line++;
        }
    }
}