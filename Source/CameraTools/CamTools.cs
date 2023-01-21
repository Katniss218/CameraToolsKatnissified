using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using KSP.UI.Screens;

namespace CameraToolsKatnissified
{
    /// <summary>
    /// The main class controlling the camera.
    /// </summary>
    [KSPAddon( KSPAddon.Startup.Flight, false )]
    public class CamTools : MonoBehaviour
    {
        public const string DIRECTORY_NAME = "CameraToolsKatnissified";

        public static CamTools Instance { get; set; }

        GameObject _cameraParent;
        Vessel _vessel;
        Vector3 _origPosition;
        Quaternion _origRotation;
        Transform _origParent;
        float _origNearClip;
        FlightCamera _flightCamera;

        Part camTarget = null;

        [CameraToolsPersistent]
        public CameraReference referenceMode = CameraReference.Surface;
        Vector3 cameraUp = Vector3.up;

        string fmUpKey = "[7]";
        string fmDownKey = "[1]";
        string fmForwardKey = "[8]";
        string fmBackKey = "[5]";
        string fmLeftKey = "[4]";
        string fmRightKey = "[6]";
        string fmZoomInKey = "[9]";
        string fmZoomOutKey = "[3]";
        //


        //current camera setting
        bool cameraToolActive = false;


        //GUI
        public static bool GuiEnabled { get; set; } = false;
        public static bool _isToolbarButtonAdded { get; set; } = false;

        bool _updateFOV = false;

        Rect _windowRect = new Rect( 0, 0, 0, 0 );
        float _windowWidth = 250;
        float _windowHeight = 400;
        float _draggableHeight = 40;
        float _leftIndent = 12;
        float _entryHeight = 20;

        bool _gameUIToggle = true;
        float _incrButtonWidth = 26;

        /// <summary>
        /// Current mode of the CameraTools camera.
        /// </summary>
        [CameraToolsPersistent]
        public CameraMode CurrentMode = CameraMode.StationaryCamera;

        [field: CameraToolsPersistent]
        public bool AutoFOV { get; set; } = false;

        [field: CameraToolsPersistent]
        public float FreeMoveSpeed = 10;

        [field: CameraToolsPersistent]
        public float KeyZoomSpeed { get; set; } = 1;

        [field: CameraToolsPersistent]
        public float Zoom { get; set; } = 1;

        [field: CameraToolsPersistent]
        public bool EnableKeypad { get; set; } = false;

        [field: CameraToolsPersistent]
        public float MaxRelV { get; set; } = 2500;

        [field: CameraToolsPersistent]
        public bool UseOrbital { get; set; } = false;

        [field: CameraToolsPersistent]
        public bool TargetCoM { get; set; } = false;

        string _guiFreeMoveSpeed = "10";

        float manualFOV = 60;
        float currentFOV = 60;
        Vector3 manualPosition = Vector3.zero;

        string guiKeyZoomSpeed = "1";
        float zoomFactor = 1;

        bool isPositionSet = false;
        Vector3 presetOffset = Vector3.zero;
        bool hasSavedRotation = false;
        Quaternion savedRotation;

        Vector3 _lastVesselPosition = Vector3.zero;
        Vector3 _lastTargetPosition = Vector3.zero;
        bool _hasTarget = false;

        bool _hasDied = false;
        float _diedTime = 0;
        //vessel reference mode
        Vector3 _initialVelocity;
        Vector3 _initialPosition;
        Orbit _initialOrbit;
        double _initialUT;

        //retaining position and rotation after vessel destruction
        Vector3 lastPosition;
        Quaternion lastRotation;

        //click waiting stuff
        bool waitingForTarget = false;
        bool isWaitingToSetPosition = false;

        bool mouseUp = false;

        //Keys
        [field: CameraToolsPersistent]
        public string CameraKey { get; set; } = "home";

        [field: CameraToolsPersistent]
        public string RevertKey { get; set; } = "end";

        //recording input for key binding
        bool isRecordingInput = false;
        bool isRecordingActivate = false;
        bool isRecordingRevert = false;

        Vector3 resetPositionFix;//fixes position movement after setting and resetting camera

        //floating origin shift handler
        // Vector3d lastOffset = FloatingOrigin.fetch.offset;

        AudioSource[] audioSources;
        float[] originalAudioSourceDoppler;
        bool hasSetDoppler = false;

        [field: CameraToolsPersistent]
        public bool UseAudioEffects = true;

        //camera shake
        // Vector3 shakeOffset = Vector3.zero;
        float _shakeMagnitude = 0;

        [field: CameraToolsPersistent]
        public float shakeMultiplier { get; set; } = 1;

        public delegate void ResetCameraTools();
        public static event ResetCameraTools OnResetCTools;
        public static double speedOfSound = 330;

        //dogfight cam
        Vessel dogfightPrevTarget;
        Vessel dogfightTarget;

        [field: CameraToolsPersistent]
        public float DogfightDistance { get; set; } = 30;

        [field: CameraToolsPersistent]
        public float DogfightOffsetX { get; set; } = 10;

        [field: CameraToolsPersistent]
        public float DogfightOffsetY { get; set; } = 4;

        float _dogfightMaxOffset = 50;
        float _dogfightLerp = 20;

        [CameraToolsPersistent]
        float autoZoomMargin = 20;

        List<Vessel> loadedVessels;
        bool showingVesselList = false;
        bool dogfightLastTarget = false;
        Vector3 dogfightLastTargetPosition;
        Vector3 dogfightLastTargetVelocity;
        bool dogfightVelocityChase = false;

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
        float currentKeyframeTime;
        string currKeyTimeString;
        bool _showKeyframeEditor = false;
        float pathStartTime;
        bool _isPlayingPath = false;
        float pathTime
        {
            get
            {
                return Time.time - pathStartTime;
            }
        }

        Vector2 keysScrollPos;

        int posCounter = 0; //debug

        bool _showPathSelectorWindow = false;
        Vector2 _pathSelectScrollPos;

        void Awake()
        {
            // Instance = the last CamTools object that has Awake called.
            if( Instance != null )
            {
                Destroy( Instance );
            }

            Instance = this;

            LoadAndDeserialize();

            guiKeyZoomSpeed = KeyZoomSpeed.ToString();
            _guiFreeMoveSpeed = FreeMoveSpeed.ToString();
        }

        void Start()
        {
            _windowRect = new Rect( Screen.width - _windowWidth - 40, 0, _windowWidth, _windowHeight );
            _flightCamera = FlightCamera.fetch;
            cameraToolActive = false;
            SaveOriginalCamera();

            AddToolbarButton();

            GameEvents.onHideUI.Add( GameUIDisable );
            GameEvents.onShowUI.Add( GameUIEnable );
            //GameEvents.onGamePause.Add (PostDeathRevert);
            GameEvents.OnVesselRecoveryRequested.Add( PostDeathRevert );
            //GameEvents.onFloatingOriginShift.Add( OnFloatingOriginShift );
            GameEvents.onGameSceneLoadRequested.Add( PostDeathRevert );

            _cameraParent = new GameObject( "StationaryCameraParent" );
            //cameraParent.SetActive(true);
            //cameraParent = (GameObject) Instantiate(cameraParent, Vector3.zero, Quaternion.identity);

            if( FlightGlobals.ActiveVessel != null )
            {
                _cameraParent.transform.position = FlightGlobals.ActiveVessel.transform.position;
                _vessel = FlightGlobals.ActiveVessel;
            }

            GameEvents.onVesselChange.Add( SwitchToVessel );
        }

        void OnDestroy()
        {
            GameEvents.onVesselChange.Remove( SwitchToVessel );
        }

