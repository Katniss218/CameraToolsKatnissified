﻿using System;
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

        protected CameraToolsManager cameraBeh;

        public CameraBehaviour( CameraToolsManager ctm )
        {
            cameraBeh = ctm;
        }

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

        public abstract void DrawGui( ref float line );

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
            //this.enabled = true;
            Debug.Log( "[CTK] StartPlaying was called." );
            OnStartPlaying();
        }

        /// <summary>
        /// Call this to stop playing this camera behaviour.
        /// </summary>
        public void StopPlaying()
        {
            OnStopPlaying();
        }

        public virtual void Update()
        {

        }

        public virtual void FixedUpdate()
        {
            if( cameraBeh.CameraToolsActive )
            {
                OnPlaying();
            }
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