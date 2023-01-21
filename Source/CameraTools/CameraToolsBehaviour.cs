using CameraToolsKatnissified.Animation;
using CameraToolsKatnissified.Cameras;
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
    public sealed partial class CameraToolsBehaviour : MonoBehaviour
    {
        public const string DIRECTORY_NAME = "CameraToolsKatnissified";

        public static string pathSaveURL = $"GameData/{DIRECTORY_NAME}/paths.cfg";

        public static CameraToolsBehaviour Instance { get; set; }

        // GUI
        /// <summary>
        /// True if the CameraTools window should be displayed.
        /// </summary>
        static bool _guiWindowVisible = false;
        static bool _isToolbarButtonAdded = false;

        /// <summary>
        /// True if the player didn't hide the UI.
        /// </summary>
        static bool _uiVisible = true;

        [field: PersistentField]
        public CameraMode CurrentCameraMode { get; set; } = CameraMode.StationaryCamera;

        [field: PersistentField]
        public CameraReference CurrentReferenceMode { get; set; } = CameraReference.Surface;

        [field: PersistentField]
        public bool UseAutoZoom { get; set; } = false;

        /// <summary>
        /// Zoom level when using manual zoom.
        /// </summary>
        [field: PersistentField]
        public float Zoom { get; set; } = 1.0f;

        /// <summary>
        /// Zoom level when using auto zoom.
        /// </summary>
        [field: PersistentField]
        public float AutoZoomMargin { get; set; } = 20.0f;

        /// <summary>
        /// Maximum velocity of the target relative to the camera. Can be negative to reverse the camera direction.
        /// </summary>
        [field: PersistentField]
        public float MaxRelativeVelocity { get; set; } = 250.0f;

        /// <summary>
        /// Whether or not to use orbital velocity as reference. True - uses orbital velocity, False - uses surface velocity.
        /// </summary>
        [field: PersistentField]
        public bool UseOrbitalInitialVelocity { get; set; } = false;

        [field: PersistentField]
        public float ShakeMultiplier { get; set; } = 0.0f;

        /// <summary>
        /// Pivot used as a parent for the camera.
        /// </summary>
        public GameObject CameraPivot { get; set; }

        public FlightCamera FlightCamera { get; set; }

        private CameraBehaviour _behaviour;
        /// <summary>
        /// True if the CameraTools camera is active.
        /// </summary>
        bool CameraToolsActive => _behaviour != null;

        float _startCameraTimestamp;
        public float TimeSinceStart
        {
            get
            {
                return Time.time - _startCameraTimestamp;
            }
        }

        public Vessel ActiveVessel { get; set; }

        Vector3 _originalCameraPosition;
        Quaternion _originalCameraRotation;
        Transform _originalCameraParent;
        float _originalCameraNearClip;

        public Vector3 UpDirection { get; set; } = Vector3.up;

        public float ManualFov { get; set; } = 60;
        public float CurrentFov { get; set; } = 60;

        public Vector3 ManualPosition { get; set; } = Vector3.zero; // offset from moving the camera manually.

        public float ZoomFactor = 1;

        bool _hasDied = false;
        float _diedTime = 0;

        public const float SCROLL_MULTIPLIER = 10.0f;

#warning TODO - maybe use separate components and add/remove them to 'this.gameObject' as needed? This would declutter this class.
        // Used for the Initial Velocity camera mode.
        public Vector3 InitialVelocity { get; set; }
        public Vector3 InitialPosition { get; set; }
        public Orbit InitialOrbit { get; set; }

        // retaining position and rotation after vessel destruction
        public Vector3 LastCameraPosition { get; set; }
        public Quaternion LastCameraRotation { get; set; }

        /// <summary>
        /// This is set to false to prevent the selector triggering immediately after the gui button is pressed.
        /// </summary>
        bool _wasMouseUp = false;

        float _cameraShakeMagnitude = 0.0f;

        //pathing
        public int _currentCameraPathIndex = -1;
        public List<CameraPath> _availableCameraPaths;

        public CameraPath CurrentCameraPath
        {
            get
            {
                if( _currentCameraPathIndex >= 0 && _currentCameraPathIndex < _availableCameraPaths.Count )
                {
                    return _availableCameraPaths[_currentCameraPathIndex];
                }

                return null;
            }
        }

#warning TODO - probably better to edit the reference inside the list in-place.
        public int _currentKeyframeIndex = -1; // setting/editing the path keyframe?
        public float _currentKeyframeTime;
        public string _currKeyTimeString;

        bool _pathWindowVisible = false;
        bool _pathKeyframeWindowVisible = false;

        bool _settingPositionEnabled;
        bool _settingTargetEnabled;

        void Awake()
        {
            // Instance = the last CamTools object that has Awake called.
            if( Instance != null )
            {
                Destroy( Instance );
            }

            Instance = this;

            LoadAndDeserialize();
        }

        void Start()
        {
            _windowRect = new Rect( Screen.width - WINDOW_WIDTH - 40, 0, WINDOW_WIDTH, _windowHeight );
            FlightCamera = FlightCamera.fetch;

            SaveOriginalCamera();

            AddToolbarButton();

            GameEvents.onHideUI.Add( GameUIDisable );
            GameEvents.onShowUI.Add( GameUIEnable );
            GameEvents.OnVesselRecoveryRequested.Add( PostDeathRevert );
            GameEvents.onGameSceneLoadRequested.Add( PostDeathRevert );
            //GameEvents.onFloatingOriginShift.Add( OnFloatingOriginShift );

            CameraPivot = new GameObject( "StationaryCameraParent" );

            if( FlightGlobals.ActiveVessel != null )
            {
                ActiveVessel = FlightGlobals.ActiveVessel;
                CameraPivot.transform.position = FlightGlobals.ActiveVessel.transform.position;
            }

            AddBehaviour();

            GameEvents.onVesselChange.Add( SwitchToVessel );
        }

        void OnDestroy()
        {
            GameEvents.onVesselChange.Remove( SwitchToVessel );
        }

        void Update()
        {
            if( Input.GetKeyDown( KeyCode.KeypadDivide ) )
            {
                _guiWindowVisible = !_guiWindowVisible;
            }

            if( Input.GetKeyDown( KeyCode.Home ) )
            {
                if( _behaviour != null && _behaviour is PathCameraBehaviour )
                {
                    ((PathCameraBehaviour)_behaviour).StartPlayingPathCamera();
                }
                StartCamera();
            }
            if( Input.GetKeyDown( KeyCode.End ) )
            {
                EndCamera();
            }

            if( Input.GetMouseButtonUp( 0 ) )
            {
                _wasMouseUp = true;
            }

            if( _behaviour != null && _behaviour is StationaryCameraBehaviour )
            {
                // Set target from a mouse raycast.
                if( _settingTargetEnabled && _wasMouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
                {
                    _settingTargetEnabled = false;

                    Part newTarget = Utils.GetPartFromMouse();
                    if( newTarget != null )
                    {
                        ((StationaryCameraBehaviour)_behaviour).StationaryCameraTarget = newTarget;
                    }
                }

                // Set position from a mouse raycast
                if( _settingPositionEnabled && _wasMouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
                {
                    _settingPositionEnabled = false;

                    Vector3? newPosition = Utils.GetPosFromMouse();
                    if( newPosition != null )
                    {
                        ((StationaryCameraBehaviour)_behaviour).StationaryCameraPosition = newPosition;
                    }
                }
            }
        }

        void LateUpdate()
        {
            //retain pos and rot after vessel destruction
            if( CameraToolsActive && FlightCamera.transform.parent != CameraPivot.transform )
            {
                FlightCamera.SetTargetNone();
                FlightCamera.transform.parent = null;
                FlightCamera.transform.position = LastCameraPosition;
                FlightCamera.transform.rotation = LastCameraRotation;
                _hasDied = true;
                _diedTime = Time.time;
            }
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

            if( !CameraToolsActive && !UseAutoZoom )
            {
                ZoomFactor = Mathf.Exp( Zoom ) / Mathf.Exp( 1 );
            }

            if( _hasDied && Time.time - _diedTime > 2 )
            {
                EndCamera();
            }
        }

        /// <summary>
        /// Starts the CameraTools camera with the current settings.
        /// </summary>
        private void StartCamera()
        {
            _startCameraTimestamp = Time.time;

            if( !CameraToolsActive )
            {
                SaveOriginalCamera();
            }

            _hasDied = false;

            if( FlightGlobals.ActiveVessel != null )
            {
                ActiveVessel = FlightGlobals.ActiveVessel;
                UpDirection = -FlightGlobals.getGeeForceAtPosition( ActiveVessel.GetWorldPos3D() ).normalized;
                if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( ActiveVessel ) == FlightCamera.Modes.ORBITAL) )
                {
                    UpDirection = Vector3.up;
                }
            }

            _behaviour?.StartPlaying();
        }

        /// <summary>
        /// Reverts the KSP camera to the state before the CameraTools took over the control.
        /// </summary>
        public void EndCamera()
        {
            _hasDied = false;

            if( FlightGlobals.ActiveVessel != null && HighLogic.LoadedScene == GameScenes.FLIGHT )
            {
                FlightCamera.SetTarget( FlightGlobals.ActiveVessel.transform, FlightCamera.TargetMode.Vessel );
            }
            FlightCamera.transform.parent = _originalCameraParent;
            FlightCamera.transform.position = _originalCameraPosition;
            FlightCamera.transform.rotation = _originalCameraRotation;
            Camera.main.nearClipPlane = _originalCameraNearClip;

            FlightCamera.SetFoV( 60 );
            FlightCamera.ActivateUpdate();
            CurrentFov = 60;

            GameObject.Destroy( _behaviour );
            _behaviour = null;
        }

        void TogglePathList()
        {
            _pathKeyframeWindowVisible = false;
            _pathWindowVisible = !_pathWindowVisible;
        }

        void SaveOriginalCamera()
        {
            _originalCameraPosition = FlightCamera.transform.position;
            _originalCameraRotation = FlightCamera.transform.localRotation;
            _originalCameraParent = FlightCamera.transform.parent;
            _originalCameraNearClip = Camera.main.nearClipPlane;
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
            float fovFactor = 2f / ZoomFactor;

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
                waveFrontFactor = (srfSpeed / (angleToCam) < 3.67f) ? srfSpeed / 15 : 0;
            }

            lagAudioFactor *= waveFrontFactor;

            lagAudioFactor = Mathf.Clamp01( lagAudioFactor ) * distanceFactor * fovFactor;

            float shakeAtmPressureMultiplier = (float)vessel.dynamicPressurekPa / 2f * lagAudioFactor;

            float shakeThrustFactor = GetTotalThrust() / 1000f * distanceFactor * fovFactor * lagAudioFactor;

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
            if( CameraToolsActive )
            {
                EndCamera();
            }
        }

        void PostDeathRevert( Vessel v )
        {
            if( CameraToolsActive )
            {
                EndCamera();
            }
        }

        void SaveAndSerialize()
        {
            Serializer.SaveFields();

            ConfigNode pathFileNode = ConfigNode.Load( pathSaveURL );
            ConfigNode pathsNode = pathFileNode.GetNode( "CAMERAPATHS" );
            pathsNode.RemoveNodes( "CAMERAPATH" );

            foreach( var path in _availableCameraPaths )
            {
                path.Save( pathsNode );
            }
            pathFileNode.Save( pathSaveURL );
        }

        void LoadAndDeserialize()
        {
            Serializer.LoadFields();

            DeselectKeyframe();
            _currentCameraPathIndex = -1;
            _availableCameraPaths = new List<CameraPath>();
            ConfigNode pathFileNode = ConfigNode.Load( pathSaveURL );

            foreach( var node in pathFileNode.GetNode( "CAMERAPATHS" ).GetNodes( "CAMERAPATH" ) )
            {
                _availableCameraPaths.Add( CameraPath.Load( node ) );
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

        void CycleReferenceMode( int step )
        {
            int length = Enum.GetValues( typeof( CameraReference ) ).Length;

            CurrentReferenceMode = (CameraReference)(((int)CurrentReferenceMode + step + length) % length); // adding length unfucks negative modulo
        }

        void CycleToolMode( int step )
        {
            int length = Enum.GetValues( typeof( CameraMode ) ).Length;

            CurrentCameraMode = (CameraMode)(((int)CurrentCameraMode + step + length) % length); // adding length unfucks negative modulo
            AddBehaviour();
        }

        /// <summary>
        /// Adds the camera behaviour for the corresponding selected camera mode.
        /// </summary>
        void AddBehaviour()
        {
            if( CurrentCameraMode == CameraMode.StationaryCamera )
            {
                if( _behaviour != null )
                {
                    Destroy( _behaviour );
                }
                _behaviour = this.gameObject.AddComponent<StationaryCameraBehaviour>();
            }
            else if( CurrentCameraMode == CameraMode.Pathing )
            {
                if( _behaviour != null )
                {
                    Destroy( _behaviour );
                }
                _behaviour = this.gameObject.AddComponent<PathCameraBehaviour>();
            }
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

        public void CreateNewPath()
        {
            _pathKeyframeWindowVisible = false;
            _availableCameraPaths.Add( new CameraPath() );
            _currentCameraPathIndex = _availableCameraPaths.Count - 1;
        }

        public void DeletePath( int index )
        {
            if( index < 0 || index >= _availableCameraPaths.Count )
            {
                return;
            }

            _availableCameraPaths.RemoveAt( index );
            _currentCameraPathIndex = -1;
        }

        public void SelectPath( int index )
        {
            _currentCameraPathIndex = index;
        }

        public void SelectKeyframe( int index )
        {
            //if( _isPlayingPath )
            //{
            //    StopPlayingPathCamera();
            //}
            if( _behaviour != null && _behaviour is PathCameraBehaviour )
            {
                PathCameraBehaviour b = (PathCameraBehaviour)_behaviour;
                b.IsPlayingPath = false;
            }

            _currentKeyframeIndex = index;
            UpdateCurrentKeyframeValues();
            _pathKeyframeWindowVisible = true;
            ViewKeyframe( _currentKeyframeIndex );
        }

        public void DeselectKeyframe()
        {
            _currentKeyframeIndex = -1;
            _pathKeyframeWindowVisible = false;
        }

        public void UpdateCurrentKeyframeValues()
        {
            if( CurrentCameraPath == null || _currentKeyframeIndex < 0 || _currentKeyframeIndex >= CurrentCameraPath.keyframeCount )
            {
                return;
            }

            CameraKeyframe currentKey = CurrentCameraPath.GetKeyframe( _currentKeyframeIndex );
            _currentKeyframeTime = currentKey.time;

            _currKeyTimeString = _currentKeyframeTime.ToString();
        }

        public void CreateNewKeyframe()
        {
            //if( !CameraToolsActive )
            //{
            //    StartPathCamera();
            // }
            if( _behaviour == null )
            {
                _behaviour = this.gameObject.AddComponent<PathCameraBehaviour>();
                ((PathCameraBehaviour)_behaviour).IsPlayingPath = false;
            }

            _pathWindowVisible = false;

            float time = CurrentCameraPath.keyframeCount > 0 ? CurrentCameraPath.GetKeyframe( CurrentCameraPath.keyframeCount - 1 ).time + 1 : 0;
            CurrentCameraPath.AddTransform( FlightCamera.transform, Zoom, time );
            SelectKeyframe( CurrentCameraPath.keyframeCount - 1 );

            if( CurrentCameraPath.keyframeCount > 6 )
            {
                _pathScrollPosition.y += ENTRY_HEIGHT;
            }
        }

        public void DeleteKeyframe( int index )
        {
            CurrentCameraPath.RemoveKeyframe( index );
            if( index == _currentKeyframeIndex )
            {
                DeselectKeyframe();
            }
            if( CurrentCameraPath.keyframeCount > 0 && _currentKeyframeIndex >= 0 )
            {
                SelectKeyframe( Mathf.Clamp( _currentKeyframeIndex, 0, CurrentCameraPath.keyframeCount - 1 ) );
            }
        }

        /// <summary>
        /// Positions the camera at the keyframe.
        /// </summary>
        public void ViewKeyframe( int index )
        {
            //if( !CameraToolsActive )
            //{
            //    StartPathCamera();
            //}
            if( _behaviour == null )
            {
                _behaviour = this.gameObject.AddComponent<PathCameraBehaviour>();
                ((PathCameraBehaviour)_behaviour).IsPlayingPath = false;
            }

            CameraKeyframe currentKey = CurrentCameraPath.GetKeyframe( index );
            FlightCamera.transform.localPosition = currentKey.position;
            FlightCamera.transform.localRotation = currentKey.rotation;
            Zoom = currentKey.zoom;
        }
    }
}