        void Update()
        {
            if( !isRecordingInput )
            {
                if( Input.GetKeyDown( KeyCode.KeypadDivide ) )
                {
                    GuiEnabled = !GuiEnabled;
                }

                if( Input.GetKeyDown( RevertKey ) )
                {
                    RevertCamera();
                }
                else if( Input.GetKeyDown( CameraKey ) )
                {
                    if( CurrentMode == CameraMode.StationaryCamera )
                    {
                        if( !cameraToolActive )
                        {
                            SaveOriginalCamera();
                            StartStationaryCamera();
                        }
                        else
                        {
                            //RevertCamera();
                            StartStationaryCamera();
                        }
                    }
                    else if( CurrentMode == CameraMode.DogfightCamera )
                    {
                        if( !cameraToolActive )
                        {
                            SaveOriginalCamera();
                            StartDogfightCamera();
                        }
                        else
                        {
                            StartDogfightCamera();
                        }
                    }
                    else if( CurrentMode == CameraMode.Pathing )
                    {
                        if( !cameraToolActive )
                        {
                            SaveOriginalCamera();
                        }
                        StartPathingCam();
                        StartPlayingPathingCamera();
                    }
                }
            }

            if( Input.GetMouseButtonUp( 0 ) )
            {
                mouseUp = true;
            }

            //get target transform from mouseClick
            if( waitingForTarget && mouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                Part tgt = GetPartFromMouse();
                if( tgt != null )
                {
                    camTarget = tgt;
                    _hasTarget = true;
                }
                else
                {
                    Vector3 pos = GetPosFromMouse();
                    if( pos != Vector3.zero )
                    {
                        _lastTargetPosition = pos;
                        _hasTarget = true;
                    }
                }

                waitingForTarget = false;
            }

            //set position from mouseClick
            if( isWaitingToSetPosition && mouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                Vector3 pos = GetPosFromMouse();
                if( pos != Vector3.zero )// && isStationaryCamera)
                {
                    presetOffset = pos;
                    isPositionSet = true;
                }
                else Debug.Log( "No pos from mouse click" );

                isWaitingToSetPosition = false;
            }
        }

        void FixedUpdate()
        {
            if( !FlightGlobals.ready )
            {
                return;
            }

            if( FlightGlobals.ActiveVessel != null && (_vessel == null || _vessel != FlightGlobals.ActiveVessel) )
            {
                _vessel = FlightGlobals.ActiveVessel;
            }

            if( _vessel != null )
            {
                _lastVesselPosition = _vessel.transform.position;
            }


            //stationary camera
            if( cameraToolActive )
            {
                if( CurrentMode == CameraMode.StationaryCamera )
                {
                    UpdateStationaryCamera();
                }
                else if( CurrentMode == CameraMode.DogfightCamera )
                {
                    UpdateDogfightCamera();
                }
                else if( CurrentMode == CameraMode.Pathing )
                {
                    UpdatePathingCamera();
                }
            }
            else
            {
                if( !AutoFOV )
                {
                    zoomFactor = Mathf.Exp( Zoom ) / Mathf.Exp( 1 );
                }
            }

            if( CurrentMode == CameraMode.DogfightCamera )
            {
                if( dogfightTarget && dogfightTarget.isActiveVessel )
                {
                    dogfightTarget = null;
                    if( cameraToolActive )
                    {
                        RevertCamera();
                    }
                }
            }

            if( _hasDied && Time.time - _diedTime > 2 )
            {
                RevertCamera();
            }
        }

        public void ShakeCamera( float magnitude )
        {
            _shakeMagnitude = Mathf.Max( _shakeMagnitude, magnitude );
        }

        void StartDogfightCamera()
        {
            if( FlightGlobals.ActiveVessel == null )
            {
                Debug.Log( "No active vessel." );
                return;
            }

            if( !dogfightTarget )
            {
                dogfightVelocityChase = true;
            }
            else
            {
                dogfightVelocityChase = false;
            }

            dogfightPrevTarget = dogfightTarget;

            _hasDied = false;
            _vessel = FlightGlobals.ActiveVessel;
            cameraUp = -FlightGlobals.getGeeForceAtPosition( _vessel.CoM ).normalized;

            _flightCamera.SetTargetNone();
            _flightCamera.transform.parent = _cameraParent.transform;
            _flightCamera.DeactivateUpdate();
            _cameraParent.transform.position = _vessel.transform.position + _vessel.rb_velocity * Time.fixedDeltaTime;

            cameraToolActive = true;

            ResetDoppler();
            OnResetCTools?.Invoke();

            SetDoppler( false );
            AddAtmoAudioControllers( false );
        }

