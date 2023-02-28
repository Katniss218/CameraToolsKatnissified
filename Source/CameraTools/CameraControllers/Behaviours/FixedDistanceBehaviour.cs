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
            // returns position along the vector in one dimension.
            // project the vector
            // length = magnitude, sign = dot.
            Vector3 newVector = Vector3.Project( position, dir );
            if( Vector3.Dot( dir, newVector.normalized ) < 0 )
            {
                return -newVector.magnitude;
            }
            return newVector.magnitude;
        }

        private Vector3 GetPosition3D( Vector3 dir, float position )
        {
            // returns the position along the direction vector.
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

            if( _Mode == Mode.Velocity )
            {
                float relativePosition = desiredPosition - position;
                if( relativePosition > 0 ) // current position too high.
                {
                    float newPos = position - (Speed * deltaTime);
                    if( newPos < desiredPosition ) // don't overshoot below the desired pos.
                    {
                        newPos = desiredPosition;
                    }
                    return newPos;
                }
                if( relativePosition < 0 ) // current position too low.
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

                Vector3 pivotPos = this.Pivot.position;
                Vector3 targetPos = this.Controller.CameraTargetWorldSpace == null ? pivotPos : this.Controller.CameraTargetWorldSpace.Value;
                Vector3 dir = this.Controller.CameraTargetWorldSpace == null ? this.Pivot.forward : (targetPos - pivotPos).normalized;
                // project position onto vector from parent to target. (immediately snaps the camera inline).

                float position = GetPosition1D( dir, pivotPos );
                float desiredPosition = Distance;

                float newPosition = Step( position, desiredPosition, Time.fixedDeltaTime );

                // Never be further/closer from target than min/max distance.
                newPosition = Mathf.Clamp( newPosition, MinDistance == null ? float.MinValue : MinDistance.Value, MaxDistance == null ? float.MaxValue : MaxDistance.Value );

#warning TODO - doesn't work.
                Vector3 newWorldPos = GetPosition3D( dir, newPosition );

                this.Pivot.position = newWorldPos;
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
