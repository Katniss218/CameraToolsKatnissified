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
    public class CameraToolsBehaviour : MonoBehaviour
    {
        /*
         * 
         * 
            foreach( var vessel in FlightGlobals.Vessels )
            {
                if( !vessel || !vessel.loaded || vessel.packed || (!includeActiveVessel && vessel.isActiveVessel) )
                {
                    continue;
                }

                vessel.gameObject.AddComponent<CTAtmosphericAudioController>();
            }
         * */

        public const string DIRECTORY_NAME = "CameraToolsKatnissified";

        public static CameraToolsBehaviour Instance { get; set; }

        GameObject _stationaryCameraParent;

        Vessel _activeVessel;
        Vector3 _originalCameraPosition;
        Quaternion _originalCameraRotation;
        Transform _originalCameraParent;
        float _originalCameraNearClip;

        FlightCamera _flightCamera;

        Vector3 cameraUp = Vector3.up;

        /// <summary>
        /// True if the CameraTools camera is active.
        /// </summary>
        bool _cameraToolsCameraActive = false;

        // GUI
        public static bool GuiEnabled { get; set; } = false;
        public static bool _isToolbarButtonAdded { get; set; } = false;

        Rect _windowRect = new Rect( 0, 0, 0, 0 );
        float _windowWidth = 250;
        float _windowHeight = 400;
        float _draggableHeight = 40;

        bool _gameUIToggle = true;

        [PersistentField]
        public CameraMode CurrentCameraMode = CameraMode.StationaryCamera;

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
        public float MaxRelativeVelocity { get; set; } = 250;

        /// <summary>
        /// Whether or not to use orbital velocity as reference. True - uses orbital velocity, False - uses surface velocity.
        /// </summary>
        [field: PersistentField]
        public bool UseOrbital { get; set; } = false;

        float manualFov = 60;
        float currentFov = 60;
        Vector3 _manualPosition = Vector3.zero;

        float zoomFactor = 1;

        bool hasSavedRotation = false;
        Quaternion savedRotation;

        bool _hasDied = false;
        float _diedTime = 0;

        Vector3 _initialVelocity;
        Vector3 _initialPosition;
        Orbit _initialOrbit;
        double _initialUT;

        // retaining position and rotation after vessel destruction
        Vector3 _lastCameraPosition;
        Quaternion _lastCameraRotation;

        /// <summary>
        /// Cached if the mouse button 0 was released this frame. Also is set to false sometimes.
        /// </summary>
        bool _mouseUp = false;

        // recording input for key binding
        bool _isRecordingKeyInput = false;
        bool _recordingActivate = false; // specifies which key is listening
        bool _recordingRevert = false; // specifies which key is listening

        Vector3 resetPositionFix; // fixes position movement after setting and resetting camera

        // floating origin shift handler
        //Vector3d lastOffset = FloatingOrigin.fetch.offset;

        // camera shake
        //Vector3 shakeOffset = Vector3.zero;
        float _shakeMagnitude = 0.0f;

        [field: PersistentField]
        public float ShakeMultiplier { get; set; } = 0.0f;

        public delegate void ResetCameraTools();
        public static event ResetCameraTools OnResetCTools;

        public static double SpeedOfSound { get; set; } = 330.0;

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

        float _startCameraTimestamp;
        float _timeSinceStart
        {
            get
            {
                return Time.time - _startCameraTimestamp;
            }
        }

        Vector2 _keysScrollPos;

        int _posCounter = 0; //debug

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
            _windowRect = new Rect( Screen.width - _windowWidth - 40, 0, _windowWidth, _windowHeight );
            _flightCamera = FlightCamera.fetch;
            _cameraToolsCameraActive = false;
            SaveOriginalCamera();

            AddToolbarButton();

            GameEvents.onHideUI.Add( GameUIDisable );
            GameEvents.onShowUI.Add( GameUIEnable );
            //GameEvents.onGamePause.Add (PostDeathRevert);
            GameEvents.OnVesselRecoveryRequested.Add( PostDeathRevert );
            //GameEvents.onFloatingOriginShift.Add( OnFloatingOriginShift );
            GameEvents.onGameSceneLoadRequested.Add( PostDeathRevert );

            _stationaryCameraParent = new GameObject( "StationaryCameraParent" );

            if( FlightGlobals.ActiveVessel != null )
            {
                _activeVessel = FlightGlobals.ActiveVessel;
                _stationaryCameraParent.transform.position = FlightGlobals.ActiveVessel.transform.position;
            }

            GameEvents.onVesselChange.Add( SwitchToVessel );
        }

        void OnDestroy()
        {
            GameEvents.onVesselChange.Remove( SwitchToVessel );
        }

        void Update()
        {
            if( !_isRecordingKeyInput )
            {
                if( Input.GetKeyDown( KeyCode.KeypadDivide ) )
                {
                    GuiEnabled = !GuiEnabled;
                }

                if( Input.GetKeyDown( KeyCode.End ) )
                {
                    EndCamera();
                }
                else if( Input.GetKeyDown( KeyCode.Home ) )
                {
                    StartCamera();
                }
            }

            if( Input.GetMouseButtonUp( 0 ) )
            {
                _mouseUp = true;
            }

            // get target transform from mouseClick
            if( _settingTargetEnabled && _mouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                _settingTargetEnabled = false;

                Part newTarget = Utils.GetPartFromMouse();
                if( newTarget != null )
                {
                    _stationaryCameraTarget = newTarget;
                }
            }

            //set position from mouseClick
            if( _settingPositionEnabled && _mouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                _stationaryCameraPosition = null;

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
            if( _cameraToolsCameraActive && _flightCamera.transform.parent != _stationaryCameraParent.transform )
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

            if( _cameraToolsCameraActive )
            {
                switch( CurrentCameraMode )
                {
                    case CameraMode.StationaryCamera:
                        UpdateStationaryCamera(); break;
                    case CameraMode.Pathing:
                        UpdatePathingCamera(); break;
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

        void UpdateStationaryCamera()
        {
            if( _posCounter < 3 )
            {
                _posCounter++;
                Debug.Log( "flightCamera position: " + _flightCamera.transform.position );
                _flightCamera.transform.position = resetPositionFix;
                if( hasSavedRotation )
                {
                    _flightCamera.transform.rotation = savedRotation;
                }
            }

            if( _flightCamera.Target != null )
            {
                _flightCamera.SetTargetNone(); //dont go to next vessel if vessel is destroyed
            }

            if( _hasTarget )
            {
                Vector3 lookPosition = _stationaryCameraTarget.transform.position;

                lookPosition += 2 * _stationaryCameraTarget.vessel.rb_velocity * Time.fixedDeltaTime;

                _flightCamera.transform.rotation = Quaternion.LookRotation( lookPosition - _flightCamera.transform.position, cameraUp );
            }

            if( _activeVessel != null )
            {
                _stationaryCameraParent.transform.position = _manualPosition + (_activeVessel.CoM - _activeVessel.rb_velocity * Time.fixedDeltaTime);

                if( CurrentReferenceMode == CameraReference.Surface )
                {
                    _flightCamera.transform.position -= Time.fixedDeltaTime * Mathf.Clamp( (float)_activeVessel.srf_velocity.magnitude, 0, MaxRelativeVelocity ) * _activeVessel.srf_velocity.normalized;
                }
                else if( CurrentReferenceMode == CameraReference.Orbit )
                {
                    _flightCamera.transform.position -= Time.fixedDeltaTime * Mathf.Clamp( (float)_activeVessel.obt_velocity.magnitude, 0, MaxRelativeVelocity ) * _activeVessel.obt_velocity.normalized;
                }
                else if( CurrentReferenceMode == CameraReference.InitialVelocity )
                {
                    Vector3 camVelocity;
                    if( UseOrbital && _initialOrbit != null )
                    {
                        camVelocity = (_initialOrbit.getOrbitalVelocityAtUT( Planetarium.GetUniversalTime() ).xzy - _activeVessel.GetObtVelocity());
                    }
                    else
                    {
                        camVelocity = (_initialVelocity - _activeVessel.srf_velocity);
                    }
                    _flightCamera.transform.position += camVelocity * Time.fixedDeltaTime;
                }
            }

            //mouse panning, moving
            Vector3 forwardLevelAxis = (Quaternion.AngleAxis( -90, cameraUp ) * _flightCamera.transform.right).normalized;
            Vector3 rightAxis = (Quaternion.AngleAxis( 90, forwardLevelAxis ) * cameraUp).normalized;

            if( Input.GetKey( KeyCode.Mouse1 ) && _stationaryCameraTarget == null )
            {
                _flightCamera.transform.rotation *= Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * 1.7f, Vector3.up ); //*(Mathf.Abs(Mouse.delta.x)/7)
                _flightCamera.transform.rotation *= Quaternion.AngleAxis( -Input.GetAxis( "Mouse Y" ) * 1.7f, Vector3.right );
                _flightCamera.transform.rotation = Quaternion.LookRotation( _flightCamera.transform.forward, cameraUp );
            }
            if( Input.GetKey( KeyCode.Mouse2 ) )
            {
                _manualPosition += _flightCamera.transform.right * Input.GetAxis( "Mouse X" ) * 2;
                _manualPosition += forwardLevelAxis * Input.GetAxis( "Mouse Y" ) * 2;
            }
            _manualPosition += cameraUp * 10 * Input.GetAxis( "Mouse ScrollWheel" );

            //autoFov
            if( _stationaryCameraTarget != null && UseAutoZoom )
            {
                float cameraDistance = Vector3.Distance( _stationaryCameraTarget.transform.position, _flightCamera.transform.position );
                float targetFoV = Mathf.Clamp( (7000 / (cameraDistance + 100)) - 14 + AutoZoomMargin, 2, 60 );
                manualFov = targetFoV;
            }

            //FOV
            if( !UseAutoZoom )
            {
                zoomFactor = Mathf.Exp( Zoom ) / Mathf.Exp( 1 );
                manualFov = 60 / zoomFactor;

                if( currentFov != manualFov )
                {
                    currentFov = Mathf.Lerp( currentFov, manualFov, 0.1f );
                    _flightCamera.SetFoV( currentFov );
                }
            }
            else
            {
                currentFov = Mathf.Lerp( currentFov, manualFov, 0.1f );
                _flightCamera.SetFoV( currentFov );
                zoomFactor = 60 / currentFov;
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
            }
            UpdateCameraShake();
        }

        public void ShakeCamera( float magnitude )
        {
            _shakeMagnitude = Mathf.Max( _shakeMagnitude, magnitude );
        }

        void UpdateCameraShake()
        {
            if( ShakeMultiplier > 0 )
            {
                _flightCamera.transform.rotation = Quaternion.AngleAxis( (ShakeMultiplier / 2) * _shakeMagnitude / 50f, Vector3.ProjectOnPlane( UnityEngine.Random.onUnitSphere, _flightCamera.transform.forward ) ) * _flightCamera.transform.rotation;
            }

            _shakeMagnitude = Mathf.Lerp( _shakeMagnitude, 0, 5 * Time.fixedDeltaTime );
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

        /// <summary>
        /// Starts the CameraTools camera with the current settings.
        /// </summary>
        private void StartCamera()
        {
            _startCameraTimestamp = Time.time;

            if( CurrentCameraMode == CameraMode.StationaryCamera )
            {
                if( !_cameraToolsCameraActive )
                {
                    SaveOriginalCamera();
                    StartStationaryCamera();
                }
                else
                {
                    StartStationaryCamera();
                }
            }
            else if( CurrentCameraMode == CameraMode.Pathing )
            {
                if( !_cameraToolsCameraActive )
                {
                    SaveOriginalCamera();
                }
                StartPathingCam();
                StartPlayingPathingCamera();
            }
        }

        /// <summary>
        /// Reverts the KSP camera to the state before the CameraTools took over the control.
        /// </summary>
        void EndCamera()
        {
            _posCounter = 0;

            if( _cameraToolsCameraActive )
            {
                _stationaryCameraPosition = _flightCamera.transform.position;
                if( _stationaryCameraTarget == null )
                {
                    savedRotation = _flightCamera.transform.rotation;
                    hasSavedRotation = true;
                }
                else
                {
                    hasSavedRotation = false;
                }
            }

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
            currentFov = 60;

            _cameraToolsCameraActive = false;

            OnResetCTools?.Invoke();

            StopPlayingPathingCamera();
        }


        void StartStationaryCamera()
        {
            Debug.Log( "flightCamera position init: " + _flightCamera.transform.position );

            if( FlightGlobals.ActiveVessel != null )
            {
                _hasDied = false;
                _activeVessel = FlightGlobals.ActiveVessel;
                cameraUp = -FlightGlobals.getGeeForceAtPosition( _activeVessel.GetWorldPos3D() ).normalized;
                if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( _activeVessel ) == FlightCamera.Modes.ORBITAL) )
                {
                    cameraUp = Vector3.up;
                }

                _flightCamera.SetTargetNone();
                _flightCamera.transform.parent = _stationaryCameraParent.transform;
                _flightCamera.DeactivateUpdate();
                _stationaryCameraParent.transform.position = _activeVessel.transform.position + _activeVessel.rb_velocity * Time.fixedDeltaTime;
                _manualPosition = Vector3.zero;

                if( _hasPosition )
                {
                    _flightCamera.transform.position = _stationaryCameraPosition.Value;
                }

                _initialVelocity = _activeVessel.srf_velocity;
                _initialOrbit = new Orbit();
                _initialOrbit.UpdateFromStateVectors( _activeVessel.orbit.pos, _activeVessel.orbit.vel, FlightGlobals.currentMainBody, Planetarium.GetUniversalTime() );
                _initialUT = Planetarium.GetUniversalTime();

                _cameraToolsCameraActive = true;
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }
            resetPositionFix = _flightCamera.transform.position;
            Debug.Log( "flightCamera position post init: " + _flightCamera.transform.position );
        }

        void StartPathingCam()
        {
            _activeVessel = FlightGlobals.ActiveVessel;
            cameraUp = -FlightGlobals.getGeeForceAtPosition( _activeVessel.GetWorldPos3D() ).normalized;
            if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( _activeVessel ) == FlightCamera.Modes.ORBITAL) )
            {
                cameraUp = Vector3.up;
            }

            _stationaryCameraParent.transform.position = _activeVessel.transform.position + _activeVessel.rb_velocity * Time.fixedDeltaTime;
            _stationaryCameraParent.transform.rotation = _activeVessel.transform.rotation;
            _flightCamera.SetTargetNone();
            _flightCamera.transform.parent = _stationaryCameraParent.transform;
            _flightCamera.DeactivateUpdate();

            _cameraToolsCameraActive = true;
        }

        void StartPlayingPathingCamera()
        {
            if( _currentCameraPathIndex < 0 || CurrentCameraPath.keyframeCount <= 0 )
            {
                EndCamera();
                return;
            }

            DeselectKeyframe();

            if( !_cameraToolsCameraActive )
            {
                StartPathingCam();
            }

            CameraTransformation firstFrame = CurrentCameraPath.Evaulate( 0 );
            _flightCamera.transform.localPosition = firstFrame.position;
            _flightCamera.transform.localRotation = firstFrame.rotation;
            Zoom = firstFrame.zoom;

            _isPlayingPath = true;
        }

        void StopPlayingPathingCamera()
        {
            _isPlayingPath = false;
        }

        void TogglePathList()
        {
            _pathKeyframeWindowVisible = false;
            _pathWindowVisible = !_pathWindowVisible;
        }

        void UpdatePathingCamera()
        {
            _stationaryCameraParent.transform.position = _activeVessel.transform.position + _activeVessel.rb_velocity * Time.fixedDeltaTime;
            _stationaryCameraParent.transform.rotation = _activeVessel.transform.rotation;

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
            manualFov = 60 / zoomFactor;

            if( currentFov != manualFov )
            {
                currentFov = Mathf.Lerp( currentFov, manualFov, 0.1f );
                _flightCamera.SetFoV( currentFov );
            }
        }

        void SaveOriginalCamera()
        {
            _originalCameraPosition = _flightCamera.transform.position;
            _originalCameraRotation = _flightCamera.transform.localRotation;
            _originalCameraParent = _flightCamera.transform.parent;
            _originalCameraNearClip = Camera.main.nearClipPlane;
        }

        void PostDeathRevert( GameScenes f )
        {
            if( _cameraToolsCameraActive )
            {
                EndCamera();
            }
        }

        void PostDeathRevert( Vessel v )
        {
            if( _cameraToolsCameraActive )
            {
                EndCamera();
            }
        }

        /// <summary>
        /// Unity Message - Draws GUI
        /// </summary>
        void OnGUI()
        {
            if( GuiEnabled && _gameUIToggle )
            {
                _windowRect = GUI.Window( 320, _windowRect, DrawGuiWindow, "" );

                if( _pathKeyframeWindowVisible )
                {
                    DrawKeyframeEditorWindow();
                }
                if( _pathWindowVisible )
                {
                    DrawPathSelectorWindow();
                }
            }
        }

        const float GUI_MARGIN = 12;
        const float ENTRY_HEIGHT = 20;

        /// <summary>
        /// Controls how the camera Tools GUI window looks.
        /// </summary>
        void DrawGuiWindow( int windowId )
        {
            GUIStyle labelCenterStyle = new GUIStyle()
            {
                alignment = TextAnchor.UpperCenter
            };
            labelCenterStyle.normal.textColor = Color.white;

            GUIStyle titleStyle = new GUIStyle( labelCenterStyle )
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle labelLeftStyle = new GUIStyle()
            {
                alignment = TextAnchor.UpperLeft
            };
            labelLeftStyle.normal.textColor = Color.white;

            GUIStyle labelLeftBoldStyle = new GUIStyle( labelLeftStyle )
            {
                fontStyle = FontStyle.Bold
            };

            GUI.DragWindow( new Rect( 0, 0, _windowWidth, _draggableHeight ) );

            float line = 1; // Used to calculate the position of the next line of the GUI.

            float contentWidth = _windowWidth - (2 * GUI_MARGIN);
            float contentTop = 20;

            GUI.Label( new Rect( 0, contentTop, _windowWidth, 40 ), "Camera Tools", titleStyle );
            line++;

            //tool mode switcher
            GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Tool: " + CurrentCameraMode.ToString(), labelLeftBoldStyle );
            line++;
            if( !_cameraToolsCameraActive )
            {
                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), "<" ) )
                {
                    CycleToolMode( false );
                }
                if( GUI.Button( new Rect( GUI_MARGIN + 25 + 4, contentTop + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), ">" ) )
                {
                    CycleToolMode( true );
                }
            }

            line++;
            line++;
            if( UseAutoZoom )
            {
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), "Autozoom Margin: " );
                line++;
                AutoZoomMargin = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + ((line) * ENTRY_HEIGHT), contentWidth - 45, ENTRY_HEIGHT ), AutoZoomMargin, 0, 50 );
                GUI.Label( new Rect( GUI_MARGIN + contentWidth - 40, contentTop + ((line - 0.15f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), AutoZoomMargin.ToString( "0.0" ), labelLeftStyle );
            }
            else
            {
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Zoom:", labelLeftStyle );
                line++;
                Zoom = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + ((line) * ENTRY_HEIGHT), contentWidth - 45, ENTRY_HEIGHT ), Zoom, 1, 8 );
                GUI.Label( new Rect( GUI_MARGIN + contentWidth - 40, contentTop + ((line - 0.15f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), zoomFactor.ToString( "0.0" ) + "x", labelLeftStyle );
            }
            line++;

            if( CurrentCameraMode != CameraMode.Pathing )
            {
                UseAutoZoom = GUI.Toggle( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), UseAutoZoom, "Auto Zoom" );//, leftLabel);
                line++;
            }
            line++;

            GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Camera shake:" );
            line++;
            ShakeMultiplier = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth - 45, ENTRY_HEIGHT ), ShakeMultiplier, 0f, 10f );
            GUI.Label( new Rect( GUI_MARGIN + contentWidth - 40, contentTop + ((line - 0.25f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), ShakeMultiplier.ToString( "0.00" ) + "x" );
            line++;

            line++;

            // Draw Stationary Camera GUI

            if( CurrentCameraMode == CameraMode.StationaryCamera )
            {
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Frame of Reference: " + CurrentReferenceMode.ToString(), labelLeftStyle );
                line++;
                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), "<" ) )
                {
                    CycleReferenceMode( false );
                }
                if( GUI.Button( new Rect( GUI_MARGIN + 25 + 4, contentTop + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), ">" ) )
                {
                    CycleReferenceMode( true );
                }

                line++;

                if( CurrentReferenceMode == CameraReference.Surface || CurrentReferenceMode == CameraReference.Orbit )
                {
                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), "Max Rel. V: ", labelLeftStyle );
                    MaxRelativeVelocity = float.Parse( GUI.TextField( new Rect( GUI_MARGIN + contentWidth / 2, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), MaxRelativeVelocity.ToString() ) );
                }
                else if( CurrentReferenceMode == CameraReference.InitialVelocity )
                {
                    UseOrbital = GUI.Toggle( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), UseOrbital, " Orbital" );
                }
                line++;

                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Camera Position:", labelLeftStyle );
                line++;

                // Draw position buttons.

                string positionButtonText = _hasPosition ? _stationaryCameraTarget.gameObject.name : "None";
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Camera Position: " + positionButtonText, labelLeftStyle );
                line++;

                positionButtonText = _settingTargetEnabled ? "waiting..." : "Set Position w/ Click";
                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT - 2 ), positionButtonText ) )
                {
                    _settingPositionEnabled = true;
                    _mouseUp = false;
                }
                if( GUI.Button( new Rect( 2 + GUI_MARGIN + contentWidth / 2, contentTop + (line * ENTRY_HEIGHT), (contentWidth / 2) - 2, ENTRY_HEIGHT - 2 ), "Clear Position" ) )
                {
                    _stationaryCameraPosition = null;
                }

                line++;

                // Draw target buttons.

                string targetButtonText = _hasTarget ? _stationaryCameraTarget.gameObject.name : "None";
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Camera Target: " + targetButtonText, labelLeftStyle );
                line++;

                targetButtonText = _settingTargetEnabled ? "waiting..." : "Set Target w/ Click";
                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT - 2 ), targetButtonText ) )
                {
                    _settingTargetEnabled = true;
                    _mouseUp = false;
                }
                if( GUI.Button( new Rect( 2 + GUI_MARGIN + contentWidth / 2, contentTop + (line * ENTRY_HEIGHT), (contentWidth / 2) - 2, ENTRY_HEIGHT - 2 ), "Clear Target" ) )
                {
                    _stationaryCameraTarget = null;
                }
            }

            // Draw pathing camera GUI.

            else if( CurrentCameraMode == CameraMode.Pathing )
            {
                if( _currentCameraPathIndex >= 0 )
                {
                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Path:" );
                    CurrentCameraPath.pathName = GUI.TextField( new Rect( GUI_MARGIN + 34, contentTop + (line * ENTRY_HEIGHT), contentWidth - 34, ENTRY_HEIGHT ), CurrentCameraPath.pathName );
                }
                else
                {
                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Path: None" );
                }
                line += 1.25f;

                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Open Path" ) )
                {
                    TogglePathList();
                }
                line++;

                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), "New Path" ) )
                {
                    CreateNewPath();
                }
                if( GUI.Button( new Rect( GUI_MARGIN + (contentWidth / 2), contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), "Delete Path" ) )
                {
                    DeletePath( _currentCameraPathIndex );
                }
                line++;

                if( _currentCameraPathIndex >= 0 )
                {
                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Interpolation rate: " + CurrentCameraPath.lerpRate.ToString( "0.0" ) );
                    line++;
                    CurrentCameraPath.lerpRate = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT) + 4, contentWidth - 50, ENTRY_HEIGHT ), CurrentCameraPath.lerpRate, 1f, 15f );
                    CurrentCameraPath.lerpRate = Mathf.Round( CurrentCameraPath.lerpRate * 10 ) / 10;
                    line++;

                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Path timescale " + CurrentCameraPath.timeScale.ToString( "0.00" ) );
                    line++;

                    CurrentCameraPath.timeScale = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT) + 4, contentWidth - 50, ENTRY_HEIGHT ), CurrentCameraPath.timeScale, 0.05f, 4f );
                    CurrentCameraPath.timeScale = Mathf.Round( CurrentCameraPath.timeScale * 20 ) / 20;
                    line++;

                    float viewHeight = Mathf.Max( 6 * ENTRY_HEIGHT, CurrentCameraPath.keyframeCount * ENTRY_HEIGHT );
                    Rect scrollRect = new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, 6 * ENTRY_HEIGHT );
                    GUI.Box( scrollRect, string.Empty );

                    float viewContentWidth = contentWidth - (2 * GUI_MARGIN);
                    _keysScrollPos = GUI.BeginScrollView( scrollRect, _keysScrollPos, new Rect( 0, 0, viewContentWidth, viewHeight ) );

                    // Draw path keyframe list.
                    if( CurrentCameraPath.keyframeCount > 0 )
                    {
                        Color origGuiColor = GUI.color;
                        for( int i = 0; i < CurrentCameraPath.keyframeCount; i++ )
                        {
                            if( i == _currentKeyframeIndex )
                            {
                                GUI.color = Color.green;
                            }
                            else
                            {
                                GUI.color = origGuiColor;
                            }
                            string kLabel = "#" + i.ToString() + ": " + CurrentCameraPath.GetKeyframe( i ).time.ToString( "0.00" ) + "s";
                            if( GUI.Button( new Rect( 0, (i * ENTRY_HEIGHT), 3 * viewContentWidth / 4, ENTRY_HEIGHT ), kLabel ) )
                            {
                                SelectKeyframe( i );
                            }
                            if( GUI.Button( new Rect( (3 * contentWidth / 4), (i * ENTRY_HEIGHT), (viewContentWidth / 4) - 20, ENTRY_HEIGHT ), "X" ) )
                            {
                                DeleteKeyframe( i );
                                break;
                            }
                        }
                        GUI.color = origGuiColor;
                    }

                    GUI.EndScrollView();
                    line += 6.5f;
                    if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), 3 * contentWidth / 4, ENTRY_HEIGHT ), "New Key" ) )
                    {
                        CreateNewKeyframe();
                    }
                }
            }

            line++;
            Rect saveRect = new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT );
            if( GUI.Button( saveRect, "Save" ) )
            {
                SaveAndSerialize();
            }

            Rect loadRect = new Rect( saveRect );
            loadRect.x += contentWidth / 2;
            if( GUI.Button( loadRect, "Reload" ) )
            {
                LoadAndDeserialize();
            }

            // fix length
            _windowHeight = contentTop + (line * ENTRY_HEIGHT) + ENTRY_HEIGHT + ENTRY_HEIGHT;
            _windowRect.height = _windowHeight;
        }

        public static string pathSaveURL = $"GameData/{DIRECTORY_NAME}/paths.cfg";

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

        void DrawKeyframeEditorWindow()
        {
            float width = 300;
            float height = 130;
            Rect kWindowRect = new Rect( _windowRect.x - width, _windowRect.y + 365, width, height );
            GUI.Box( kWindowRect, string.Empty );
            GUI.BeginGroup( kWindowRect );
            GUI.Label( new Rect( 5, 5, 100, 25 ), "Keyframe #" + _currentKeyframeIndex );
            if( GUI.Button( new Rect( 105, 5, 180, 25 ), "Revert Pos" ) )
            {
                ViewKeyframe( _currentKeyframeIndex );
            }
            GUI.Label( new Rect( 5, 35, 80, 25 ), "Time: " );
            _currKeyTimeString = GUI.TextField( new Rect( 100, 35, 195, 25 ), _currKeyTimeString, 16 );

            if( float.TryParse( _currKeyTimeString, out float parsed ) )
            {
                _currentKeyframeTime = parsed;
            }
            bool applied = false;

            if( GUI.Button( new Rect( 100, 65, 195, 25 ), "Apply" ) )
            {
                Debug.Log( "Applying keyframe at time: " + _currentKeyframeTime );
                CurrentCameraPath.SetTransform( _currentKeyframeIndex, _flightCamera.transform, Zoom, _currentKeyframeTime );
                applied = true;
            }

            if( GUI.Button( new Rect( 100, 105, 195, 20 ), "Cancel" ) )
            {
                applied = true;
            }

            GUI.EndGroup();

            if( applied )
            {
                DeselectKeyframe();
            }
        }

        void DrawPathSelectorWindow()
        {
            float width = 300;
            float height = 300;
            float indent = 5;
            float scrollRectSize = width - indent - indent;
            Rect pSelectRect = new Rect( _windowRect.x - width, _windowRect.y + 290, width, height );
            GUI.Box( pSelectRect, string.Empty );
            GUI.BeginGroup( pSelectRect );

            Rect scrollRect = new Rect( indent, indent, scrollRectSize, scrollRectSize );
            float scrollHeight = Mathf.Max( scrollRectSize, ENTRY_HEIGHT * _availableCameraPaths.Count );
            Rect scrollViewRect = new Rect( 0, 0, scrollRectSize - 20, scrollHeight );
            _pathSelectScrollPos = GUI.BeginScrollView( scrollRect, _pathSelectScrollPos, scrollViewRect );
            bool selected = false;

            for( int i = 0; i < _availableCameraPaths.Count; i++ )
            {
                if( GUI.Button( new Rect( 0, i * ENTRY_HEIGHT, scrollRectSize - 90, ENTRY_HEIGHT ), _availableCameraPaths[i].pathName ) )
                {
                    SelectPath( i );
                    selected = true;
                }
                if( GUI.Button( new Rect( scrollRectSize - 80, i * ENTRY_HEIGHT, 60, ENTRY_HEIGHT ), "Delete" ) )
                {
                    DeletePath( i );
                    break;
                }
            }

            GUI.EndScrollView();

            GUI.EndGroup();
            if( selected )
            {
                _pathWindowVisible = false;
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
            GuiEnabled = true;
            Debug.Log( "Showing CamTools GUI" );
        }

        void ButtonDisableGUI()
        {
            GuiEnabled = false;
            Debug.Log( "Hiding CamTools GUI" );
        }

        void EmptyMethod()
        { }

        void GameUIEnable()
        {
            _gameUIToggle = true;
        }

        void GameUIDisable()
        {
            _gameUIToggle = false;
        }

        void CycleReferenceMode( bool forward )
        {
            int length = Enum.GetValues( typeof( CameraReference ) ).Length;

            if( forward )
            {
                CurrentReferenceMode = (CameraReference)(((int)CurrentReferenceMode + 1) % length);
            }
            else
            {
                CurrentReferenceMode = (CameraReference)(((int)CurrentReferenceMode - 1 + length) % length); // adding length unfucks negative modulo
            }
        }

        void CycleToolMode( bool forward )
        {
            int length = Enum.GetValues( typeof( CameraMode ) ).Length;

            if( forward )
            {
                CurrentCameraMode = (CameraMode)(((int)CurrentCameraMode + 1) % length);
            }
            else
            {
                CurrentCameraMode = (CameraMode)(((int)CurrentCameraMode - 1 + length) % length); // adding length unfucks negative modulo
            }
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
                StopPlayingPathingCamera();
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
            if( !_cameraToolsCameraActive )
            {
                StartPathingCam();
            }

            _pathWindowVisible = false;

            float time = CurrentCameraPath.keyframeCount > 0 ? CurrentCameraPath.GetKeyframe( CurrentCameraPath.keyframeCount - 1 ).time + 1 : 0;
            CurrentCameraPath.AddTransform( _flightCamera.transform, Zoom, time );
            SelectKeyframe( CurrentCameraPath.keyframeCount - 1 );

            if( CurrentCameraPath.keyframeCount > 6 )
            {
                _keysScrollPos.y += ENTRY_HEIGHT;
            }
        }

        void ViewKeyframe( int index )
        {
            if( !_cameraToolsCameraActive )
            {
                StartPathingCam();
            }

            CameraKeyframe currentKey = CurrentCameraPath.GetKeyframe( index );
            _flightCamera.transform.localPosition = currentKey.position;
            _flightCamera.transform.localRotation = currentKey.rotation;
            Zoom = currentKey.zoom;
        }
    }
}