        void UpdateDogfightCamera()
        {
            if( !_vessel || (!dogfightTarget && !dogfightLastTarget && !dogfightVelocityChase) )
            {
                RevertCamera();
                return;
            }


            if( dogfightTarget )
            {
                dogfightLastTarget = true;
                dogfightLastTargetPosition = dogfightTarget.CoM;
                dogfightLastTargetVelocity = dogfightTarget.rb_velocity;
            }
            else if( dogfightLastTarget )
            {
                dogfightLastTargetPosition += dogfightLastTargetVelocity * Time.fixedDeltaTime;
            }

            _cameraParent.transform.position = (_vessel.CoM - (_vessel.rb_velocity * Time.fixedDeltaTime));

            if( dogfightVelocityChase )
            {
                if( _vessel.srfSpeed > 1 )
                {
                    dogfightLastTargetPosition = _vessel.CoM + (_vessel.srf_velocity.normalized * 5000);
                }
                else
                {
                    dogfightLastTargetPosition = _vessel.CoM + (_vessel.ReferenceTransform.up * 5000);
                }
            }

            Vector3 offsetDirection = Vector3.Cross( cameraUp, dogfightLastTargetPosition - _vessel.CoM ).normalized;
            Vector3 camPos = _vessel.CoM + ((_vessel.CoM - dogfightLastTargetPosition).normalized * DogfightDistance) + (DogfightOffsetX * offsetDirection) + (DogfightOffsetY * cameraUp);

            Vector3 localCamPos = _cameraParent.transform.InverseTransformPoint( camPos );
            _flightCamera.transform.localPosition = Vector3.Lerp( _flightCamera.transform.localPosition, localCamPos, _dogfightLerp * Time.fixedDeltaTime );

            //rotation
            Quaternion vesselLook = Quaternion.LookRotation( _vessel.CoM - _flightCamera.transform.position, cameraUp );
            Quaternion targetLook = Quaternion.LookRotation( dogfightLastTargetPosition - _flightCamera.transform.position, cameraUp );
            Quaternion camRot = Quaternion.Lerp( vesselLook, targetLook, 0.5f );
            _flightCamera.transform.rotation = Quaternion.Lerp( _flightCamera.transform.rotation, camRot, _dogfightLerp * Time.fixedDeltaTime );

            //autoFov
            if( AutoFOV )
            {
                float targetFoV;
                if( dogfightVelocityChase )
                {
                    targetFoV = Mathf.Clamp( (7000 / (DogfightDistance + 100)) - 14 + autoZoomMargin, 2, 60 );
                }
                else
                {
                    float angle = Vector3.Angle( (dogfightLastTargetPosition + (dogfightLastTargetVelocity * Time.fixedDeltaTime)) - _flightCamera.transform.position, (_vessel.CoM + (_vessel.rb_velocity * Time.fixedDeltaTime)) - _flightCamera.transform.position );
                    targetFoV = Mathf.Clamp( angle + autoZoomMargin, 0.1f, 60f );
                }
                manualFOV = targetFoV;
            }
            //FOV
            if( !AutoFOV )
            {
                zoomFactor = Mathf.Exp( Zoom ) / Mathf.Exp( 1 );
                manualFOV = 60 / zoomFactor;
                _updateFOV = (currentFOV != manualFOV);
                if( _updateFOV )
                {
                    currentFOV = Mathf.Lerp( currentFOV, manualFOV, 0.1f );
                    _flightCamera.SetFoV( currentFOV );
                    _updateFOV = false;
                }
            }
            else
            {
                currentFOV = Mathf.Lerp( currentFOV, manualFOV, 0.1f );
                _flightCamera.SetFoV( currentFOV );
                zoomFactor = 60 / currentFOV;
            }

            //free move
            if( EnableKeypad )
            {
                if( Input.GetKey( fmUpKey ) )
                {
                    DogfightOffsetY += FreeMoveSpeed * Time.fixedDeltaTime;
                    DogfightOffsetY = Mathf.Clamp( DogfightOffsetY, -_dogfightMaxOffset, _dogfightMaxOffset );
                }
                else if( Input.GetKey( fmDownKey ) )
                {
                    DogfightOffsetY -= FreeMoveSpeed * Time.fixedDeltaTime;
                    DogfightOffsetY = Mathf.Clamp( DogfightOffsetY, -_dogfightMaxOffset, _dogfightMaxOffset );
                }
                if( Input.GetKey( fmForwardKey ) )
                {
                    DogfightDistance -= FreeMoveSpeed * Time.fixedDeltaTime;
                    DogfightDistance = Mathf.Clamp( DogfightDistance, 1f, 100f );
                }
                else if( Input.GetKey( fmBackKey ) )
                {
                    DogfightDistance += FreeMoveSpeed * Time.fixedDeltaTime;
                    DogfightDistance = Mathf.Clamp( DogfightDistance, 1f, 100f );
                }
                if( Input.GetKey( fmLeftKey ) )
                {
                    DogfightOffsetX -= FreeMoveSpeed * Time.fixedDeltaTime;
                    DogfightOffsetX = Mathf.Clamp( DogfightOffsetX, -_dogfightMaxOffset, _dogfightMaxOffset );
                }
                else if( Input.GetKey( fmRightKey ) )
                {
                    DogfightOffsetX += FreeMoveSpeed * Time.fixedDeltaTime;
                    DogfightOffsetX = Mathf.Clamp( DogfightOffsetX, -_dogfightMaxOffset, _dogfightMaxOffset );
                }

                //keyZoom
                if( !AutoFOV )
                {
                    if( Input.GetKey( fmZoomInKey ) )
                    {
                        Zoom = Mathf.Clamp( Zoom + (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                    }
                    else if( Input.GetKey( fmZoomOutKey ) )
                    {
                        Zoom = Mathf.Clamp( Zoom - (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                    }
                }
                else
                {
                    if( Input.GetKey( fmZoomInKey ) )
                    {
                        autoZoomMargin = Mathf.Clamp( autoZoomMargin + (KeyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
                    }
                    else if( Input.GetKey( fmZoomOutKey ) )
                    {
                        autoZoomMargin = Mathf.Clamp( autoZoomMargin - (KeyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
                    }
                }
            }

            //vessel camera shake
            if( shakeMultiplier > 0 )
            {
                foreach( var vessel in FlightGlobals.Vessels )
                {
                    if( !vessel || !vessel.loaded || vessel.packed || vessel.isActiveVessel )
                    {
                        continue;
                    }

                    DoCameraShake( vessel );
                }
            }

            UpdateCameraShake();

            if( dogfightTarget != dogfightPrevTarget )
            {
                //RevertCamera();
                StartDogfightCamera();
            }
        }

        void UpdateStationaryCamera()
        {
            if( UseAudioEffects )
            {
                speedOfSound = 233 * Math.Sqrt( 1 + (FlightGlobals.getExternalTemperature( _vessel.GetWorldPos3D(), _vessel.mainBody ) / 273.15) );
                //Debug.Log("speed of sound: " + speedOfSound);
            }

            if( posCounter < 3 )
            {
                posCounter++;
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

            if( camTarget != null )
            {
                Vector3 lookPosition = camTarget.transform.position;
                if( TargetCoM )
                {
                    lookPosition = camTarget.vessel.CoM;
                }

                lookPosition += 2 * camTarget.vessel.rb_velocity * Time.fixedDeltaTime;
                if( TargetCoM )
                {
                    lookPosition += camTarget.vessel.rb_velocity * Time.fixedDeltaTime;
                }

                _flightCamera.transform.rotation = Quaternion.LookRotation( lookPosition - _flightCamera.transform.position, cameraUp );
                _lastTargetPosition = lookPosition;
            }
            else if( _hasTarget )
            {
                _flightCamera.transform.rotation = Quaternion.LookRotation( _lastTargetPosition - _flightCamera.transform.position, cameraUp );
            }

            if( _vessel != null )
            {
                _cameraParent.transform.position = manualPosition + (_vessel.CoM - _vessel.rb_velocity * Time.fixedDeltaTime);

                if( referenceMode == CameraReference.Surface )
                {
                    _flightCamera.transform.position -= Time.fixedDeltaTime * Mathf.Clamp( (float)_vessel.srf_velocity.magnitude, 0, MaxRelV ) * _vessel.srf_velocity.normalized;
                }
                else if( referenceMode == CameraReference.Orbit )
                {
                    _flightCamera.transform.position -= Time.fixedDeltaTime * Mathf.Clamp( (float)_vessel.obt_velocity.magnitude, 0, MaxRelV ) * _vessel.obt_velocity.normalized;
                }
                else if( referenceMode == CameraReference.InitialVelocity )
                {
                    Vector3 camVelocity = Vector3.zero;
                    if( UseOrbital && _initialOrbit != null )
                    {
                        camVelocity = (_initialOrbit.getOrbitalVelocityAtUT( Planetarium.GetUniversalTime() ).xzy - _vessel.GetObtVelocity());
                    }
                    else
                    {
                        camVelocity = (_initialVelocity - _vessel.srf_velocity);
                    }
                    _flightCamera.transform.position += camVelocity * Time.fixedDeltaTime;
                }
            }


            //mouse panning, moving
            Vector3 forwardLevelAxis = (Quaternion.AngleAxis( -90, cameraUp ) * _flightCamera.transform.right).normalized;
            Vector3 rightAxis = (Quaternion.AngleAxis( 90, forwardLevelAxis ) * cameraUp).normalized;

            //free move
            if( EnableKeypad )
            {
                if( Input.GetKey( fmUpKey ) )
                {
                    manualPosition += cameraUp * FreeMoveSpeed * Time.fixedDeltaTime;
                }
                else if( Input.GetKey( fmDownKey ) )
                {
                    manualPosition -= cameraUp * FreeMoveSpeed * Time.fixedDeltaTime;
                }
                if( Input.GetKey( fmForwardKey ) )
                {
                    manualPosition += forwardLevelAxis * FreeMoveSpeed * Time.fixedDeltaTime;
                }
                else if( Input.GetKey( fmBackKey ) )
                {
                    manualPosition -= forwardLevelAxis * FreeMoveSpeed * Time.fixedDeltaTime;
                }
                if( Input.GetKey( fmLeftKey ) )
                {
                    manualPosition -= _flightCamera.transform.right * FreeMoveSpeed * Time.fixedDeltaTime;
                }
                else if( Input.GetKey( fmRightKey ) )
                {
                    manualPosition += _flightCamera.transform.right * FreeMoveSpeed * Time.fixedDeltaTime;
                }

                //keyZoom
                if( !AutoFOV )
                {
                    if( Input.GetKey( fmZoomInKey ) )
                    {
                        Zoom = Mathf.Clamp( Zoom + (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                    }
                    else if( Input.GetKey( fmZoomOutKey ) )
                    {
                        Zoom = Mathf.Clamp( Zoom - (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                    }
                }
                else
                {
                    if( Input.GetKey( fmZoomInKey ) )
                    {
                        autoZoomMargin = Mathf.Clamp( autoZoomMargin + (KeyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
                    }
                    else if( Input.GetKey( fmZoomOutKey ) )
                    {
                        autoZoomMargin = Mathf.Clamp( autoZoomMargin - (KeyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
                    }
                }
            }


            if( camTarget == null && Input.GetKey( KeyCode.Mouse1 ) )
            {
                _flightCamera.transform.rotation *= Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * 1.7f, Vector3.up ); //*(Mathf.Abs(Mouse.delta.x)/7)
                _flightCamera.transform.rotation *= Quaternion.AngleAxis( -Input.GetAxis( "Mouse Y" ) * 1.7f, Vector3.right );
                _flightCamera.transform.rotation = Quaternion.LookRotation( _flightCamera.transform.forward, cameraUp );
            }
            if( Input.GetKey( KeyCode.Mouse2 ) )
            {
                manualPosition += _flightCamera.transform.right * Input.GetAxis( "Mouse X" ) * 2;
                manualPosition += forwardLevelAxis * Input.GetAxis( "Mouse Y" ) * 2;
            }
            manualPosition += cameraUp * 10 * Input.GetAxis( "Mouse ScrollWheel" );

            //autoFov
            if( camTarget != null && AutoFOV )
            {
                float cameraDistance = Vector3.Distance( camTarget.transform.position, _flightCamera.transform.position );
                float targetFoV = Mathf.Clamp( (7000 / (cameraDistance + 100)) - 14 + autoZoomMargin, 2, 60 );
                //flightCamera.SetFoV(targetFoV);	
                manualFOV = targetFoV;
            }
            //FOV
            if( !AutoFOV )
            {
                zoomFactor = Mathf.Exp( Zoom ) / Mathf.Exp( 1 );
                manualFOV = 60 / zoomFactor;
                _updateFOV = (currentFOV != manualFOV);
                if( _updateFOV )
                {
                    currentFOV = Mathf.Lerp( currentFOV, manualFOV, 0.1f );
                    _flightCamera.SetFoV( currentFOV );
                    _updateFOV = false;
                }
            }
            else
            {
                currentFOV = Mathf.Lerp( currentFOV, manualFOV, 0.1f );
                _flightCamera.SetFoV( currentFOV );
                zoomFactor = 60 / currentFOV;
            }
            lastPosition = _flightCamera.transform.position;
            lastRotation = _flightCamera.transform.rotation;



            //vessel camera shake
            if( shakeMultiplier > 0 )
            {
                foreach( var v in FlightGlobals.Vessels )
                {
                    if( !v || !v.loaded || v.packed ) continue;
                    DoCameraShake( v );
                }
            }
            UpdateCameraShake();
        }


        void LateUpdate()
        {

            //retain pos and rot after vessel destruction
            if( cameraToolActive && _flightCamera.transform.parent != _cameraParent.transform )
            {
                _flightCamera.SetTargetNone();
                _flightCamera.transform.parent = null;
                _flightCamera.transform.position = lastPosition;
                _flightCamera.transform.rotation = lastRotation;
                _hasDied = true;
                _diedTime = Time.time;
            }

        }

        void UpdateCameraShake()
        {
            if( shakeMultiplier > 0 )
            {
                /*if( shakeMagnitude > 0.1f )
                {
                    Vector3 shakeAxis = UnityEngine.Random.onUnitSphere;
                    shakeOffset = Mathf.Sin( shakeMagnitude * 20 * Time.time ) * (shakeMagnitude / 10) * shakeAxis;
                }
                */

                _flightCamera.transform.rotation = Quaternion.AngleAxis( (shakeMultiplier / 2) * _shakeMagnitude / 50f, Vector3.ProjectOnPlane( UnityEngine.Random.onUnitSphere, _flightCamera.transform.forward ) ) * _flightCamera.transform.rotation;
            }

            _shakeMagnitude = Mathf.Lerp( _shakeMagnitude, 0, 5 * Time.fixedDeltaTime );
        }

        public void DoCameraShake( Vessel vessel )
        {
            //shake
            float camDistance = Vector3.Distance( _flightCamera.transform.position, vessel.CoM );

            float distanceFactor = 50f / camDistance;
            float fovFactor = 2f / zoomFactor;
            float thrustFactor = GetTotalThrust() / 1000f;

            float atmosphericFactor = (float)vessel.dynamicPressurekPa / 2f;

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

            atmosphericFactor *= lagAudioFactor;

            thrustFactor *= distanceFactor * fovFactor * lagAudioFactor;

            ShakeCamera( atmosphericFactor + thrustFactor );
        }

        float GetTotalThrust()
        {
            float total = 0;
            foreach( var engine in _vessel.FindPartModulesImplementing<ModuleEngines>() )
            {
                total += engine.finalThrust;
            }
            return total;
        }

        void AddAtmoAudioControllers( bool includeActiveVessel )
        {
            if( !UseAudioEffects )
            {
                return;
            }

            foreach( var vessel in FlightGlobals.Vessels )
            {
                if( !vessel || !vessel.loaded || vessel.packed || (!includeActiveVessel && vessel.isActiveVessel) )
                {
                    continue;
                }

                vessel.gameObject.AddComponent<CTAtmosphericAudioController>();
            }
        }

        void SetDoppler( bool includeActiveVessel )
        {
            if( hasSetDoppler )
            {
                return;
            }

            if( !UseAudioEffects )
            {
                return;
            }

            audioSources = FindObjectsOfType<AudioSource>();
            originalAudioSourceDoppler = new float[audioSources.Length];

            for( int i = 0; i < audioSources.Length; i++ )
            {
                originalAudioSourceDoppler[i] = audioSources[i].dopplerLevel;

                if( !includeActiveVessel )
                {
                    Part p = audioSources[i].GetComponentInParent<Part>();
                    if( p && p.vessel.isActiveVessel ) continue;
                }

                audioSources[i].dopplerLevel = 1;
                audioSources[i].velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
                audioSources[i].bypassEffects = false;
                audioSources[i].spatialBlend = 1;

                if( audioSources[i].gameObject.GetComponentInParent<Part>() )
                {
                    //Debug.Log("Added CTPartAudioController to :" + audioSources[i].name);
                    CTPartAudioController pa = audioSources[i].gameObject.AddComponent<CTPartAudioController>();
                    pa.audioSource = audioSources[i];
                }
            }

            hasSetDoppler = true;
        }

        void ResetDoppler()
        {
            if( !hasSetDoppler )
            {
                return;
            }

            for( int i = 0; i < audioSources.Length; i++ )
            {
                if( audioSources[i] != null )
                {
                    audioSources[i].dopplerLevel = originalAudioSourceDoppler[i];
                    audioSources[i].velocityUpdateMode = AudioVelocityUpdateMode.Auto;
                }
            }



            hasSetDoppler = false;
        }


        void StartStationaryCamera()
        {
            Debug.Log( "flightCamera position init: " + _flightCamera.transform.position );
            if( FlightGlobals.ActiveVessel != null )
            {
                _hasDied = false;
                _vessel = FlightGlobals.ActiveVessel;
                cameraUp = -FlightGlobals.getGeeForceAtPosition( _vessel.GetWorldPos3D() ).normalized;
                if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( _vessel ) == FlightCamera.Modes.ORBITAL) )
                {
                    cameraUp = Vector3.up;
                }

                _flightCamera.SetTargetNone();
                _flightCamera.transform.parent = _cameraParent.transform;
                _flightCamera.DeactivateUpdate();
                _cameraParent.transform.position = _vessel.transform.position + _vessel.rb_velocity * Time.fixedDeltaTime;
                manualPosition = Vector3.zero;


                _hasTarget = (camTarget != null) ? true : false;

                Vector3 rightAxis = -Vector3.Cross( _vessel.srf_velocity, _vessel.upAxis ).normalized;
                //Vector3 upAxis = flightCamera.transform.up;


                /*if( AutoFlybyPosition )
                {
                    isPositionSet = false;
                    Vector3 velocity = _vessel.srf_velocity;
                    if( referenceMode == CameraReference.Orbit ) velocity = _vessel.obt_velocity;

                    Vector3 clampedVelocity = Mathf.Clamp( (float)_vessel.srfSpeed, 0, MaxRelV ) * velocity.normalized;
                    float clampedSpeed = clampedVelocity.magnitude;
                    float sideDistance = Mathf.Clamp( 20 + (clampedSpeed / 10), 20, 150 );
                    float distanceAhead = Mathf.Clamp( 4 * clampedSpeed, 30, 3500 );

                    _flightCamera.transform.rotation = Quaternion.LookRotation( _vessel.transform.position - _flightCamera.transform.position, cameraUp );


                    if( referenceMode == CameraReference.Surface && _vessel.srfSpeed > 0 )
                    {
                        _flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.srf_velocity.normalized);
                    }
                    else if( referenceMode == CameraReference.Orbit && _vessel.obt_speed > 0 )
                    {
                        _flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.obt_velocity.normalized);
                    }
                    else
                    {
                        _flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.vesselTransform.up);
                    }


                    if( _flightCamera.mode == FlightCamera.Modes.FREE || FlightCamera.GetAutoModeForVessel( _vessel ) == FlightCamera.Modes.FREE )
                    {
                        _flightCamera.transform.position += (sideDistance * rightAxis) + (15 * cameraUp);
                    }
                    else if( _flightCamera.mode == FlightCamera.Modes.ORBITAL || FlightCamera.GetAutoModeForVessel( _vessel ) == FlightCamera.Modes.ORBITAL )
                    {
                        _flightCamera.transform.position += (sideDistance * FlightGlobals.getUpAxis()) + (15 * Vector3.up);
                    }


                }*/
                /*else if( manualOffset )
                {
                    isPositionSet = false;
                    float sideDistance = manualOffsetRight;
                    float distanceAhead = manualOffsetForward;


                    _flightCamera.transform.rotation = Quaternion.LookRotation( _vessel.transform.position - _flightCamera.transform.position, cameraUp );

                    if( referenceMode == CameraReference.Surface && _vessel.srfSpeed > 4 )
                    {
                        _flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.srf_velocity.normalized);
                    }
                    else if( referenceMode == CameraReference.Orbit && _vessel.obt_speed > 4 )
                    {
                        _flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.obt_velocity.normalized);
                    }
                    else
                    {
                        _flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.vesselTransform.up);
                    }

                    if( _flightCamera.mode == FlightCamera.Modes.FREE || FlightCamera.GetAutoModeForVessel( _vessel ) == FlightCamera.Modes.FREE )
                    {
                        _flightCamera.transform.position += (sideDistance * rightAxis) + (manualOffsetUp * cameraUp);
                    }
                    else if( _flightCamera.mode == FlightCamera.Modes.ORBITAL || FlightCamera.GetAutoModeForVessel( _vessel ) == FlightCamera.Modes.ORBITAL )
                    {
                        _flightCamera.transform.position += (sideDistance * FlightGlobals.getUpAxis()) + (manualOffsetUp * Vector3.up);
                    }
                }*/
                if( isPositionSet )
                {
                    _flightCamera.transform.position = presetOffset;
                    //setPresetOffset = false;
                }

                _initialVelocity = _vessel.srf_velocity;
                _initialOrbit = new Orbit();
                _initialOrbit.UpdateFromStateVectors( _vessel.orbit.pos, _vessel.orbit.vel, FlightGlobals.currentMainBody, Planetarium.GetUniversalTime() );
                _initialUT = Planetarium.GetUniversalTime();

                cameraToolActive = true;

                SetDoppler( true );
                AddAtmoAudioControllers( true );
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }
            resetPositionFix = _flightCamera.transform.position;
            Debug.Log( "flightCamera position post init: " + _flightCamera.transform.position );
        }

        /// <summary>
        /// Reverts the KSP camera to the state before the CameraTools took over the control.
        /// </summary>
        void RevertCamera()
        {
            posCounter = 0;

            if( cameraToolActive )
            {
                presetOffset = _flightCamera.transform.position;
                if( camTarget == null )
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
            _flightCamera.transform.parent = _origParent;
            _flightCamera.transform.position = _origPosition;
            _flightCamera.transform.rotation = _origRotation;
            Camera.main.nearClipPlane = _origNearClip;

            _flightCamera.SetFoV( 60 );
            _flightCamera.ActivateUpdate();
            currentFOV = 60;

            cameraToolActive = false;

            ResetDoppler();
            OnResetCTools?.Invoke();

            StopPlayingPathingCamera();
        }

        void SaveOriginalCamera()
        {
            _origPosition = _flightCamera.transform.position;
            _origRotation = _flightCamera.transform.localRotation;
            _origParent = _flightCamera.transform.parent;
            _origNearClip = Camera.main.nearClipPlane;
        }

        Part GetPartFromMouse()
        {
            Vector3 mouseAim = new Vector3( Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height, 0 );
            Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay( mouseAim );
            RaycastHit hit;
            if( Physics.Raycast( ray, out hit, 10000, 1 << 0 ) )
            {
                Part p = hit.transform.GetComponentInParent<Part>();
                return p;
            }
            else return null;
        }

        Vector3 GetPosFromMouse()
        {
            Vector3 mouseAim = new Vector3( Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height, 0 );
            Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay( mouseAim );

            const int layerMask = 0b10001000000000000001;

            if( Physics.Raycast( ray, out RaycastHit hit, 15000.0f, layerMask ) )
            {
                return hit.point - (10 * ray.direction);
            }

            return Vector3.zero;
        }

        void PostDeathRevert()
        {
            if( cameraToolActive )
            {
                RevertCamera();
            }
        }

        void PostDeathRevert( GameScenes f )
        {
            if( cameraToolActive )
            {
                RevertCamera();
            }
        }

        void PostDeathRevert( Vessel v )
        {
            if( cameraToolActive )
            {
                RevertCamera();
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

                if( _showKeyframeEditor )
                {
                    DrawKeyframeEditorWindow();
                }
                if( _showPathSelectorWindow )
                {
                    DrawPathSelectorWindow();
                }
            }
        }

        /// <summary>
        /// Controls how the camera Tools GUI window looks.
        /// </summary>
        void DrawGuiWindow( int windowId )
        {
            GUI.DragWindow( new Rect( 0, 0, _windowWidth, _draggableHeight ) );

            GUIStyle centerLabel = new GUIStyle();
            centerLabel.alignment = TextAnchor.UpperCenter;
            centerLabel.normal.textColor = Color.white;

            GUIStyle leftLabel = new GUIStyle();
            leftLabel.alignment = TextAnchor.UpperLeft;
            leftLabel.normal.textColor = Color.white;

            GUIStyle leftLabelBold = new GUIStyle( leftLabel );
            leftLabelBold.fontStyle = FontStyle.Bold;

            float line = 1; // Used to calculate the position of the next line of the GUI.

            float contentWidth = (_windowWidth) - (2 * _leftIndent);
            float contentTop = 20;
            GUIStyle titleStyle = new GUIStyle( centerLabel );
            titleStyle.fontSize = 24;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Label( new Rect( 0, contentTop, _windowWidth, 40 ), "Camera Tools", titleStyle );
            line++;
            float parseResult;

            //tool mode switcher
            GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Tool: " + CurrentMode.ToString(), leftLabelBold );
            line++;
            if( !cameraToolActive )
            {
                if( GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), 25, _entryHeight - 2 ), "<" ) )
                {
                    CycleToolMode( false );
                }
                if( GUI.Button( new Rect( _leftIndent + 25 + 4, contentTop + (line * _entryHeight), 25, _entryHeight - 2 ), ">" ) )
                {
                    CycleToolMode( true );
                }
            }
            line++;
            line++;
            if( AutoFOV )
            {
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), "Autozoom Margin: " );
                line++;
                autoZoomMargin = GUI.HorizontalSlider( new Rect( _leftIndent, contentTop + ((line) * _entryHeight), contentWidth - 45, _entryHeight ), autoZoomMargin, 0, 50 );
                GUI.Label( new Rect( _leftIndent + contentWidth - 40, contentTop + ((line - 0.15f) * _entryHeight), 40, _entryHeight ), autoZoomMargin.ToString( "0.0" ), leftLabel );
            }
            else
            {
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Zoom:", leftLabel );
                line++;
                Zoom = GUI.HorizontalSlider( new Rect( _leftIndent, contentTop + ((line) * _entryHeight), contentWidth - 45, _entryHeight ), Zoom, 1, 8 );
                GUI.Label( new Rect( _leftIndent + contentWidth - 40, contentTop + ((line - 0.15f) * _entryHeight), 40, _entryHeight ), zoomFactor.ToString( "0.0" ) + "x", leftLabel );
            }
            line++;

            if( CurrentMode != CameraMode.Pathing )
            {
                AutoFOV = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), AutoFOV, "Auto Zoom" );//, leftLabel);
                line++;
            }
            line++;
            UseAudioEffects = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), UseAudioEffects, "Use Audio Effects" );
            line++;
            GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Camera shake:" );
            line++;
            shakeMultiplier = GUI.HorizontalSlider( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth - 45, _entryHeight ), shakeMultiplier, 0f, 10f );
            GUI.Label( new Rect( _leftIndent + contentWidth - 40, contentTop + ((line - 0.25f) * _entryHeight), 40, _entryHeight ), shakeMultiplier.ToString( "0.00" ) + "x" );
            line++;
            line++;

            // Draw Stationary Camera GUI

            if( CurrentMode == CameraMode.StationaryCamera )
            {
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Frame of Reference: " + referenceMode.ToString(), leftLabel );
                line++;
                if( GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), 25, _entryHeight - 2 ), "<" ) )
                {
                    CycleReferenceMode( false );
                }
                if( GUI.Button( new Rect( _leftIndent + 25 + 4, contentTop + (line * _entryHeight), 25, _entryHeight - 2 ), ">" ) )
                {
                    CycleReferenceMode( true );
                }

                line++;

                if( referenceMode == CameraReference.Surface || referenceMode == CameraReference.Orbit )
                {
                    GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), "Max Rel. V: ", leftLabel );
                    MaxRelV = float.Parse( GUI.TextField( new Rect( _leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), MaxRelV.ToString() ) );
                }
                else if( referenceMode == CameraReference.InitialVelocity )
                {
                    UseOrbital = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), UseOrbital, " Orbital" );
                }
                line++;

                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Camera Position:", leftLabel );
                line++;
                string posButtonText = "Set Position w/ Click";
#warning TODO - Use a tri-state enum instead of 2 booleans.
                if( isPositionSet )
                {
                    posButtonText = "Clear Position";
                }
                if( isWaitingToSetPosition )
                {
                    posButtonText = "Waiting...";
                }

