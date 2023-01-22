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
        protected CameraToolsBehaviour cameraBeh;

        protected bool isPlaying;

        public bool IsPlaying { get => isPlaying; }

        protected abstract void OnStartPlaying();
        protected abstract void OnPlaying();
        protected abstract void OnStopPlaying();

        protected virtual void Awake()
        {
            cameraBeh = this.GetComponent<CameraToolsBehaviour>();
        }

        public void StartPlaying()
        {
            this.enabled = true;
            Debug.Log( "[CTK] StartPlaying was called." );
            isPlaying = true;
            OnStartPlaying();
        }

        public void StopPlaying()
        {
            this.enabled = false;
            isPlaying = false;
            OnStopPlaying();
        }

        protected virtual void FixedUpdate()
        {
            if( isPlaying )
            {
                OnPlaying();
            }
        }
    }
}