using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.CameraControllers
{
    public sealed class CameraPlayerController : CameraController
    {
        public List<CameraBehaviour> Behaviours { get; private set; } = new List<CameraBehaviour>();

        public Vector3? CameraTargetWorldSpace { get; set; }

        public float ShakeMultiplier { get; set; } = 0.1f;

        List<Transform> _pivots = new List<Transform>();

        CameraToolsManager _ctm;

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
            _ctm = CameraToolsManager.Instance;
            Behaviours = new List<CameraBehaviour>();
            Behaviours.Add( CameraBehaviour.GetBehaviour( CameraBehaviour.GetDefaultType(), this ) );

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

            if( IsPlaying )
            {
                float fov = 60 / (Mathf.Exp( Zoom ) / Mathf.Exp( 1 ));
                if( Camera.FieldOfView != fov )
                {
                    Camera.SetFoV( fov );
                }

                //vessel camera shake
                if( ShakeMultiplier > 0 )
                {
                    foreach( var vessel in FlightGlobals.Vessels )
                    {
                        if( !vessel || !vessel.loaded || vessel.packed )
                        {
                            continue;
                        }

                        DoCameraShake( vessel );
                    }

                    UpdateCameraShakeMagnitude();
                }
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

        float _cameraShakeMagnitude = 0.0f;


        public void ShakeCamera( float magnitude )
        {
            _cameraShakeMagnitude = Mathf.Max( _cameraShakeMagnitude, magnitude );
        }

        public void UpdateCameraShakeMagnitude()
        {
            if( ShakeMultiplier > 0 )
            {
                Camera.transform.rotation = Quaternion.AngleAxis( (ShakeMultiplier / 2) * _cameraShakeMagnitude / 50f, Vector3.ProjectOnPlane( UnityEngine.Random.onUnitSphere, Camera.transform.forward ) ) * Camera.transform.rotation;
            }

            _cameraShakeMagnitude = Mathf.Lerp( _cameraShakeMagnitude, 0, 5 * Time.fixedDeltaTime );
        }

        public void DoCameraShake( Vessel vessel )
        {
            //shake
            float camDistance = Vector3.Distance( Camera.transform.position, vessel.CoM );

            float distanceFactor = 50f / camDistance;

            float angleToCam = Vector3.Angle( vessel.srf_velocity, FlightCamera.fetch.mainCamera.transform.position - vessel.transform.position );
            angleToCam = Mathf.Clamp( angleToCam, 1, 180 );

            float srfSpeed = (float)vessel.srfSpeed;

            float lagAudioFactor = (75000 / (Vector3.Distance( vessel.transform.position, FlightCamera.fetch.mainCamera.transform.position ) * srfSpeed * angleToCam / 90));
            lagAudioFactor = Mathf.Clamp( lagAudioFactor * lagAudioFactor * lagAudioFactor, 0, 4 );
            lagAudioFactor += srfSpeed / 230;

            float waveFrontFactor = ((3.67f * angleToCam) / srfSpeed);
            waveFrontFactor = Mathf.Clamp( waveFrontFactor * waveFrontFactor * waveFrontFactor, 0, 2 );
            if( vessel.srfSpeed > 330 )
            {
                waveFrontFactor = ((srfSpeed / angleToCam) < 3.67f) ? (srfSpeed / 15.0f) : 0.0f;
            }

            lagAudioFactor *= waveFrontFactor;

            lagAudioFactor = Mathf.Clamp01( lagAudioFactor ) * distanceFactor;

            float shakeAtmPressureMultiplier = (float)vessel.dynamicPressurekPa / 2f * lagAudioFactor;

            float shakeThrustFactor = GetTotalThrust() / 1000f * distanceFactor * lagAudioFactor;

            ShakeCamera( shakeAtmPressureMultiplier + shakeThrustFactor );
        }

        float GetTotalThrust()
        {
            float total = 0;
            foreach( var engine in _ctm.ActiveVessel.FindPartModulesImplementing<ModuleEngines>() )
            {
                total += engine.finalThrust;
            }
            return total;
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

            Behaviours[behaviourIndex] = CameraBehaviour.GetBehaviour( types[newTypeIndex], this );
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
