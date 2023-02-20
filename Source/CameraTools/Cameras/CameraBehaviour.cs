using CameraToolsKatnissified.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.Cameras
{
    public abstract class CameraBehaviour
    {
        // This class should be a base camera behaviour that you can derive from to make new camera modes.

        protected CameraToolsManager Ctm { get; private set; }

        public CameraBehaviour()
        {
            Ctm = CameraToolsManager.Instance;
        }

        public Transform Pivot { get; private set; }

        public void SetTransform( Transform pivot )
        {
            Pivot = pivot;
        }

        /// <summary>
        /// Called when the camera behaviour starts playing. Use this to set the initial state.
        /// </summary>
        protected abstract void OnStartPlaying();

        /// <summary>
        /// Called when the camera behaviour stops playing.
        /// </summary>
        protected abstract void OnStopPlaying();

        public virtual void OnGUI() { }

        public abstract void DrawGui( UILayout UILayout, ref int line );

        public virtual void OnLoad( ConfigNode node )
        {

        }

        public virtual void OnSave( ConfigNode node )
        {

        }

        /// <summary>
        /// Call this to start playing the camera behaviour.
        /// </summary>
        public void StartPlaying()
        {
            Debug.Log( "[CameraToolsKatnissified] StartPlaying was called." );
            OnStartPlaying();
        }

        /// <summary>
        /// Call this to stop playing this camera behaviour.
        /// </summary>
        public void StopPlaying()
        {
            Debug.Log( "[CameraToolsKatnissified] StopPlaying was called." );
            OnStopPlaying();
        }

        public virtual void Update( bool isPlaying )
        {

        }

        public virtual void FixedUpdate( bool isPlaying )
        {

        }

        // -------

        static Type[] cachedCameraTypes;

        private static void CacheBehaviours()
        {
            Type cameraBehaviourType = typeof( CameraBehaviour );
            cachedCameraTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany( a => a.GetTypes() )
                .Where( t => t != cameraBehaviourType && cameraBehaviourType.IsAssignableFrom( t ) ).ToArray();
        }

        public static int GetBehaviourCountWithCache()
        {
            if( cachedCameraTypes == null )
            {
                CacheBehaviours();
            }

            return cachedCameraTypes.Length;
        }

        public static Type[] GetBehaviourTypesWithCache()
        {
            if( cachedCameraTypes == null )
            {
                CacheBehaviours();
            }

            return cachedCameraTypes.ToArray(); // copy.
        }
    }
}