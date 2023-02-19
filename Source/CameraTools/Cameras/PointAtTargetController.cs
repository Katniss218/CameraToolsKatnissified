using CameraToolsKatnissified.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.Cameras
{
    [DisallowMultipleComponent]
    public sealed class PointAtTargetController : CameraController
    {
        public Part Target { get; private set; } = null;

        public Vector3 UpDirection { get; set; } = Vector3.up;

        bool _settingTargetEnabled;

        public PointAtTargetController( CameraToolsManager ctm ) : base( ctm )
        {

        }

        public override void Update()
        {
            // Set target from a mouse raycast.
            if( _settingTargetEnabled && cameraBeh._wasMouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                _settingTargetEnabled = false;

                Part newTarget = Utils.GetPartFromMouse();
                if( newTarget != null )
                {
                    Target = newTarget;
                }
            }
        }

        protected override void OnStartPlaying()
        {
            if( FlightGlobals.ActiveVessel != null )
            {
                if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( cameraBeh.ActiveVessel ) == FlightCamera.Modes.ORBITAL) )
                {
                    UpDirection = Vector3.up;
                }
                else
                {
                    UpDirection = -FlightGlobals.getGeeForceAtPosition( cameraBeh.ActiveVessel.GetWorldPos3D() ).normalized;
                }
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }
        }

        protected override void OnPlayingFixedUpdate()
        {
            if( cameraBeh.FlightCamera.Target != null )
            {
                cameraBeh.FlightCamera.SetTargetNone(); //dont go to next vessel if vessel is destroyed
            }

            if( Target != null )
            {
                Vector3 toTargetDirection = (Target.transform.position - this.Pivot.transform.position).normalized;

                this.Pivot.transform.rotation = Quaternion.LookRotation( toTargetDirection, UpDirection );
            }

            float fov = 60 / (Mathf.Exp( cameraBeh.Zoom ) / Mathf.Exp( 1 ));
            if( cameraBeh.FlightCamera.FieldOfView != fov )
            {
                cameraBeh.FlightCamera.SetFoV( fov );
            }
        }

        protected override void OnStopPlaying()
        {

        }

        public override void DrawGui( UILayout UILayout, ref int line )
        {
            GUI.Label( UILayout.GetRectX( line, 1, 7 ), $"Target: {(Target == null ? "None" : Target.gameObject.name)}" );
            if( GUI.Button( UILayout.GetRectX( line, 8, 9 ), _settingTargetEnabled ? "..." : "S" ) )
            {
                _settingTargetEnabled = true;
                cameraBeh._wasMouseUp = false;
            }
            if( GUI.Button( UILayout.GetRectX( line, 10, 11 ), "X" ) )
            {
                Target = null;
            }
        }
    }
}