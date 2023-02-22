using CameraToolsKatnissified.UI;
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
            CameraLocal,
            /// Use the local space of this.Pivot.
            SelfLocal,
            /// Use the current local space of the Pivot's parent (local space of itself if root).
            ParentLocal,
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
        public FrameOfReference ReferenceFrame { get; set; } = FrameOfReference.SelfLocal;


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
            if( ReferenceFrame == FrameOfReference.CameraLocal )
            {
                var worldRot = this.Controller.Camera.transform.rotation * RotationRate;
                var localRot = Quaternion.Inverse( this.Pivot.rotation ) * worldRot;
                var worldOffset = this.Controller.Camera.transform.TransformPoint( TranslationRate );
                var localOffset = this.Pivot.InverseTransformPoint( worldOffset );
                return (localOffset, localRot);
            }
            if( ReferenceFrame == FrameOfReference.SelfLocal )
            {
                return (TranslationRate, RotationRate);
            }
            if( ReferenceFrame == FrameOfReference.ParentLocal )
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
        }
    }
}