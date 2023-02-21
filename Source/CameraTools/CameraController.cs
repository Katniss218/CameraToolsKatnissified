using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified
{
    public abstract class CameraController : MonoBehaviour
    {
        public float Zoom { get; set; } = 1.0f;

        public FlightCamera Camera { get; set; }

        public bool IsPlaying { get; private set; }

        private Transform _pivot;
        public Transform Pivot
        {
            get
            {
                if( !IsPlaying )
                    throw new InvalidOperationException( "Can't get Pivot of CameraController that is not playing. Pivots only get created when the controller starts playing." );
                return _pivot;
            }
            private set
            {
                _pivot = value;
            }
        }


        /// <summary>
        /// Starts "playing".
        /// </summary>
        public void StartPlaying()
        {
            if( IsPlaying )
            {
                throw new InvalidOperationException( "The camera controller is already playing." );
            }

            CameraToolsManager.Instance.SaveAndDisableCamera();

            GameObject camRoot;
            camRoot = new GameObject( "Camera Root" );
            camRoot.transform.position = Camera.transform.position;
            camRoot.transform.rotation = Camera.transform.rotation;
            Camera.transform.SetParent( camRoot.transform );

            this.Pivot = camRoot.transform;

            IsPlaying = true;

            OnStartPlaying();
        }

        protected abstract void OnStartPlaying();

        /// <summary>
        /// Ends "playing".
        /// </summary>
        public void EndPlaying()
        {
            if( !IsPlaying )
            {
                throw new InvalidOperationException( "The camera controller is not playing." );
            }

            OnEndPlaying();

            CameraToolsManager.Instance.LoadSavedAndEnableCamera();
            // Apparently order at which this happens matters.
            Destroy( this.Pivot.gameObject );

            IsPlaying = false;
        }

        protected abstract void OnEndPlaying();


        public static CameraController Attach<T>( FlightCamera camera ) where T : CameraController
        {
            T controller = CameraToolsManager.Instance.gameObject.AddComponent<T>();
            controller.Pivot = null;
            controller.Camera = camera;

            return controller;
        }

        public static void Detach( CameraController c )
        {
            Destroy( c );
        }
    }
}
