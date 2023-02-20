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
        public FlightCamera Camera { get; set; }

        GameObject _camRoot;

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

            _camRoot = new GameObject( "Camera Root" );
            _camRoot.transform.position = Camera.transform.position;
            _camRoot.transform.rotation = Camera.transform.rotation;
            Camera.transform.SetParent( _camRoot.transform );

            this.Pivot = _camRoot.transform;

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

            Destroy( this.Pivot.gameObject );

            IsPlaying = false;

            CameraToolsManager.Instance.LoadSavedAndEnableCamera();

            Destroy( _camRoot );
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
