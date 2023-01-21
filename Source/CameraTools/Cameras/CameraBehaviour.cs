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

        bool isPlaying;

        protected abstract void OnStart();
        protected abstract void OnUpdate();

        protected virtual void Awake()
        {
            cameraBeh = this.GetComponent<CameraToolsBehaviour>();
        }

        public void StartPlaying()
        {
            isPlaying = true;
            OnStart();
        }

        protected virtual void FixedUpdate()
        {
            if( isPlaying )
            {
                OnUpdate();
            }
        }
    }
}