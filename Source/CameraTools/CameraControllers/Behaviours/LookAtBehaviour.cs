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
    public sealed class LookAtBehaviour : CameraBehaviour
    {
        public enum UpDirection
        {
            /// Use the inverse of the gravity vector for the up direction.
            Gravity,
            /// Use the parent object's up direction for the up direction.
            /// World's up if parent is null.
            Parent
        }

        public Transform Target { get; private set; }

        public UpDirection Up { get; set; }

        /// If true, it will auto-zoom to the target.
        public bool UseZoom { get; set; }

        /// How big the object should appear.
        public float ZoomAngularSize { get; set; } = 5.0f;

        /// If 1, the angular size will be constant, if 0, the zoom will not change with distance. Values in-between use lerp?
        public float ZoomDistanceFactor { get; set; }

        Vector3 _upDir = Vector3.up;

        bool _settingTargetEnabled;

        public LookAtBehaviour( CameraPlayerController controller ) : base( controller )
        {

        }

        protected override void OnStartPlaying()
        {
            Debug.Log( $"Started playing {nameof( LookAtBehaviour )}" );

            if( FlightGlobals.ActiveVessel != null )
            {
                if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( Ctm.ActiveVessel ) == FlightCamera.Modes.ORBITAL) )
                {
                    _upDir = Vector3.up;
                }
                else
                {
                    _upDir = -FlightGlobals.getGeeForceAtPosition( Ctm.ActiveVessel.GetWorldPos3D() ).normalized;
                }
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }
        }

        protected override void OnStopPlaying()
        {

        }

        public override void Update( bool isPlaying )
        {
            // Set target from a mouse raycast.
            if( _settingTargetEnabled && Ctm._wasMouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                _settingTargetEnabled = false;

                Part newTarget = Misc.GetPartFromMouse();
                if( newTarget != null )
                {
                    Target = newTarget.transform;
                }
            }
        }

        public override void FixedUpdate( bool isPlaying )
        {
            if( !isPlaying )
            {
                return;
            }

            // if( Ctm.FlightCamera.Target != null )
            // {
            //     Ctm.FlightCamera.SetTargetNone(); //dont go to next vessel if vessel is destroyed
            // }

            if( Target != null )
            {
                Vector3 toTargetDirection = (Target.position - this.Pivot.position).normalized;

                this.Pivot.rotation = Quaternion.LookRotation( toTargetDirection, _upDir );
                this.Controller.CameraTargetWorldSpace = Target.position;

                if( UseZoom )
                {
                    const float NORMAL_ZOOM = 1.0f;

                    float cameraDistance = Vector3.Distance( Target.position, this.Pivot.position );

                    float targetFoV = GetFovToFixSize( cameraDistance, (ZoomAngularSize / 10000.0f) );

                    float zoom = Mathf.Log( 60 / targetFoV ) + 1;

                    float zoom2 = Mathf.Lerp( NORMAL_ZOOM, zoom, ZoomDistanceFactor ); // depending on the zoomDistanceFactor, blend a different amount of the normal FoV, and the autozoom.

                    Controller.Zoom = zoom2;
                }
            }
        }

        /// <summary>
        /// Returns the FoV needed to make an object appear a constant angular size.
        /// </summary>
        /// <param name="fixedSize">Controls the size of the object.</param>
        public static float GetFovToFixSize( float distance, float fixedSize )
        {
            const float SCALE = 1.0f;
            float fov = SCALE / (distance * fixedSize);

            return fov;
        }

        public override void DrawGui( UILayout UILayout, ref int line )
        {
            GUI.Label( UILayout.GetRectX( line, 1, 7 ), $"Target: {(Target == null ? "None" : Target.gameObject.name)}" );
            if( GUI.Button( UILayout.GetRectX( line, 8, 9 ), _settingTargetEnabled ? "..." : "S" ) )
            {
                _settingTargetEnabled = true;
                Ctm._wasMouseUp = false;
            }
            if( GUI.Button( UILayout.GetRectX( line, 10, 11 ), "X" ) )
            {
                Target = null;
            }
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 9 ), $"Up: {Up}" );
            if( GUI.Button( UILayout.GetRect( 10, line ), "<" ) )
            {
                Up = Misc.CycleEnum( Up, -1 );
            }
            if( GUI.Button( UILayout.GetRect( 11, line ), ">" ) )
            {
                Up = Misc.CycleEnum( Up, 1 );
            }
            line++;

            UseZoom = GUI.Toggle( UILayout.GetRectX( line ), UseZoom, "Use Auto-Zoom" );
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 5 ), "Zoom Angular Size:" );
            ZoomAngularSize = float.Parse( GUI.TextField( UILayout.GetRectX( line, 6, 11 ), ZoomAngularSize.ToString() ) );
            if( ZoomAngularSize < 1 )
            {
                ZoomAngularSize = 1;
            }
            line++;
            GUI.Label( UILayout.GetRectX( line, 1, 5 ), "Zoom Dist. Factor:" );
            ZoomDistanceFactor = GUI.HorizontalSlider( UILayout.GetRectX( line, 6, 11 ), ZoomDistanceFactor, 0, 1 );
        }
    }
}