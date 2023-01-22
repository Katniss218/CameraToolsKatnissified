using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.Cameras
{
    public abstract class CameraBehaviour : MonoBehaviour
    {
        // This class should be a base camera behaviour that you can derive from to make new camera modes.

        protected CameraToolsManager cameraBeh;

        /// <summary>
        /// True if the camera behaviour is currently controlling the camera (playing). False otherwise.
        /// </summary>
        public bool IsPlaying { get; protected set; } = false;

        /// <summary>
        /// Called when the camera behaviour starts playing. Use this to set the initial state.
        /// </summary>
        protected abstract void OnStartPlaying();

        /// <summary>
        /// Called every frame while the camera behaviour is playing.
        /// </summary>
        protected abstract void OnPlaying();

        /// <summary>
        /// Called when the camera behaviour stops playing.
        /// </summary>
        protected abstract void OnStopPlaying();

        /// <summary>
        /// Call this to start playing the camera behaviour.
        /// </summary>
        public void StartPlaying()
        {
            this.enabled = true;
            Debug.Log( "[CTK] StartPlaying was called." );
            IsPlaying = true;
            OnStartPlaying();
        }

        /// <summary>
        /// Call this to stop playing the camera behaviour.
        /// </summary>
        public void StopPlaying()
        {
            this.enabled = false;
            IsPlaying = false;
            OnStopPlaying();
        }


        protected virtual void Awake()
        {
            cameraBeh = this.GetComponent<CameraToolsManager>();
        }

        protected virtual void FixedUpdate()
        {
            if( IsPlaying )
            {
                OnPlaying();

                cameraBeh.LastCameraPosition = cameraBeh.FlightCamera.transform.position; // was in stationary camera only.
                cameraBeh.LastCameraRotation = cameraBeh.FlightCamera.transform.rotation;
            }
        }
    }
}