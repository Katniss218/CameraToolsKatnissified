using CameraToolsKatnissified.Animation;
using CameraToolsKatnissified.CameraControllers;
using KSP.UI;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CameraToolsKatnissified
{
    /// <summary>
    /// The main class controlling the camera.
    /// </summary>
    [KSPAddon( KSPAddon.Startup.Flight, false )]
    [DisallowMultipleComponent]
    public sealed partial class CameraToolsManager : MonoBehaviour
    {
        // This class should handle user input, and manage the camera behaviours.
        // Also should manage the camera itself.

        public const string DIRECTORY_NAME = "CameraToolsKatnissified";

        public const string PATHS_FILE = "GameData/" + DIRECTORY_NAME + "/paths.cfg";

        static CameraToolsManager _instance;
        public static CameraToolsManager Instance
        {
            get
            {
                if( _instance == null )
                {
                    _instance = FindObjectOfType<CameraToolsManager>();
                }
                return _instance;
            }
        }

        /// <summary>
        /// True if the CameraTools window should be displayed.
        /// </summary>
        public static bool _guiWindowVisible = false;
        static bool _isToolbarButtonAdded = false;

        /// <summary>
        /// True if the player didn't hide the UI.
        /// </summary>
        public static bool _uiVisible = true;

        public CameraController CurrentController { get; private set; }

        /// <summary>
        /// True if CameraTools is editing a path.
        /// </summary>
        public bool IsActive { get => CurrentController != null && CurrentController.IsPlaying; }

        /// <summary>
        /// Zoom level when using manual zoom.
        /// </summary>
        public float Zoom { get; set; } = 1.0f;

        public float ShakeMultiplier { get; set; } = 0.0f;

        /// <summary>
        /// The main camera.
        /// </summary>
        public FlightCamera FlightCamera { get; set; }

        public Vessel ActiveVessel { get; set; }

        Vector3 _originalCameraPosition;
        Quaternion _originalCameraRotation;
        Transform _originalCameraParent;
        float _originalCameraNearClip;

        bool _hasDied { get; set; } = false;
        float _diedTime = 0;

        public const float SCROLL_MULTIPLIER = 10.0f;

        /// <summary>
        /// This is set to false to prevent the selector triggering immediately after the gui button is pressed.
        /// </summary>
        public bool _wasMouseUp { get; set; } = false;

        float _cameraShakeMagnitude = 0.0f;

        //      new
        // Vessel - stationary follow, path, drone all move and rotate this obj
        // - VesselOffset - stationary follow offset velocity moves this
        // - - PlayerOffset - offset to make it orbit
        // - - - Shake - shake shakes this
        // - - - - Camera



        public void SetController<T>() where T : CameraController
        {
            if( CurrentController != null ) // Should only be null on first load.
            {
                if( CurrentController.IsPlaying )
                {
                    CurrentController.EndPlaying();
                }
                CameraController.Detach( CurrentController );
            }

            CurrentController = CameraController.Attach<T>( FlightCamera );
        }

        public void SaveAndDisableCamera()
        {
            Debug.Log( $"[CameraToolsKatnissified] '{nameof( SaveAndDisableCamera )}'" );
            _hasDied = false;
            _originalCameraPosition = FlightCamera.transform.position;
            _originalCameraRotation = FlightCamera.transform.rotation;
            _originalCameraParent = FlightCamera.transform.parent;
            _originalCameraNearClip = Camera.main.nearClipPlane;

            FlightCamera.SetTargetNone();
            FlightCamera.DeactivateUpdate();
            //FlightCamera.enabled = false;
        }

        public void LoadSavedAndEnableCamera()
        {
            Debug.Log( $"[CameraToolsKatnissified] '{nameof( LoadSavedAndEnableCamera )}'" );
            if( FlightGlobals.ActiveVessel != null && HighLogic.LoadedScene == GameScenes.FLIGHT )
            {
                FlightCamera.SetTarget( FlightGlobals.ActiveVessel.transform, FlightCamera.TargetMode.Vessel );
            }
            FlightCamera.transform.SetParent( _originalCameraParent );
            FlightCamera.transform.position = _originalCameraPosition;
            FlightCamera.transform.rotation = _originalCameraRotation;
            Camera.main.nearClipPlane = _originalCameraNearClip;
            FlightCamera.SetFoV( 60 );
            FlightCamera.ActivateUpdate();

#warning TODO - possibly not needed.
            if( !FlightCamera.enabled )
            {
                FlightCamera.enabled = true; // sometimes is set to false, god knows why.
            }
            //FlightCamera.EnableCamera(); // this fixes broken sound.
        }


        void Awake()
        {
            _instance = this;
            //LoadAndDeserialize();
        }

        void Start()
        {
            _windowRect = new Rect( Screen.width - (12 * 20) - 40, 0, (12 * 20), _windowHeight );
            FlightCamera = FlightCamera.fetch;

            SetController<CameraPlayerController>();

            AddToolbarButton();

            GameEvents.onHideUI.Add( GameUIDisable );
            GameEvents.onShowUI.Add( GameUIEnable );
            GameEvents.OnVesselRecoveryRequested.Add( PostDeathRevert );
            GameEvents.onGameSceneLoadRequested.Add( PostDeathRevert );
            GameEvents.onVesselChange.Add( SwitchToVessel );
            //GameEvents.onFloatingOriginShift.Add( OnFloatingOriginShift );

            if( FlightGlobals.ActiveVessel != null )
            {
                ActiveVessel = FlightGlobals.ActiveVessel;
            }
        }

        void OnDestroy()
        {
            GameEvents.onHideUI.Remove( GameUIDisable );
            GameEvents.onShowUI.Remove( GameUIEnable );
            GameEvents.OnVesselRecoveryRequested.Remove( PostDeathRevert );
            GameEvents.onGameSceneLoadRequested.Remove( PostDeathRevert );
            GameEvents.onVesselChange.Remove( SwitchToVessel );
        }

        void Update()
        {
            if( Input.GetKeyDown( KeyCode.KeypadDivide ) )
            {
                _guiWindowVisible = !_guiWindowVisible;
            }

            if( Input.GetMouseButtonUp( 0 ) || !Input.GetMouseButton( 0 ) )
            {
                _wasMouseUp = true;
            }

            if( Input.GetKeyDown( KeyCode.Home ) )
            {
                if( CurrentController.IsPlaying )
                {
                    CurrentController.EndPlaying();
                }
                CurrentController.StartPlaying();
            }

            if( Input.GetKeyDown( KeyCode.PageUp ) )
            {
                if( CurrentController.IsPlaying )
                {
                    CurrentController.EndPlaying();
                }
                if( !(CurrentController is CameraPlayerController) )
                {
                    SetController<CameraPlayerController>();
                }
                CurrentController.StartPlaying();
            }

            if( Input.GetKeyDown( KeyCode.End ) )
            {
                if( CurrentController.IsPlaying )
                {
                    CurrentController.EndPlaying();
                }
                if( !(CurrentController is CameraPlayerController) )
                {
                    SetController<CameraPlayerController>();
                }
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

        void LateUpdate()
        {
            //if( IsPlayingCT && FlightCamera.transform.parent != _behaviours[_behaviours.Count - 1].Pivot )
            //{
            //    OnKilled();
            //}
        }

        void FixedUpdate()
        {
            if( !FlightGlobals.ready )
            {
                return;
            }

            if( FlightGlobals.ActiveVessel != null && (ActiveVessel == null || ActiveVessel != FlightGlobals.ActiveVessel) )
            {
                ActiveVessel = FlightGlobals.ActiveVessel;
            }

            //if( !CameraToolsActive && !UseAutoZoom )
            // {
            //    ZoomFactor = Mathf.Exp( Zoom ) / Mathf.Exp( 1 );
            //}

            // LastCameraPosition = FlightCamera.transform.position; // was in stationary camera only.
            // LastCameraRotation = FlightCamera.transform.rotation;

            /*if( _hasDied && Time.time - _diedTime > 2 )
            {
                if( CurrentController.IsPlaying )
                {
                    CurrentController.EndPlaying();
                }
                SetController<CameraPlayerController>();
            }*/
        }

        public void ShakeCamera( float magnitude )
        {
            _cameraShakeMagnitude = Mathf.Max( _cameraShakeMagnitude, magnitude );
        }

        public void UpdateCameraShakeMagnitude()
        {
            if( ShakeMultiplier > 0 )
            {
                FlightCamera.transform.rotation = Quaternion.AngleAxis( (ShakeMultiplier / 2) * _cameraShakeMagnitude / 50f, Vector3.ProjectOnPlane( UnityEngine.Random.onUnitSphere, FlightCamera.transform.forward ) ) * FlightCamera.transform.rotation;
            }

            _cameraShakeMagnitude = Mathf.Lerp( _cameraShakeMagnitude, 0, 5 * Time.fixedDeltaTime );
        }

        public void DoCameraShake( Vessel vessel )
        {
            //shake
            float camDistance = Vector3.Distance( FlightCamera.transform.position, vessel.CoM );

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
            foreach( var engine in ActiveVessel.FindPartModulesImplementing<ModuleEngines>() )
            {
                total += engine.finalThrust;
            }
            return total;
        }

        void PostDeathRevert( GameScenes f )
        {
            if( IsActive )
            {
                CurrentController.EndPlaying();
                SetController<CameraPlayerController>();
            }
        }

        void PostDeathRevert( Vessel v )
        {
            if( IsActive )
            {
                CurrentController.EndPlaying();
                SetController<CameraPlayerController>();
            }
        }

        /// <summary>
        /// Adds the button to the toolbar.
        /// </summary>
        void AddToolbarButton()
        {
            if( !_isToolbarButtonAdded )
            {
                Texture buttonTexture = GameDatabase.Instance.GetTexture( $"{DIRECTORY_NAME}/Textures/icon", false );
                ApplicationLauncher.Instance.AddModApplication( ButtonEnableGUI, ButtonDisableGUI, EmptyMethod, EmptyMethod, EmptyMethod, EmptyMethod, ApplicationLauncher.AppScenes.FLIGHT, buttonTexture );
                _isToolbarButtonAdded = true;
            }
        }

        void ButtonEnableGUI()
        {
            _guiWindowVisible = true;
            Debug.Log( "Showing CamTools GUI" );
        }

        void ButtonDisableGUI()
        {
            _guiWindowVisible = false;
            Debug.Log( "Hiding CamTools GUI" );
        }

        void EmptyMethod()
        { }

        /// <summary>
        /// Listener to fire when the player shows the UI with F2 key.
        /// </summary>
        void GameUIEnable()
        {
            _uiVisible = true;
        }

        /// <summary>
        /// Listener to fire when the player hides the UI with F2 key.
        /// </summary>
        void GameUIDisable()
        {
            _uiVisible = false;
        }

        /*OnFloatingOriginShift( Vector3d offset, Vector3d data1 )
        {

            Debug.LogWarning ("======Floating origin shifted.======");
            Debug.LogWarning ("======Passed offset: "+offset+"======");
            Debug.LogWarning ("======FloatingOrigin offset: "+FloatingOrigin.fetch.offset+"======");
            Debug.LogWarning("========Floating Origin threshold: "+FloatingOrigin.fetch.threshold+"==========");

        }*/

        public void SwitchToVessel( Vessel vessel )
        {
            ActiveVessel = vessel;
        }
    }
}