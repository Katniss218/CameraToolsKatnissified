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
    public class ConstantMovementBehaviour : CameraBehaviour
    {
        public enum FrameOfReference
        {
            /// Use current local space of the camera.
            Camera,
            /// Use the local space of this.Pivot.
            Self,
            /// Use the current local space of the Pivot's parent (local space of itself if root).
            Parent,
            /// Use the space of the vessel calculated when the camera is initialized.
            VesselInit,
            /// Use the current vessel space.
            Vessel
        }


        /// The amount of translation (movement) applied every second (TranslationRate * Time.fixedDeltaTime).
        /// Can be zero.
        public Vector3 TranslationRate { get; set; } = new Vector3( 0, 0, 10 );


        /// The amount of rotation applied every second (RotationRate * Time.fixedDeltaTime).
        /// Can be zero (identity).
        public Quaternion RotationRate { get; set; } = Quaternion.identity;


        /// The reference frame used to apply rotation.
        public FrameOfReference ReferenceFrame { get; set; } = FrameOfReference.Self;


        Matrix4x4 _vLocal2World; // transformation space to local.
        Quaternion _vLocal2WorldRotation; // transformationSpace rotation.


        public ConstantMovementBehaviour( CameraPlayerController controller ) : base( controller )
        {

        }
        // This behaviour applies a constant rotation to the pivot.
        // it doesn't follow anything




        /// Calculate the transformation to apply in this frame in local space of this.Pivot.
        (Vector3 offset, Quaternion rot) GetTransformationLocal()
        {
            if( ReferenceFrame == FrameOfReference.Camera )
            {
                var worldRot = this.Controller.Camera.transform.rotation * RotationRate;
                var localRot = Quaternion.Inverse( this.Pivot.rotation ) * worldRot;
                var worldOffset = this.Controller.Camera.transform.TransformPoint( TranslationRate );
                var localOffset = this.Pivot.InverseTransformPoint( worldOffset );
                return (localOffset, localRot);
            }
            if( ReferenceFrame == FrameOfReference.Self )
            {
                return (TranslationRate, RotationRate);
            }
            if( ReferenceFrame == FrameOfReference.Parent )
            {
                if( this.Pivot.parent == null )
                {
                    return (TranslationRate, RotationRate);
                }
                var worldRot = this.Pivot.parent.rotation * RotationRate;
                var localRot = Quaternion.Inverse( this.Pivot.rotation ) * worldRot;
                var worldOffset = this.Pivot.parent.TransformPoint( TranslationRate );
                var localOffset = this.Pivot.InverseTransformPoint( worldOffset );
                return (localOffset, localRot);
            }
            if( ReferenceFrame == FrameOfReference.VesselInit )
            {
                var worldRot = _vLocal2WorldRotation * RotationRate;
                var localRot = Quaternion.Inverse( this.Pivot.rotation ) * worldRot;
                var worldOffset = _vLocal2World.MultiplyPoint( TranslationRate );
                var localOffset = this.Pivot.InverseTransformPoint( worldOffset );
                return (localOffset, localRot);
            }
            if( ReferenceFrame == FrameOfReference.Vessel )
            {
                var worldRot = this.Ctm.ActiveVessel.ReferenceTransform.rotation * RotationRate;
                var localRot = Quaternion.Inverse( this.Pivot.rotation ) * worldRot;
                var worldOffset = this.Ctm.ActiveVessel.ReferenceTransform.TransformPoint( TranslationRate );
                var localOffset = this.Pivot.InverseTransformPoint( worldOffset );
                return (localOffset, localRot);
            }
            throw new InvalidOperationException( $"Unknown reference frame '{ReferenceFrame}'." );
        }


        protected override void OnStartPlaying()
        {
            _vLocal2World = this.Ctm.ActiveVessel.ReferenceTransform.localToWorldMatrix;
            _vLocal2WorldRotation = this.Ctm.ActiveVessel.ReferenceTransform.rotation;
        }

        protected override void OnStopPlaying()
        {
        }

        public override void FixedUpdate( bool isPlaying )
        {
            if( isPlaying )
            {
                (var offset, var rot) = GetTransformationLocal();


                this.Pivot.localPosition += offset;
                this.Pivot.localRotation *= rot;
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


            GUI.Label( UILayout.GetRectX( line, 1, 2 ), "T. rate:" );
            TranslationRate = new Vector3( float.Parse( GUI.TextField( UILayout.GetRectX( line, 3, 5 ), TranslationRate.x.ToString( "0.0#########" ) ) ), TranslationRate.y, TranslationRate.z );
            TranslationRate = new Vector3( TranslationRate.x, float.Parse( GUI.TextField( UILayout.GetRectX( line, 6, 8 ), TranslationRate.y.ToString( "0.0#########" ) ) ), TranslationRate.z );
            TranslationRate = new Vector3( TranslationRate.x, TranslationRate.y, float.Parse( GUI.TextField( UILayout.GetRectX( line, 9, 11 ), TranslationRate.z.ToString( "0.0#########" ) ) ) );
            line++;

            GUI.Label( UILayout.GetRectX( line, 1, 2 ), "R. rate:" );
            RotationRate = Quaternion.Euler( float.Parse( GUI.TextField( UILayout.GetRectX( line, 3, 5 ), RotationRate.eulerAngles.x.ToString( "0.0#########" ) ) ), RotationRate.eulerAngles.y, RotationRate.eulerAngles.z );
            RotationRate = Quaternion.Euler( RotationRate.eulerAngles.x, float.Parse( GUI.TextField( UILayout.GetRectX( line, 6, 8 ), RotationRate.eulerAngles.y.ToString( "0.0#########" ) ) ), RotationRate.eulerAngles.z );
            RotationRate = Quaternion.Euler( RotationRate.eulerAngles.x, RotationRate.eulerAngles.y, float.Parse( GUI.TextField( UILayout.GetRectX( line, 9, 11 ), RotationRate.eulerAngles.z.ToString( "0.0#########" ) ) ) );
        }
    }
}