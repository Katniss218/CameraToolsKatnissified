using CameraToolsKatnissified.Animation;
using KSP.UI.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraToolsKatnissified
{
    /// <summary>
    /// The main class controlling the camera.
    /// </summary>
    [KSPAddon( KSPAddon.Startup.Flight, false )]
    public partial class CameraToolsBehaviour : MonoBehaviour
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
        float AutoZoomMargin { get; set; } = 20.0f;

        /// <summary>
        /// Maximum velocity of the target relative to the camera. Can be negative to reverse the camera direction.
        /// </summary>
        [field: PersistentField]
        public float MaxRelativeVelocity { get; set; } = 250.0f;

        /// <summary>
        /// Whether or not to use orbital velocity as reference. True - uses orbital velocity, False - uses surface velocity.
        /// </summary>
        [field: PersistentField]
        public bool UseOrbital { get; set; } = false;

        [field: PersistentField]
        public float ShakeMultiplier { get; set; } = 0.0f;

        /// <summary>
        /// Pivot used as a parent for the camera.
        /// </summary>
        GameObject _cameraPivot;

        /// <summary>
        /// True if the CameraTools camera is active.
        /// </summary>
        bool _cameraToolsActive = false;

        float _startCameraTimestamp;
        float _timeSinceStart
        {
            get
            {
                return Time.time - _startCameraTimestamp;
            }
        }

        Vessel _activeVessel;
        Vector3 _originalCameraPosition;
        Quaternion _originalCameraRotation;
        Transform _originalCameraParent;
        float _originalCameraNearClip;

        FlightCamera _flightCamera;

        Vector3 _upDirection = Vector3.up;

        float _manualFov = 60;
        float _currentFov = 60;

        Vector3 _manualPosition = Vector3.zero; // offset from moving the camera manually.

        float zoomFactor = 1;

        bool _hasDied = false;
        float _diedTime = 0;

        const float SCROLL_MULTIPLIER = 50.0f;

#warning TODO - maybe use separate components and add/remove them to 'this.gameObject' as needed? This would declutter this class.
        // Used for the Initial Velocity camera mode.
        Vector3 _initialVelocity;
        Vector3 _initialPosition;
        Orbit _initialOrbit;

        // retaining position and rotation after vessel destruction
        Vector3 _lastCameraPosition;
        Quaternion _lastCameraRotation;

        /// <summary>
        /// This is set to false to prevent the selector triggering immediately after the gui button is pressed.
        /// </summary>
        bool _wasMouseUp = false;

        float _cameraShakeMagnitude = 0.0f;

        //pathing
        int _currentCameraPathIndex = -1;
        List<CameraPath> _availableCameraPaths;

        CameraPath CurrentCameraPath
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
        int _currentKeyframeIndex = -1; // setting/editing the path keyframe?
        float _currentKeyframeTime;
        string _currKeyTimeString;

        bool _isPlayingPath = false;

        bool _pathWindowVisible = false;
        bool _pathKeyframeWindowVisible = false;

        Vector2 _pathSelectScrollPos;

        Vector3? _stationaryCameraPosition = null;
        bool _hasPosition => _stationaryCameraPosition != null;

        Part _stationaryCameraTarget = null;
        bool _hasTarget => _stationaryCameraTarget != null;

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
            _flightCamera = FlightCamera.fetch;
            _cameraToolsActive = false;
            SaveOriginalCamera();

            AddToolbarButton();

            GameEvents.onHideUI.Add( GameUIDisable );
            GameEvents.onShowUI.Add( GameUIEnable );
            GameEvents.OnVesselRecoveryRequested.Add( PostDeathRevert );
            GameEvents.onGameSceneLoadRequested.Add( PostDeathRevert );
            //GameEvents.onFloatingOriginShift.Add( OnFloatingOriginShift );

            _cameraPivot = new GameObject( "StationaryCameraParent" );

            if( FlightGlobals.ActiveVessel != null )
            {
                _activeVessel = FlightGlobals.ActiveVessel;
                _cameraPivot.transform.position = FlightGlobals.ActiveVessel.transform.position;
            }

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

            // Set target from a mouse raycast.
            if( _settingTargetEnabled && _wasMouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                _settingTargetEnabled = false;

                Part newTarget = Utils.GetPartFromMouse();
                if( newTarget != null )
                {
                    _stationaryCameraTarget = newTarget;
                }
            }

            // Set position from a mouse raycast
            if( _settingPositionEnabled && _wasMouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                _settingPositionEnabled = false;

                Vector3? newPosition = Utils.GetPosFromMouse();
                if( newPosition != null )
                {
                    _stationaryCameraPosition = newPosition;
                }
            }
        }

        void LateUpdate()
        {
            //retain pos and rot after vessel destruction
            if( _cameraToolsActive && _flightCamera.transform.parent != _cameraPivot.transform )
            {
                _flightCamera.SetTargetNone();
                _flightCamera.transform.parent = null;
                _flightCamera.transform.position = _lastCameraPosition;
                _flightCamera.transform.rotation = _lastCameraRotation;
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

            if( FlightGlobals.ActiveVessel != null && (_activeVessel == null || _activeVessel != FlightGlobals.ActiveVessel) )
            {
                _activeVessel = FlightGlobals.ActiveVessel;
            }

            if( _cameraToolsActive )
            {
                if( CurrentCameraMode == CameraMode.StationaryCamera )
                {
                    UpdateStationaryCamera();
                }
                else if( CurrentCameraMode == CameraMode.Pathing )
                {
                    UpdatePathingCamera();
                }
            }
            else
            {
                if( !UseAutoZoom )
                {
                    zoomFactor = Mathf.Exp( Zoom ) / Mathf.Exp( 1 );
                }
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

            if( !_cameraToolsActive )
            {
                SaveOriginalCamera();
            }

            _hasDied = false;

            if( FlightGlobals.ActiveVessel != null )
            {
                _activeVessel = FlightGlobals.ActiveVessel;
                _upDirection = -FlightGlobals.getGeeForceAtPosition( _activeVessel.GetWorldPos3D() ).normalized;
                if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( _activeVessel ) == FlightCamera.Modes.ORBITAL) )
                {
                    _upDirection = Vector3.up;
                }
            }

            if( CurrentCameraMode == CameraMode.StationaryCamera )
            {
                StartStationaryCamera();
            }
            else if( CurrentCameraMode == CameraMode.Pathing )
            {
                StartPathCamera();
                StartPlayingPathCamera();
            }
        }

        /// <summary>
        /// Reverts the KSP camera to the state before the CameraTools took over the control.
        /// </summary>
        void EndCamera()
        {
            _hasDied = false;

            if( FlightGlobals.ActiveVessel != null && HighLogic.LoadedScene == GameScenes.FLIGHT )
            {
                _flightCamera.SetTarget( FlightGlobals.ActiveVessel.transform, FlightCamera.TargetMode.Vessel );
            }
            _flightCamera.transform.parent = _originalCameraParent;
            _flightCamera.transform.position = _originalCameraPosition;
            _flightCamera.transform.rotation = _originalCameraRotation;
            Camera.main.nearClipPlane = _originalCameraNearClip;

            _flightCamera.SetFoV( 60 );
            _flightCamera.ActivateUpdate();
            _currentFov = 60;

            _cameraToolsActive = false;

            StopPlayingPathCamera();
        }


        void StartStationaryCamera()
        {
            Debug.Log( "flightCamera position init: " + _flightCamera.transform.position );

            if( FlightGlobals.ActiveVessel != null )
            {
                _flightCamera.SetTargetNone();
                _flightCamera.transform.parent = _cameraPivot.transform;
                _flightCamera.DeactivateUpdate();

                _cameraPivot.transform.position = _activeVessel.transform.position + _activeVessel.rb_velocity * Time.fixedDeltaTime;
                _manualPosition = Vector3.zero;

                if( _hasPosition )
                {
                    _flightCamera.transform.position = _stationaryCameraPosition.Value;
                }

                _initialVelocity = _activeVessel.srf_velocity;
                _initialOrbit = new Orbit();
                _initialOrbit.UpdateFromStateVectors( _activeVessel.orbit.pos, _activeVessel.orbit.vel, FlightGlobals.currentMainBody, Planetarium.GetUniversalTime() );
                //_initialUT = Planetarium.GetUniversalTime();

                _cameraToolsActive = true;
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }

            Debug.Log( "flightCamera position post init: " + _flightCamera.transform.position );
        }

        void UpdateStationaryCamera()
        {
            if( _flightCamera.Target != null )
            {
                _flightCamera.SetTargetNone(); //dont go to next vessel if vessel is destroyed
            }

            if( _hasTarget )
            {
                Vector3 toTargetDirection = (_stationaryCameraTarget.transform.position - _flightCamera.transform.position).normalized;

                _flightCamera.transform.rotation = Quaternion.LookRotation( toTargetDirection, _upDirection );
            }

            if( _activeVessel != null )
            {
                // Parent follows the vessel.
                _cameraPivot.transform.position = _manualPosition + _activeVessel.transform.position;

                // Camera itself accumulates the inverse of the vessel movement.
                if( CurrentReferenceMode == CameraReference.Surface )
                {
                    float magnitude = Mathf.Clamp( (float)_activeVessel.srf_velocity.magnitude, 0, MaxRelativeVelocity );
                    _flightCamera.transform.position -= Time.fixedDeltaTime * magnitude * _activeVessel.srf_velocity.normalized;
                }
                else if( CurrentReferenceMode == CameraReference.Orbit )
                {
                    float magnitude = Mathf.Clamp( (float)_activeVessel.obt_velocity.magnitude, 0, MaxRelativeVelocity );
                    _flightCamera.transform.position -= Time.fixedDeltaTime * magnitude * _activeVessel.obt_velocity.normalized;
                }
                else if( CurrentReferenceMode == CameraReference.InitialVelocity )
                {
                    Vector3 camVelocity;
                    if( UseOrbital && _initialOrbit != null )
                    {
                        camVelocity = _initialOrbit.getOrbitalVelocityAtUT( Planetarium.GetUniversalTime() ).xzy - _activeVessel.GetObtVelocity();
                    }
                    else
                    {
                        camVelocity = _initialVelocity - _activeVessel.srf_velocity;
                    }
                    _flightCamera.transform.position += camVelocity * Time.fixedDeltaTime;
                }
#warning TODO - add the velocity direction mode here.
            }

            //mouse panning, moving
            Vector3 forwardLevelAxis = (Quaternion.AngleAxis( -90, _upDirection ) * _flightCamera.transform.right).normalized;

            if( Input.GetKey( KeyCode.Mouse1 ) ) // right mouse
            {
                // No target - should turn the camera like a tripod.
                // Has target - should orbit the target.
                if( !_hasTarget )
                {
                    _flightCamera.transform.rotation *= Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * 1.7f, Vector3.up );
                    _flightCamera.transform.rotation *= Quaternion.AngleAxis( -Input.GetAxis( "Mouse Y" ) * 1.7f, Vector3.right );
                    _flightCamera.transform.rotation = Quaternion.LookRotation( _flightCamera.transform.forward, _upDirection );
                }
                else
                {
                    var verticalaxis = _flightCamera.transform.TransformDirection( Vector3.up );
                    var horizontalaxis = _flightCamera.transform.TransformDirection( Vector3.right );
                    _flightCamera.transform.RotateAround( _stationaryCameraTarget.transform.position, verticalaxis, Input.GetAxis( "Mouse X" ) * 1.7f );
                    _flightCamera.transform.RotateAround( _stationaryCameraTarget.transform.position, horizontalaxis, -Input.GetAxis( "Mouse Y" ) * 1.7f );
                    _flightCamera.transform.rotation = Quaternion.LookRotation( _flightCamera.transform.forward, _upDirection );
                }
            }

            if( Input.GetKey( KeyCode.Mouse2 ) ) // middle mouse
            {
                _manualPosition += _flightCamera.transform.right * Input.GetAxis( "Mouse X" ) * 2;
                _manualPosition += forwardLevelAxis * Input.GetAxis( "Mouse Y" ) * 2;
            }

            _manualPosition += _upDirection * SCROLL_MULTIPLIER * Input.GetAxis( "Mouse ScrollWheel" );

            // autoFov
            if( _hasTarget && UseAutoZoom )
            {
                float cameraDistance = Vector3.Distance( _stationaryCameraTarget.transform.position, _flightCamera.transform.position );

                float targetFoV = Mathf.Clamp( (7000 / (cameraDistance + 100)) - 14 + AutoZoomMargin, 2, 60 );

                _manualFov = targetFoV;
            }

            //FOV
            if( !UseAutoZoom )
            {
                zoomFactor = Mathf.Exp( Zoom ) / Mathf.Exp( 1 );
                _manualFov = 60 / zoomFactor;

                if( _currentFov != _manualFov )
                {
                    _currentFov = Mathf.Lerp( _currentFov, _manualFov, 0.1f );
                    _flightCamera.SetFoV( _currentFov );
                }
            }
            else
            {
                _currentFov = Mathf.Lerp( _currentFov, _manualFov, 0.1f );
                _flightCamera.SetFoV( _currentFov );
                zoomFactor = 60 / _currentFov;
            }

            _lastCameraPosition = _flightCamera.transform.position;
            _lastCameraRotation = _flightCamera.transform.rotation;

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

        void StartPathCamera()
        {
            if( FlightGlobals.ActiveVessel != null )
            {
                _cameraPivot.transform.position = _activeVessel.transform.position + _activeVessel.rb_velocity * Time.fixedDeltaTime;
                _cameraPivot.transform.rotation = _activeVessel.transform.rotation;

                _flightCamera.SetTargetNone();
                _flightCamera.transform.parent = _cameraPivot.transform;
                _flightCamera.DeactivateUpdate();

                _cameraToolsActive = true;
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }
        }

        void StartPlayingPathCamera()
        {
            if( _currentCameraPathIndex < 0 || CurrentCameraPath.keyframeCount <= 0 )
            {
                EndCamera();
                return;
            }

            DeselectKeyframe();

            if( !_cameraToolsActive )
            {
                StartPathCamera();
            }

            CameraTransformation firstFrame = CurrentCameraPath.Evaulate( 0 );
            _flightCamera.transform.localPosition = firstFrame.position;
            _flightCamera.transform.localRotation = firstFrame.rotation;
            Zoom = firstFrame.zoom;

            _isPlayingPath = true;

            // initialize the rotation on start, but don't update it so if the rocket rolls, the camera won't follow it.
            _cameraPivot.transform.rotation = _activeVessel.transform.rotation;
        }

        void StopPlayingPathCamera()
        {
            _isPlayingPath = false;
        }

        void UpdatePathingCamera()
        {
            // Update the frame of reference's position to follow the vessel.
            _cameraPivot.transform.position = _activeVessel.transform.position + _activeVessel.rb_velocity * Time.fixedDeltaTime;
            //_stationaryCameraParent.transform.rotation = _activeVessel.transform.rotation; // here to follow rotation.

            if( _isPlayingPath )
            {
                CameraTransformation tf = CurrentCameraPath.Evaulate( _timeSinceStart * CurrentCameraPath.timeScale );
                _flightCamera.transform.localPosition = Vector3.Lerp( _flightCamera.transform.localPosition, tf.position, CurrentCameraPath.lerpRate * Time.fixedDeltaTime );
                _flightCamera.transform.localRotation = Quaternion.Slerp( _flightCamera.transform.localRotation, tf.rotation, CurrentCameraPath.lerpRate * Time.fixedDeltaTime );
                Zoom = Mathf.Lerp( Zoom, tf.zoom, CurrentCameraPath.lerpRate * Time.fixedDeltaTime );
            }
            else
            {
                //move
                //mouse panning, moving
                Vector3 forwardLevelAxis = _flightCamera.transform.forward;


                if( Input.GetKey( KeyCode.Mouse1 ) && Input.GetKey( KeyCode.Mouse2 ) )
                {
                    _flightCamera.transform.rotation = Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * -1.7f, _flightCamera.transform.forward ) * _flightCamera.transform.rotation;
                }
                else
                {
                    if( Input.GetKey( KeyCode.Mouse1 ) )
                    {
                        _flightCamera.transform.rotation *= Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * 1.7f / (Zoom * Zoom), Vector3.up );
                        _flightCamera.transform.rotation *= Quaternion.AngleAxis( -Input.GetAxis( "Mouse Y" ) * 1.7f / (Zoom * Zoom), Vector3.right );
                        _flightCamera.transform.rotation = Quaternion.LookRotation( _flightCamera.transform.forward, _flightCamera.transform.up );
                    }
                    if( Input.GetKey( KeyCode.Mouse2 ) )
                    {
                        _flightCamera.transform.position += _flightCamera.transform.right * Input.GetAxis( "Mouse X" ) * 2;
                        _flightCamera.transform.position += forwardLevelAxis * Input.GetAxis( "Mouse Y" ) * 2;
                    }
                }
                _flightCamera.transform.position += _flightCamera.transform.up * 10 * Input.GetAxis( "Mouse ScrollWheel" );

            }

            //zoom
            zoomFactor = Mathf.Exp( Zoom ) / Mathf.Exp( 1 );
            _manualFov = 60 / zoomFactor;

            if( _currentFov != _manualFov )
            {
                _currentFov = Mathf.Lerp( _currentFov, _manualFov, 0.1f );
                _flightCamera.SetFoV( _currentFov );
            }
        }

        void TogglePathList()
        {
            _pathKeyframeWindowVisible = false;
            _pathWindowVisible = !_pathWindowVisible;
        }

        void SaveOriginalCamera()
        {
            _originalCameraPosition = _flightCamera.transform.position;
            _originalCameraRotation = _flightCamera.transform.localRotation;
            _originalCameraParent = _flightCamera.transform.parent;
            _originalCameraNearClip = Camera.main.nearClipPlane;
        }

        public void ShakeCamera( float magnitude )
        {
            _cameraShakeMagnitude = Mathf.Max( _cameraShakeMagnitude, magnitude );
        }

        void UpdateCameraShakeMagnitude()
        {
            if( ShakeMultiplier > 0 )
            {
                _flightCamera.transform.rotation = Quaternion.AngleAxis( (ShakeMultiplier / 2) * _cameraShakeMagnitude / 50f, Vector3.ProjectOnPlane( UnityEngine.Random.onUnitSphere, _flightCamera.transform.forward ) ) * _flightCamera.transform.rotation;
            }

            _cameraShakeMagnitude = Mathf.Lerp( _cameraShakeMagnitude, 0, 5 * Time.fixedDeltaTime );
        }

        public void DoCameraShake( Vessel vessel )
        {
            //shake
            float camDistance = Vector3.Distance( _flightCamera.transform.position, vessel.CoM );

            float distanceFactor = 50f / camDistance;
            float fovFactor = 2f / zoomFactor;

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
            foreach( var engine in _activeVessel.FindPartModulesImplementing<ModuleEngines>() )
            {
                total += engine.finalThrust;
            }
            return total;
        }

        void PostDeathRevert( GameScenes f )
        {
            if( _cameraToolsActive )
            {
                EndCamera();
            }
        }

        void PostDeathRevert( Vessel v )
        {
            if( _cameraToolsActive )
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
        }

        /*OnFloatingOriginShift( Vector3d offset, Vector3d data1 )
        {

            Debug.LogWarning ("======Floating origin shifted.======");
            Debug.LogWarning ("======Passed offset: "+offset+"======");
            Debug.LogWarning ("======FloatingOrigin offset: "+FloatingOrigin.fetch.offset+"======");
            Debug.LogWarning("========Floating Origin threshold: "+FloatingOrigin.fetch.threshold+"==========");

        }*/

        void SwitchToVessel( Vessel vessel )
        {
            _activeVessel = vessel;
        }

        void CreateNewPath()
        {
            _pathKeyframeWindowVisible = false;
            _availableCameraPaths.Add( new CameraPath() );
            _currentCameraPathIndex = _availableCameraPaths.Count - 1;
        }

        void DeletePath( int index )
        {
            if( index < 0 || index >= _availableCameraPaths.Count )
            {
                return;
            }

            _availableCameraPaths.RemoveAt( index );
            _currentCameraPathIndex = -1;
        }

        void SelectPath( int index )
        {
            _currentCameraPathIndex = index;
        }

        void SelectKeyframe( int index )
        {
            if( _isPlayingPath )
            {
                StopPlayingPathCamera();
            }

            _currentKeyframeIndex = index;
            UpdateCurrentKeyframeValues();
            _pathKeyframeWindowVisible = true;
            ViewKeyframe( _currentKeyframeIndex );
        }

        void DeselectKeyframe()
        {
            _currentKeyframeIndex = -1;
            _pathKeyframeWindowVisible = false;
        }

        void UpdateCurrentKeyframeValues()
        {
            if( CurrentCameraPath == null || _currentKeyframeIndex < 0 || _currentKeyframeIndex >= CurrentCameraPath.keyframeCount )
            {
                return;
            }

            CameraKeyframe currentKey = CurrentCameraPath.GetKeyframe( _currentKeyframeIndex );
            _currentKeyframeTime = currentKey.time;

            _currKeyTimeString = _currentKeyframeTime.ToString();
        }

        void CreateNewKeyframe()
        {
            if( !_cameraToolsActive )
            {
                StartPathCamera();
            }

            _pathWindowVisible = false;

            float time = CurrentCameraPath.keyframeCount > 0 ? CurrentCameraPath.GetKeyframe( CurrentCameraPath.keyframeCount - 1 ).time + 1 : 0;
            CurrentCameraPath.AddTransform( _flightCamera.transform, Zoom, time );
            SelectKeyframe( CurrentCameraPath.keyframeCount - 1 );

            if( CurrentCameraPath.keyframeCount > 6 )
            {
                _pathScrollPosition.y += ENTRY_HEIGHT;
            }
        }

        void DeleteKeyframe( int index )
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
        void ViewKeyframe( int index )
        {
            if( !_cameraToolsActive )
            {
                StartPathCamera();
            }

            CameraKeyframe currentKey = CurrentCameraPath.GetKeyframe( index );
            _flightCamera.transform.localPosition = currentKey.position;
            _flightCamera.transform.localRotation = currentKey.rotation;
            Zoom = currentKey.zoom;
        }
    }
}