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
    [DisallowMultipleComponent]
    public sealed class FollowVesselBehaviour : CameraBehaviour
    {
        public enum FrameOfReference
        {
            /// Use surface velocity for accumulatedOffset
            Surface,
            /// Use orbital velocity for accumulatedOffset
            Orbit
        }

        public enum ConstraintMode
        {
            /// <summary>
            /// Don't constraint the camera movement.
            /// </summary>
            None,
            /// <summary>
            /// Move accumulatedOffset along the prograde-retrograde axis only.
            /// </summary>
            Prograde,
            /// <summary>
            /// Move accumulatedOffset along the normal-antinormal axis only.
            /// </summary>
            Normal,
            /// <summary>
            /// Move accumulatedOffset along the radial-antiradial axis only.
            /// </summary>
            Radial,
            /// <summary>
            /// Move accumulatedOffset along the gravity vector axis only.
            /// </summary>
            GravityVector,
            /// <summary>
            /// Move accumulatedOffset along the vessel's left-right axis only.
            /// </summary>
            VesselX,
            /// <summary>
            /// Move accumulatedOffset along the vessel's up-down axis only.
            /// </summary>
            VesselY,
            /// <summary>
            /// Move accumulatedOffset along the vessel's forward-back axis only.
            /// </summary>
            VesselZ
        }

        /// Specifies what to add to the accumulatedOffset.
        public FrameOfReference ReferenceFrame { get; set; }

        /// Constraint removes the components of the accumulatedOffset that are not along a specified vector before applying.
        public ConstraintMode Constraint { get; set; }

        /// If True, it's gonna add the initial velocity (in a given frame of reference) to the accumulatedOffset on startup.
        public bool UseInitialVelocity { get; set; }

        /// If true, it'll make the accumulatedOffset apply in the opposite direction.
        public bool ReverseDirection { get; set; }

        /// The maximum velocity relative to the vessel that the accumulatedoffset can have.
        /// After all the constraints have been applied.
        /// Cannot take negative numbers.
        public float MaxRelativeVelocity { get; set; } = 250;

        Vector3 _upDir = Vector3.up;

        // Used for the Initial Velocity camera mode.
        Vector3 _initialSurfaceVelocity;
        Orbit _initialOrbit;

        Vector3 _initialOffset = Vector3.zero; // The offset between the vessel and the camera on initialization. Alternative for initial position.
        Vector3 _accumulatedOffset = Vector3.zero;

        public FollowVesselBehaviour( CameraPlayerController controller ) : base( controller )
        {

        }

        protected override void OnStartPlaying()
        {
            Debug.Log( $"Started playing {nameof( FollowVesselBehaviour )}" );

            if( FlightGlobals.ActiveVessel == null )
            {
                Debug.Log( $"[CameraToolsKatnissified] {nameof( FollowVesselBehaviour )} failed. Active Vessel is null." );
            }

            if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( Ctm.ActiveVessel ) == FlightCamera.Modes.ORBITAL) )
            {
                _upDir = Vector3.up;
            }
            else
            {
                _upDir = -FlightGlobals.getGeeForceAtPosition( Ctm.ActiveVessel.GetWorldPos3D() ).normalized;
            }

            _initialOffset = this.Pivot.position - Ctm.ActiveVessel.transform.position;
            _accumulatedOffset = Vector3.zero;
            _initialSurfaceVelocity = Ctm.ActiveVessel.srf_velocity;

            _initialOrbit = new Orbit();
            _initialOrbit.UpdateFromStateVectors( Ctm.ActiveVessel.orbit.pos, Ctm.ActiveVessel.orbit.vel, FlightGlobals.currentMainBody, Planetarium.GetUniversalTime() );
            //_initialUT = Planetarium.GetUniversalTime();
        }

        protected override void OnStopPlaying()
        {

        }

        public override void FixedUpdate( bool isPlaying )
        {
            if( !isPlaying )
            {
                return;
            }

            if( Ctm.ActiveVessel != null )
            {
                // Parent follows the vessel.
                this.Pivot.position = Ctm.ActiveVessel.transform.position + _initialOffset;

                Vector3 cameraVelocity = Vector3.zero;

                // Camera itself accumulates the inverse of the vessel movement.
                if( ReferenceFrame == FrameOfReference.Surface )
                {
                    cameraVelocity -= Ctm.ActiveVessel.srf_velocity;
                }
                else if( ReferenceFrame == FrameOfReference.Orbit )
                {
                    cameraVelocity -= Ctm.ActiveVessel.obt_velocity;
                }

                if( UseInitialVelocity )
                {
                    if( ReferenceFrame == FrameOfReference.Surface )
                    {
                        cameraVelocity += _initialSurfaceVelocity;
                    }
                    else if( ReferenceFrame == FrameOfReference.Orbit )
                    {
                        // this will desync if followed long enough.
                        cameraVelocity += _initialOrbit.getOrbitalVelocityAtUT( Planetarium.GetUniversalTime() ).xzy;
                    }
                }

                if( Constraint != ConstraintMode.None )
                {
                    Vector3 constraintAxis = Vector3.zero;

                    if( Constraint == ConstraintMode.Prograde )
                    {
                        if( ReferenceFrame == FrameOfReference.Surface )
                        {
                            constraintAxis = Ctm.ActiveVessel.srf_velocity; // Surface prograde
                        }
                        else if( ReferenceFrame == FrameOfReference.Orbit )
                        {
                            constraintAxis = Ctm.ActiveVessel.orbit.Prograde( Planetarium.GetUniversalTime() );
                        }
                    }
                    if( Constraint == ConstraintMode.Normal )
                    {
                        if( ReferenceFrame == FrameOfReference.Surface )
                        {
                            constraintAxis = Ctm.ActiveVessel.mainBody.RotationAxis; // surface Normal
                        }
                        else if( ReferenceFrame == FrameOfReference.Orbit )
                        {
                            constraintAxis = Ctm.ActiveVessel.orbit.Normal( Planetarium.GetUniversalTime() );
                        }
                    }
                    if( Constraint == ConstraintMode.Radial )
                    {
                        if( ReferenceFrame == FrameOfReference.Surface )
                        {
                            constraintAxis = Ctm.ActiveVessel.upAxis; // surface radialIn
                        }
                        else if( ReferenceFrame == FrameOfReference.Orbit )
                        {
                            constraintAxis = Ctm.ActiveVessel.orbit.Radial( Planetarium.GetUniversalTime() );
                        }
                    }
                    /*if( Constraint == ConstraintMode.ThrustVector ) removed because too laggy.
                    {
                        Vector3 vectorWorldSpace = Vector3.zero;
                        var engines = Ctm.ActiveVessel.GetAllEngines();
                        foreach( var engine in engines )
                        {
                            for( int i = 0; i < engine.thrustTransforms.Count; i++ )
                            {
                                vectorWorldSpace += engine.thrustTransforms[i].forward * engine.thrustTransformMultipliers[i];
                            }
                        }
                        if( vectorWorldSpace != Vector3.zero )
                        {
                            vectorWorldSpace.Normalize();
                        }
                        constraintVector = vectorWorldSpace;
                    }*/
                    if( Constraint == ConstraintMode.GravityVector )
                    {
                        constraintAxis = FlightGlobals.getGeeForceAtPosition( Ctm.ActiveVessel.GetWorldPos3D() ).normalized;
                    }
                    if( Constraint == ConstraintMode.VesselX )
                    {
                        constraintAxis = Ctm.ActiveVessel.transform.right;
                    }
                    if( Constraint == ConstraintMode.VesselY )
                    {
                        constraintAxis = Ctm.ActiveVessel.transform.up;
                    }
                    if( Constraint == ConstraintMode.VesselZ )
                    {
                        constraintAxis = Ctm.ActiveVessel.transform.forward;
                    }

                    if( constraintAxis != Vector3.zero ) // keep the component pointing along the constraint vector.
                    {
                        cameraVelocity = Vector3.Project( cameraVelocity, constraintAxis.normalized );
                    }
                }

                if( cameraVelocity.magnitude > MaxRelativeVelocity )
                {
                    cameraVelocity = cameraVelocity.normalized * MaxRelativeVelocity;
                }

                if( ReverseDirection )
                {
                    _accumulatedOffset -= cameraVelocity * Time.fixedDeltaTime;
                }
                else
                {
                    _accumulatedOffset += cameraVelocity * Time.fixedDeltaTime;
                }

                this.Pivot.position += _accumulatedOffset;
            }
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

            GUI.Label( UILayout.GetRectX( line, 1, 9 ), $"Constraint: {Constraint}" );
            if( GUI.Button( UILayout.GetRect( 10, line ), "<" ) )
            {
                Constraint = Misc.CycleEnum( Constraint, -1 );
            }
            if( GUI.Button( UILayout.GetRect( 11, line ), ">" ) )
            {
                Constraint = Misc.CycleEnum( Constraint, 1 );
            }
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 4 ), "Max Rel. V:" );
            MaxRelativeVelocity = float.Parse( GUI.TextField( UILayout.GetRectX( line, 5, 11 ), MaxRelativeVelocity.ToString( "0.0#########" ) ) );
            if( MaxRelativeVelocity < 0 )
            {
                MaxRelativeVelocity = 0;
            }
            line++;

            ReverseDirection = GUI.Toggle( UILayout.GetRectX( line, 1, 11 ), ReverseDirection, "Reverse Direction" );
            line++;

            UseInitialVelocity = GUI.Toggle( UILayout.GetRectX( line, 1, 11 ), UseInitialVelocity, "Use Init. Velocity" );
        }
    }
}