                if( FlightGlobals.ActiveVessel != null && GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight - 2 ), posButtonText ) )
                {
                    if( isPositionSet )
                    {
                        isPositionSet = false;
                    }
                    else
                    {
                        isWaitingToSetPosition = true;
                        mouseUp = false;
                    }
                }

                line++;

                string targetText = "None";
                if( camTarget != null )
                {
                    targetText = camTarget.gameObject.name;
                }
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Camera Target: " + targetText, leftLabel );
                line++;
                string tgtButtonText = "Set Target w/ Click";
                if( waitingForTarget )
                {
                    tgtButtonText = "waiting...";
                }
                if( GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight - 2 ), tgtButtonText ) )
                {
                    waitingForTarget = true;
                    mouseUp = false;
                }
                line++;
                if( GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), (contentWidth / 2) - 2, _entryHeight - 2 ), "Target Self" ) )
                {
                    camTarget = FlightGlobals.ActiveVessel.GetReferenceTransformPart();
                    _hasTarget = true;
                }
                if( GUI.Button( new Rect( 2 + _leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), (contentWidth / 2) - 2, _entryHeight - 2 ), "Clear Target" ) )
                {
                    camTarget = null;
                    _hasTarget = false;
                }
                line++;

                TargetCoM = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight - 2 ), TargetCoM, "Vessel Center of Mass" );
            }

            // Draw Dogfight Camera GUI.

            else if( CurrentMode == CameraMode.DogfightCamera )
            {
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Secondary target:" );
                line++;
                string tVesselLabel;
                if( showingVesselList )
                {
                    tVesselLabel = "Clear";
                }
                else if( dogfightTarget )
                {
                    tVesselLabel = dogfightTarget.vesselName;
                }
                else
                {
                    tVesselLabel = "None";
                }
                if( GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), tVesselLabel ) )
                {
                    if( showingVesselList )
                    {
                        showingVesselList = false;
                        dogfightTarget = null;
                    }
                    else
                    {
                        ReCacheVessels();
                        showingVesselList = true;
                    }
                }
                line++;

                if( showingVesselList )
                {
                    foreach( var vessel in loadedVessels )
                    {
                        if( !vessel || !vessel.loaded )
                        {
                            continue;
                        }
                        if( GUI.Button( new Rect( _leftIndent + 10, contentTop + (line * _entryHeight), contentWidth - 10, _entryHeight ), vessel.vesselName ) )
                        {
                            dogfightTarget = vessel;
                            showingVesselList = false;
                        }
                        line++;
                    }
                }

                line++;
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), "Distance: " + DogfightDistance.ToString( "0.0" ) );
                line++;
                DogfightDistance = GUI.HorizontalSlider( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), DogfightDistance, 1, 100 );
                line += 1.5f;

                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Offset:" );
                line++;
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), 15, _entryHeight ), "X: " );
                DogfightOffsetX = GUI.HorizontalSlider( new Rect( _leftIndent + 15, contentTop + (line * _entryHeight) + 6, contentWidth - 45, _entryHeight ), DogfightOffsetX, -_dogfightMaxOffset, _dogfightMaxOffset );
                GUI.Label( new Rect( _leftIndent + contentWidth - 25, contentTop + (line * _entryHeight), 25, _entryHeight ), DogfightOffsetX.ToString( "0.0" ) );
                line++;
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), 15, _entryHeight ), "Y: " );
                DogfightOffsetY = GUI.HorizontalSlider( new Rect( _leftIndent + 15, contentTop + (line * _entryHeight) + 6, contentWidth - 45, _entryHeight ), DogfightOffsetY, -_dogfightMaxOffset, _dogfightMaxOffset );
                GUI.Label( new Rect( _leftIndent + contentWidth - 25, contentTop + (line * _entryHeight), 25, _entryHeight ), DogfightOffsetY.ToString( "0.0" ) );
                line += 1.5f;
            }
            else if( CurrentMode == CameraMode.Pathing )
            {
                if( _currentCameraPathIndex >= 0 )
                {
                    GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Path:" );
                    CurrentCameraPath.pathName = GUI.TextField( new Rect( _leftIndent + 34, contentTop + (line * _entryHeight), contentWidth - 34, _entryHeight ), CurrentCameraPath.pathName );
                }
                else
                {
                    GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Path: None" );
                }
                line += 1.25f;
                if( GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Open Path" ) )
                {
                    TogglePathList();
                }
                line++;
                if( GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), "New Path" ) )
                {
                    CreateNewPath();
                }
                if( GUI.Button( new Rect( _leftIndent + (contentWidth / 2), contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), "Delete Path" ) )
                {
                    DeletePath( _currentCameraPathIndex );
                }
                line++;
                if( _currentCameraPathIndex >= 0 )
                {
                    GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Interpolation rate: " + CurrentCameraPath.lerpRate.ToString( "0.0" ) );
                    line++;
                    CurrentCameraPath.lerpRate = GUI.HorizontalSlider( new Rect( _leftIndent, contentTop + (line * _entryHeight) + 4, contentWidth - 50, _entryHeight ), CurrentCameraPath.lerpRate, 1f, 15f );
                    CurrentCameraPath.lerpRate = Mathf.Round( CurrentCameraPath.lerpRate * 10 ) / 10;
                    line++;
                    GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Path timescale " + CurrentCameraPath.timeScale.ToString( "0.00" ) );
                    line++;
                    CurrentCameraPath.timeScale = GUI.HorizontalSlider( new Rect( _leftIndent, contentTop + (line * _entryHeight) + 4, contentWidth - 50, _entryHeight ), CurrentCameraPath.timeScale, 0.05f, 4f );
                    CurrentCameraPath.timeScale = Mathf.Round( CurrentCameraPath.timeScale * 20 ) / 20;
                    line++;
                    float viewHeight = Mathf.Max( 6 * _entryHeight, CurrentCameraPath.keyframeCount * _entryHeight );
                    Rect scrollRect = new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, 6 * _entryHeight );
                    GUI.Box( scrollRect, string.Empty );
                    float viewContentWidth = contentWidth - (2 * _leftIndent);
                    keysScrollPos = GUI.BeginScrollView( scrollRect, keysScrollPos, new Rect( 0, 0, viewContentWidth, viewHeight ) );

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
                            if( GUI.Button( new Rect( 0, (i * _entryHeight), 3 * viewContentWidth / 4, _entryHeight ), kLabel ) )
                            {
                                SelectKeyframe( i );
                            }
                            if( GUI.Button( new Rect( (3 * contentWidth / 4), (i * _entryHeight), (viewContentWidth / 4) - 20, _entryHeight ), "X" ) )
                            {
                                DeleteKeyframe( i );
                                break;
                            }
                        }
                        GUI.color = origGuiColor;
                    }

                    GUI.EndScrollView();
                    line += 6;
                    line += 0.5f;
                    if( GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), 3 * contentWidth / 4, _entryHeight ), "New Key" ) )
                    {
                        CreateNewKeyframe();
                    }
                }
            }

            line += 1.25f;

            EnableKeypad = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), EnableKeypad, "Keypad Control" );
            if( EnableKeypad )
            {
                line++;

                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), "Move Speed:" );
                _guiFreeMoveSpeed = GUI.TextField( new Rect( _leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), _guiFreeMoveSpeed );
                if( float.TryParse( _guiFreeMoveSpeed, out parseResult ) )
                {
                    FreeMoveSpeed = Mathf.Abs( parseResult );
                    _guiFreeMoveSpeed = FreeMoveSpeed.ToString();
                }

                line++;

                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), "Zoom Speed:" );
                guiKeyZoomSpeed = GUI.TextField( new Rect( _leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), guiKeyZoomSpeed );
                if( float.TryParse( guiKeyZoomSpeed, out parseResult ) )
                {
                    KeyZoomSpeed = Mathf.Abs( parseResult );
                    guiKeyZoomSpeed = KeyZoomSpeed.ToString();
                }
            }
            else
            {
                line++;
                line++;
            }

            line++;
            line++;
            GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Keys:", centerLabel );
            line++;

            //activate key binding
            GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Activate: ", leftLabel );
            GUI.Label( new Rect( _leftIndent + 60, contentTop + (line * _entryHeight), 60, _entryHeight ), CameraKey, leftLabel );
            if( !isRecordingInput )
            {
                if( GUI.Button( new Rect( _leftIndent + 125, contentTop + (line * _entryHeight), 100, _entryHeight ), "Bind Key" ) )
                {
                    mouseUp = false;
                    isRecordingInput = true;
                    isRecordingActivate = true;
                }
            }
            else if( mouseUp && isRecordingActivate )
            {
                GUI.Label( new Rect( _leftIndent + 125, contentTop + (line * _entryHeight), 100, _entryHeight ), "Press a Key", leftLabel );

                string inputString = CCInputUtils.GetInputString();
                if( inputString.Length > 0 )
                {
                    CameraKey = inputString;
                    isRecordingInput = false;
                    isRecordingActivate = false;
                }
            }

            line++;

            //revert key binding
            GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Revert: ", leftLabel );
            GUI.Label( new Rect( _leftIndent + 60, contentTop + (line * _entryHeight), 60, _entryHeight ), RevertKey );
            if( !isRecordingInput )
            {
                if( GUI.Button( new Rect( _leftIndent + 125, contentTop + (line * _entryHeight), 100, _entryHeight ), "Bind Key" ) )
                {
                    mouseUp = false;
                    isRecordingInput = true;
                    isRecordingRevert = true;
                }
            }
            else if( mouseUp && isRecordingRevert )
            {
                GUI.Label( new Rect( _leftIndent + 125, contentTop + (line * _entryHeight), 100, _entryHeight ), "Press a Key", leftLabel );
                string inputString = CCInputUtils.GetInputString();
                if( inputString.Length > 0 )
                {
                    RevertKey = inputString;
                    isRecordingInput = false;
                    isRecordingRevert = false;
                }
            }

            line++;
            line++;
            Rect saveRect = new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight );
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
            _windowHeight = contentTop + (line * _entryHeight) + _entryHeight + _entryHeight;
            _windowRect.height = _windowHeight;
        }

        public static string pathSaveURL = $"GameData/{DIRECTORY_NAME}/paths.cfg";

        void SaveAndSerialize()
        {
            CameraToolsPersistent.Save();

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
            CameraToolsPersistent.Load();
            guiKeyZoomSpeed = KeyZoomSpeed.ToString();
            _guiFreeMoveSpeed = FreeMoveSpeed.ToString();

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
            currKeyTimeString = GUI.TextField( new Rect( 100, 35, 195, 25 ), currKeyTimeString, 16 );
            float parsed;
            if( float.TryParse( currKeyTimeString, out parsed ) )
            {
                currentKeyframeTime = parsed;
            }
            bool applied = false;
            if( GUI.Button( new Rect( 100, 65, 195, 25 ), "Apply" ) )
            {
                Debug.Log( "Applying keyframe at time: " + currentKeyframeTime );
                CurrentCameraPath.SetTransform( _currentKeyframeIndex, _flightCamera.transform, Zoom, currentKeyframeTime );
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
            float scrollHeight = Mathf.Max( scrollRectSize, _entryHeight * _availableCameraPaths.Count );
            Rect scrollViewRect = new Rect( 0, 0, scrollRectSize - 20, scrollHeight );
            _pathSelectScrollPos = GUI.BeginScrollView( scrollRect, _pathSelectScrollPos, scrollViewRect );
            bool selected = false;

            for( int i = 0; i < _availableCameraPaths.Count; i++ )
            {
                if( GUI.Button( new Rect( 0, i * _entryHeight, scrollRectSize - 90, _entryHeight ), _availableCameraPaths[i].pathName ) )
                {
                    SelectPath( i );
                    selected = true;
                }
                if( GUI.Button( new Rect( scrollRectSize - 80, i * _entryHeight, 60, _entryHeight ), "Delete" ) )
                {
                    DeletePath( i );
                    break;
                }
            }

            GUI.EndScrollView();

            GUI.EndGroup();
            if( selected )
            {
                _showPathSelectorWindow = false;
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
                ApplicationLauncher.Instance.AddModApplication( EnableGui, DisableGui, EmptyMethod, EmptyMethod, EmptyMethod, EmptyMethod, ApplicationLauncher.AppScenes.FLIGHT, buttonTexture );
                _isToolbarButtonAdded = true;
            }
        }

        void EnableGui()
        {
            GuiEnabled = true;
            Debug.Log( "Showing CamTools GUI" );
        }

        void DisableGui()
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
                referenceMode = (CameraReference)(((int)referenceMode + 1) % length);
            }
            else
            {
                referenceMode = (CameraReference)(((int)referenceMode - 1) % length);
            }
            /*f( forward )
            {
                referenceMode++;
                if( (int)referenceMode == length )
                {
                    referenceMode = 0;
                }
            }
            else
            {
                referenceMode--;
                if( (int)referenceMode == -1 )
                {
                    referenceMode = (CameraReference)length - 1;
                }
            }*/
        }

        void CycleToolMode( bool forward )
        {
            int length = Enum.GetValues( typeof( CameraMode ) ).Length;

            if( forward )
            {
                CurrentMode = (CameraMode)(((int)CurrentMode + 1) % length);
            }
            else
            {
                CurrentMode = (CameraMode)(((int)CurrentMode - 1) % length);
            }
            /*if( forward )
            {
                CurrentMode++;
                if( (int)CurrentMode == length )
                {
                    CurrentMode = 0;
                }
            }
            else
            {
                CurrentMode--;
                if( (int)CurrentMode == -1 )
                {
                    CurrentMode = (CameraMode)length - 1;
                }
            }*/
        }

        /*OnFloatingOriginShift( Vector3d offset, Vector3d data1 )
        {
            
			Debug.LogWarning ("======Floating origin shifted.======");
			Debug.LogWarning ("======Passed offset: "+offset+"======");
			Debug.LogWarning ("======FloatingOrigin offset: "+FloatingOrigin.fetch.offset+"======");
			Debug.LogWarning("========Floating Origin threshold: "+FloatingOrigin.fetch.threshold+"==========");
			
        }*/

        void ReCacheVessels()
        {
            loadedVessels = new List<Vessel>();

            foreach( var vessel in FlightGlobals.Vessels )
            {
                if( vessel.loaded && vessel.vesselType != VesselType.Debris && !vessel.isActiveVessel )
                {
                    loadedVessels.Add( vessel );
                }
            }
        }

        void SwitchToVessel( Vessel vessel )
        {
            _vessel = vessel;

            if( cameraToolActive )
            {
                if( CurrentMode == CameraMode.DogfightCamera )
                {
                    StartCoroutine( ResetDogfightCamRoutine() );
                }
            }
        }

        IEnumerator ResetDogfightCamRoutine()
        {
            yield return new WaitForEndOfFrame();

            RevertCamera();
            StartDogfightCamera();
        }

        void CreateNewPath()
        {
            _showKeyframeEditor = false;
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
            UpdateCurrentValues();
            _showKeyframeEditor = true;
            ViewKeyframe( _currentKeyframeIndex );
        }

        void DeselectKeyframe()
        {
            _currentKeyframeIndex = -1;
            _showKeyframeEditor = false;
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

        void UpdateCurrentValues()
        {
            if( CurrentCameraPath == null || _currentKeyframeIndex < 0 || _currentKeyframeIndex >= CurrentCameraPath.keyframeCount )
            {
                return;
            }

            CameraKeyframe currentKey = CurrentCameraPath.GetKeyframe( _currentKeyframeIndex );
            currentKeyframeTime = currentKey.time;

            currKeyTimeString = currentKeyframeTime.ToString();
        }

        void CreateNewKeyframe()
        {
            if( !cameraToolActive )
            {
                StartPathingCam();
            }

            _showPathSelectorWindow = false;

            float time = CurrentCameraPath.keyframeCount > 0 ? CurrentCameraPath.GetKeyframe( CurrentCameraPath.keyframeCount - 1 ).time + 1 : 0;
            CurrentCameraPath.AddTransform( _flightCamera.transform, Zoom, time );
            SelectKeyframe( CurrentCameraPath.keyframeCount - 1 );

            if( CurrentCameraPath.keyframeCount > 6 )
            {
                keysScrollPos.y += _entryHeight;
            }
        }

        void ViewKeyframe( int index )
        {
            if( !cameraToolActive )
            {
                StartPathingCam();
            }

            CameraKeyframe currentKey = CurrentCameraPath.GetKeyframe( index );
            _flightCamera.transform.localPosition = currentKey.position;
            _flightCamera.transform.localRotation = currentKey.rotation;
            Zoom = currentKey.zoom;
        }

        void StartPathingCam()
        {
            _vessel = FlightGlobals.ActiveVessel;
            cameraUp = -FlightGlobals.getGeeForceAtPosition( _vessel.GetWorldPos3D() ).normalized;
            if( FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel( _vessel ) == FlightCamera.Modes.ORBITAL) )
            {
                cameraUp = Vector3.up;
            }

            _cameraParent.transform.position = _vessel.transform.position + _vessel.rb_velocity * Time.fixedDeltaTime;
            _cameraParent.transform.rotation = _vessel.transform.rotation;
            _flightCamera.SetTargetNone();
            _flightCamera.transform.parent = _cameraParent.transform;
            _flightCamera.DeactivateUpdate();

            cameraToolActive = true;
        }

        void StartPlayingPathingCamera()
        {
            if( _currentCameraPathIndex < 0 || CurrentCameraPath.keyframeCount <= 0 )
            {
                RevertCamera();
                return;
            }

            DeselectKeyframe();

            if( !cameraToolActive )
            {
                StartPathingCam();
            }

            CameraTransformation firstFrame = CurrentCameraPath.Evaulate( 0 );
            _flightCamera.transform.localPosition = firstFrame.position;
            _flightCamera.transform.localRotation = firstFrame.rotation;
            Zoom = firstFrame.zoom;

            _isPlayingPath = true;
            pathStartTime = Time.time;
        }

        void StopPlayingPathingCamera()
        {
            _isPlayingPath = false;
        }

        void TogglePathList()
        {
            _showKeyframeEditor = false;
            _showPathSelectorWindow = !_showPathSelectorWindow;
        }

        void UpdatePathingCamera()
        {
            _cameraParent.transform.position = _vessel.transform.position + _vessel.rb_velocity * Time.fixedDeltaTime;
            _cameraParent.transform.rotation = _vessel.transform.rotation;

            if( _isPlayingPath )
            {
                CameraTransformation tf = CurrentCameraPath.Evaulate( pathTime * CurrentCameraPath.timeScale );
                _flightCamera.transform.localPosition = Vector3.Lerp( _flightCamera.transform.localPosition, tf.position, CurrentCameraPath.lerpRate * Time.fixedDeltaTime );
                _flightCamera.transform.localRotation = Quaternion.Slerp( _flightCamera.transform.localRotation, tf.rotation, CurrentCameraPath.lerpRate * Time.fixedDeltaTime );
                Zoom = Mathf.Lerp( Zoom, tf.zoom, CurrentCameraPath.lerpRate * Time.fixedDeltaTime );
            }
            else
            {
                //move
                //mouse panning, moving
                Vector3 forwardLevelAxis = _flightCamera.transform.forward;
                if( EnableKeypad )
                {
                    if( Input.GetKey( fmUpKey ) )
                    {
                        _flightCamera.transform.position += cameraUp * FreeMoveSpeed * Time.fixedDeltaTime;
                    }
                    else if( Input.GetKey( fmDownKey ) )
                    {
                        _flightCamera.transform.position -= cameraUp * FreeMoveSpeed * Time.fixedDeltaTime;
                    }

                    if( Input.GetKey( fmForwardKey ) )
                    {
                        _flightCamera.transform.position += forwardLevelAxis * FreeMoveSpeed * Time.fixedDeltaTime;
                    }
                    else if( Input.GetKey( fmBackKey ) )
                    {
                        _flightCamera.transform.position -= forwardLevelAxis * FreeMoveSpeed * Time.fixedDeltaTime;
                    }

                    if( Input.GetKey( fmLeftKey ) )
                    {
                        _flightCamera.transform.position -= _flightCamera.transform.right * FreeMoveSpeed * Time.fixedDeltaTime;
                    }
                    else if( Input.GetKey( fmRightKey ) )
                    {
                        _flightCamera.transform.position += _flightCamera.transform.right * FreeMoveSpeed * Time.fixedDeltaTime;
                    }

                    //keyZoom
                    if( !AutoFOV )
                    {
                        if( Input.GetKey( fmZoomInKey ) )
                        {
                            Zoom = Mathf.Clamp( Zoom + (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                        }
                        else if( Input.GetKey( fmZoomOutKey ) )
                        {
                            Zoom = Mathf.Clamp( Zoom - (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                        }
                    }
                    else
                    {
                        if( Input.GetKey( fmZoomInKey ) )
                        {
                            autoZoomMargin = Mathf.Clamp( autoZoomMargin + (KeyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
                        }
                        else if( Input.GetKey( fmZoomOutKey ) )
                        {
                            autoZoomMargin = Mathf.Clamp( autoZoomMargin - (KeyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
                        }
                    }
                }

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
            manualFOV = 60 / zoomFactor;
            _updateFOV = (currentFOV != manualFOV);

            if( _updateFOV )
            {
                currentFOV = Mathf.Lerp( currentFOV, manualFOV, 0.1f );
                _flightCamera.SetFoV( currentFOV );
                _updateFOV = false;
            }
        }
    }
}