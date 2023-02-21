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
        public Vector3 TranslationRate { get; set; }


        /// The amount of rotation applied every second (RotationRate * Time.fixedDeltaTime).
        /// Can be zero (identity).
        public Quaternion RotationRate { get; set; }


        /// The reference frame used to apply rotation.
        public FrameOfReference ReferenceFrame { get; set; }


        public Matrix4x4 tsToLocal; // transformation space to local.
        public Quaternion tsRotation; // transformationSpace rotation.


        // This behaviour applies a constant rotation to the pivot.
        // it doesn't follow anything

        public ConstantMovementBehaviour( CameraPlayerController controller ) : base( controller )
        {

        }


        /// Calculate the transformation to apply in this frame in local space of this.Pivot.
        (Vector3 offset, Quaternion rot) GetTransformationLocal()
        {
            throw new NotImplementedException();
        }


        void OnPlayingFixedUpdate()
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
