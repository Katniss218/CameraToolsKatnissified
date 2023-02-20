using CameraToolsKatnissified.Cameras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified
{
    public sealed class CameraPlayerController : CameraController
    {
        List<Transform> _pivots = new List<Transform>();
        public List<CameraBehaviour> Behaviours { get; private set; } = new List<CameraBehaviour>();

        void DestroyPivots()
        {
            foreach( var pivot in _pivots )
            {
                Destroy( pivot.gameObject );
            }
        }

        void CreatePivots()
        {
            Transform parent = null;
            Transform pivot = Pivot;

            bool first = true;
            foreach( var beh in Behaviours )
            {
                if( first ) // don't create an extra object for the first camera.
                {
                    first = false;
                }
                else
                {
                    GameObject go = new GameObject( $"Camera Pivot - {beh.GetType().FullName}" );
                    pivot = go.transform;
                }

                pivot.SetParent( parent );
                pivot.position = Camera.transform.position;
                pivot.rotation = Camera.transform.rotation;

                beh.SetTransform( pivot );

                parent = pivot;
            }

            Camera.transform.SetParent( parent );
        }

        void Awake()
        {
            Behaviours = new List<CameraBehaviour>();
            Behaviours.Add( new StationaryBehaviour() );

            Load();
        }

        protected override void OnStartPlaying()
        {
            CreatePivots();

            foreach( var beh in Behaviours )
            {
                beh.StartPlaying();
            }
        }

        protected override void OnEndPlaying()
        {
            foreach( var beh in Behaviours )
            {
                beh.StopPlaying();
            }

            DestroyPivots();
        }

        /*void Start()
        {
           
        }*/

        void OnDestroy()
        {
            foreach( var beh in Behaviours )
            {
                beh.StopPlaying();
            }

            DestroyPivots();
        }

        void Update()
        {
            foreach( var beh in Behaviours )
            {
                beh.Update( IsPlaying );
            }
        }

        void FixedUpdate()
        {
            foreach( var beh in Behaviours )
            {
                beh.FixedUpdate( IsPlaying );
            }
        }

        void OnGUI()
        {
            foreach( var beh in Behaviours )
            {
                beh.OnGUI();
            }
        }


        public void CycleToolMode( int behaviourIndex, int step )
        {
            if( behaviourIndex < 0 || behaviourIndex >= Behaviours.Count )
            {
                throw new ArgumentOutOfRangeException( "Behaviour Index must be within the behaviours array", nameof( behaviourIndex ) );
            }

            if( this.IsPlaying )
            {
                this.EndPlaying();
            }

            Type[] types = CameraBehaviour.GetBehaviourTypesWithCache();
            Type thisType = Behaviours[behaviourIndex].GetType();
            int typeIndex = types.IndexOf( thisType );

            int newTypeIndex = (typeIndex + step + types.Length) % types.Length; // adding length unfucks negative modulo

            Behaviours[behaviourIndex] = (CameraBehaviour)Activator.CreateInstance( types[newTypeIndex], new object[] { } );
        }


        public void Load()
        {
            foreach( var beh in Behaviours )
            {
                beh.OnLoad( null );
            }
        }

        public void Save()
        {
            foreach( var beh in Behaviours )
            {
                beh.OnSave( null );
            }
        }
    }
}
