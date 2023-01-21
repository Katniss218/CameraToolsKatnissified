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

        [CTPersistentField]
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
        public static bool HasAddedButton { get; set; } = false;

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
        [CTPersistentField]
        public CameraMode CurrentMode = CameraMode.StationaryCamera;

        //stationary camera vars
        [CTPersistentField]
        public bool autoFlybyPosition = false;

        [CTPersistentField]
        public bool autoFOV = false;

        [CTPersistentField]
        public float freeMoveSpeed = 10;

        string guiFreeMoveSpeed = "10";

        [CTPersistentField]
        public float keyZoomSpeed = 1;

        [CTPersistentField]
        public float _zoomExp = 1;

        [CTPersistentField]
        public bool enableKeypad = false;

        [CTPersistentField]
        public float maxRelV = 2500;

        [CTPersistentField]
        public bool manualOffset = false;

        [CTPersistentField]
        public float manualOffsetForward = 500;

        [CTPersistentField]
        public float manualOffsetRight = 50;

        [CTPersistentField]
        public float manualOffsetUp = 5;

        [CTPersistentField]
        public bool useOrbital = false;

        [CTPersistentField]
        public bool targetCoM = false;

        float manualFOV = 60;
        float currentFOV = 60;
        Vector3 manualPosition = Vector3.zero;

        string guiKeyZoomSpeed = "1";
        float zoomFactor = 1;

        bool setPresetOffset = false;
        Vector3 presetOffset = Vector3.zero;
        bool hasSavedRotation = false;
        Quaternion savedRotation;

        string guiOffsetForward = "500";
        string guiOffsetRight = "50";
        string guiOffsetUp = "5";

        Vector3 lastVesselPosition = Vector3.zero;
        Vector3 lastTargetPosition = Vector3.zero;
        bool hasTarget = false;

        bool hasDied = false;
        float diedTime = 0;
        //vessel reference mode
        Vector3 initialVelocity = Vector3.zero;
        Vector3 initialPosition = Vector3.zero;
        Orbit initialOrbit = null;
        double initialUT;

        //retaining position and rotation after vessel destruction
        Vector3 lastPosition;
        Quaternion lastRotation;


        //click waiting stuff
        bool waitingForTarget = false;
        bool waitingForPosition = false;

        bool mouseUp = false;

        //Keys
        [CTPersistentField]
        public string cameraKey = "home";

        [CTPersistentField]
        public string revertKey = "end";

        //recording input for key binding
        bool isRecordingInput = false;
        bool isRecordingActivate = false;
        bool isRecordingRevert = false;

        Vector3 resetPositionFix;//fixes position movement after setting and resetting camera

        //floating origin shift handler
        Vector3d lastOffset = FloatingOrigin.fetch.offset;

        AudioSource[] audioSources;
        float[] originalAudioSourceDoppler;
        bool hasSetDoppler = false;

        [CTPersistentField]
        public bool useAudioEffects = true;

        //camera shake
        Vector3 shakeOffset = Vector3.zero;
        float shakeMagnitude = 0;
        [CTPersistentField]
        public float shakeMultiplier = 1;

        public delegate void ResetCTools();
        public static event ResetCTools OnResetCTools;
        public static double speedOfSound = 330;

        //dogfight cam
        Vessel dogfightPrevTarget;
        Vessel dogfightTarget;
        [CTPersistentField]
        float dogfightDistance = 30;
        [CTPersistentField]
        float dogfightOffsetX = 10;
        [CTPersistentField]
        float dogfightOffsetY = 4;
        float dogfightMaxOffset = 50;
        float dogfightLerp = 20;
        [CTPersistentField]
        float autoZoomMargin = 20;
        List<Vessel> loadedVessels;
        bool showingVesselList = false;
        bool dogfightLastTarget = false;
        Vector3 dogfightLastTargetPosition;
        Vector3 dogfightLastTargetVelocity;
        bool dogfightVelocityChase = false;

        //pathing
        int _selectedPathIndex = -1;
        List<CameraPath> _availablePaths;

        CameraPath CurrentPath
        {
            get
            {
                if( _selectedPathIndex >= 0 && _selectedPathIndex < _availablePaths.Count )
                {
                    return _availablePaths[_selectedPathIndex];
                }

                return null;
            }
        }

        int currentKeyframeIndex = -1;
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

        void Awake()
        {
            if( Instance )
            {
                Destroy( Instance );
            }

            Instance = this;

            Load();

            guiOffsetForward = manualOffsetForward.ToString();
            guiOffsetRight = manualOffsetRight.ToString();
            guiOffsetUp = manualOffsetUp.ToString();
            guiKeyZoomSpeed = keyZoomSpeed.ToString();
            guiFreeMoveSpeed = freeMoveSpeed.ToString();
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
            GameEvents.onFloatingOriginShift.Add( OnFloatingOriginShift );
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

                if( Input.GetKeyDown( revertKey ) )
                {
                    RevertCamera();
                }
                else if( Input.GetKeyDown( cameraKey ) )
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
                    hasTarget = true;
                }
                else
                {
                    Vector3 pos = GetPosFromMouse();
                    if( pos != Vector3.zero )
                    {
                        lastTargetPosition = pos;
                        hasTarget = true;
                    }
                }

                waitingForTarget = false;
            }

            //set position from mouseClick
            if( waitingForPosition && mouseUp && Input.GetKeyDown( KeyCode.Mouse0 ) )
            {
                Vector3 pos = GetPosFromMouse();
                if( pos != Vector3.zero )// && isStationaryCamera)
                {
                    presetOffset = pos;
                    setPresetOffset = true;
                }
                else Debug.Log( "No pos from mouse click" );

                waitingForPosition = false;
            }



        }

        public void ShakeCamera( float magnitude )
        {
            shakeMagnitude = Mathf.Max( shakeMagnitude, magnitude );
        }


        int posCounter = 0;//debug
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
                lastVesselPosition = _vessel.transform.position;
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
                if( !autoFOV )
                {
                    zoomFactor = Mathf.Exp( _zoomExp ) / Mathf.Exp( 1 );
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


            if( hasDied && Time.time - diedTime > 2 )
            {
                RevertCamera();
            }
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

            hasDied = false;
            _vessel = FlightGlobals.ActiveVessel;
            cameraUp = -FlightGlobals.getGeeForceAtPosition( _vessel.CoM ).normalized;

            _flightCamera.SetTargetNone();
            _flightCamera.transform.parent = _cameraParent.transform;
            _flightCamera.DeactivateUpdate();
            _cameraParent.transform.position = _vessel.transform.position + _vessel.rb_velocity * Time.fixedDeltaTime;

            cameraToolActive = true;

            ResetDoppler();
            if( OnResetCTools != null )
            {
                OnResetCTools();
            }

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
            Vector3 camPos = _vessel.CoM + ((_vessel.CoM - dogfightLastTargetPosition).normalized * dogfightDistance) + (dogfightOffsetX * offsetDirection) + (dogfightOffsetY * cameraUp);

            Vector3 localCamPos = _cameraParent.transform.InverseTransformPoint( camPos );
            _flightCamera.transform.localPosition = Vector3.Lerp( _flightCamera.transform.localPosition, localCamPos, dogfightLerp * Time.fixedDeltaTime );

            //rotation
            Quaternion vesselLook = Quaternion.LookRotation( _vessel.CoM - _flightCamera.transform.position, cameraUp );
            Quaternion targetLook = Quaternion.LookRotation( dogfightLastTargetPosition - _flightCamera.transform.position, cameraUp );
            Quaternion camRot = Quaternion.Lerp( vesselLook, targetLook, 0.5f );
            _flightCamera.transform.rotation = Quaternion.Lerp( _flightCamera.transform.rotation, camRot, dogfightLerp * Time.fixedDeltaTime );

            //autoFov
            if( autoFOV )
            {
                float targetFoV;
                if( dogfightVelocityChase )
                {
                    targetFoV = Mathf.Clamp( (7000 / (dogfightDistance + 100)) - 14 + autoZoomMargin, 2, 60 );
                }
                else
                {
                    float angle = Vector3.Angle( (dogfightLastTargetPosition + (dogfightLastTargetVelocity * Time.fixedDeltaTime)) - _flightCamera.transform.position, (_vessel.CoM + (_vessel.rb_velocity * Time.fixedDeltaTime)) - _flightCamera.transform.position );
                    targetFoV = Mathf.Clamp( angle + autoZoomMargin, 0.1f, 60f );
                }
                manualFOV = targetFoV;
            }
            //FOV
            if( !autoFOV )
            {
                zoomFactor = Mathf.Exp( _zoomExp ) / Mathf.Exp( 1 );
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
            if( enableKeypad )
            {
                if( Input.GetKey( fmUpKey ) )
                {
                    dogfightOffsetY += freeMoveSpeed * Time.fixedDeltaTime;
                    dogfightOffsetY = Mathf.Clamp( dogfightOffsetY, -dogfightMaxOffset, dogfightMaxOffset );
                }
                else if( Input.GetKey( fmDownKey ) )
                {
                    dogfightOffsetY -= freeMoveSpeed * Time.fixedDeltaTime;
                    dogfightOffsetY = Mathf.Clamp( dogfightOffsetY, -dogfightMaxOffset, dogfightMaxOffset );
                }
                if( Input.GetKey( fmForwardKey ) )
                {
                    dogfightDistance -= freeMoveSpeed * Time.fixedDeltaTime;
                    dogfightDistance = Mathf.Clamp( dogfightDistance, 1f, 100f );
                }
                else if( Input.GetKey( fmBackKey ) )
                {
                    dogfightDistance += freeMoveSpeed * Time.fixedDeltaTime;
                    dogfightDistance = Mathf.Clamp( dogfightDistance, 1f, 100f );
                }
                if( Input.GetKey( fmLeftKey ) )
                {
                    dogfightOffsetX -= freeMoveSpeed * Time.fixedDeltaTime;
                    dogfightOffsetX = Mathf.Clamp( dogfightOffsetX, -dogfightMaxOffset, dogfightMaxOffset );
                }
                else if( Input.GetKey( fmRightKey ) )
                {
                    dogfightOffsetX += freeMoveSpeed * Time.fixedDeltaTime;
                    dogfightOffsetX = Mathf.Clamp( dogfightOffsetX, -dogfightMaxOffset, dogfightMaxOffset );
                }

                //keyZoom
                if( !autoFOV )
                {
                    if( Input.GetKey( fmZoomInKey ) )
                    {
                        _zoomExp = Mathf.Clamp( _zoomExp + (keyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                    }
                    else if( Input.GetKey( fmZoomOutKey ) )
                    {
                        _zoomExp = Mathf.Clamp( _zoomExp - (keyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                    }
                }
                else
                {
                    if( Input.GetKey( fmZoomInKey ) )
                    {
                        autoZoomMargin = Mathf.Clamp( autoZoomMargin + (keyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
                    }
                    else if( Input.GetKey( fmZoomOutKey ) )
                    {
                        autoZoomMargin = Mathf.Clamp( autoZoomMargin - (keyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
                    }
                }
            }

            //vessel camera shake
            if( shakeMultiplier > 0 )
            {
                foreach( var v in FlightGlobals.Vessels )
                {
                    if( !v || !v.loaded || v.packed || v.isActiveVessel ) continue;
                    VesselCameraShake( v );
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
            if( useAudioEffects )
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
            if( _flightCamera.Target != null ) _flightCamera.SetTargetNone(); //dont go to next vessel if vessel is destroyed

            if( camTarget != null )
            {
                Vector3 lookPosition = camTarget.transform.position;
                if( targetCoM )
                {
                    lookPosition = camTarget.vessel.CoM;
                }

                lookPosition += 2 * camTarget.vessel.rb_velocity * Time.fixedDeltaTime;
                if( targetCoM )
                {
                    lookPosition += camTarget.vessel.rb_velocity * Time.fixedDeltaTime;
                }

                _flightCamera.transform.rotation = Quaternion.LookRotation( lookPosition - _flightCamera.transform.position, cameraUp );
                lastTargetPosition = lookPosition;
            }
            else if( hasTarget )
            {
                _flightCamera.transform.rotation = Quaternion.LookRotation( lastTargetPosition - _flightCamera.transform.position, cameraUp );
            }



            if( _vessel != null )
            {
                _cameraParent.transform.position = manualPosition + (_vessel.CoM - _vessel.rb_velocity * Time.fixedDeltaTime);

                if( referenceMode == CameraReference.Surface )
                {
                    _flightCamera.transform.position -= Time.fixedDeltaTime * Mathf.Clamp( (float)_vessel.srf_velocity.magnitude, 0, maxRelV ) * _vessel.srf_velocity.normalized;
                }
                else if( referenceMode == CameraReference.Orbit )
                {
                    _flightCamera.transform.position -= Time.fixedDeltaTime * Mathf.Clamp( (float)_vessel.obt_velocity.magnitude, 0, maxRelV ) * _vessel.obt_velocity.normalized;
                }
                else if( referenceMode == CameraReference.InitialVelocity )
                {
                    Vector3 camVelocity = Vector3.zero;
                    if( useOrbital && initialOrbit != null )
                    {
                        camVelocity = (initialOrbit.getOrbitalVelocityAtUT( Planetarium.GetUniversalTime() ).xzy - _vessel.GetObtVelocity());
                    }
                    else
                    {
                        camVelocity = (initialVelocity - _vessel.srf_velocity);
                    }
                    _flightCamera.transform.position += camVelocity * Time.fixedDeltaTime;
                }
            }


            //mouse panning, moving
            Vector3 forwardLevelAxis = (Quaternion.AngleAxis( -90, cameraUp ) * _flightCamera.transform.right).normalized;
            Vector3 rightAxis = (Quaternion.AngleAxis( 90, forwardLevelAxis ) * cameraUp).normalized;

            //free move
            if( enableKeypad )
            {
                if( Input.GetKey( fmUpKey ) )
                {
                    manualPosition += cameraUp * freeMoveSpeed * Time.fixedDeltaTime;
                }
                else if( Input.GetKey( fmDownKey ) )
                {
                    manualPosition -= cameraUp * freeMoveSpeed * Time.fixedDeltaTime;
                }
                if( Input.GetKey( fmForwardKey ) )
                {
                    manualPosition += forwardLevelAxis * freeMoveSpeed * Time.fixedDeltaTime;
                }
                else if( Input.GetKey( fmBackKey ) )
                {
                    manualPosition -= forwardLevelAxis * freeMoveSpeed * Time.fixedDeltaTime;
                }
                if( Input.GetKey( fmLeftKey ) )
                {
                    manualPosition -= _flightCamera.transform.right * freeMoveSpeed * Time.fixedDeltaTime;
                }
                else if( Input.GetKey( fmRightKey ) )
                {
                    manualPosition += _flightCamera.transform.right * freeMoveSpeed * Time.fixedDeltaTime;
                }

                //keyZoom
                if( !autoFOV )
                {
                    if( Input.GetKey( fmZoomInKey ) )
                    {
                        _zoomExp = Mathf.Clamp( _zoomExp + (keyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                    }
                    else if( Input.GetKey( fmZoomOutKey ) )
                    {
                        _zoomExp = Mathf.Clamp( _zoomExp - (keyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                    }
                }
                else
                {
                    if( Input.GetKey( fmZoomInKey ) )
                    {
                        autoZoomMargin = Mathf.Clamp( autoZoomMargin + (keyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
                    }
                    else if( Input.GetKey( fmZoomOutKey ) )
                    {
                        autoZoomMargin = Mathf.Clamp( autoZoomMargin - (keyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
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
            if( camTarget != null && autoFOV )
            {
                float cameraDistance = Vector3.Distance( camTarget.transform.position, _flightCamera.transform.position );
                float targetFoV = Mathf.Clamp( (7000 / (cameraDistance + 100)) - 14 + autoZoomMargin, 2, 60 );
                //flightCamera.SetFoV(targetFoV);	
                manualFOV = targetFoV;
            }
            //FOV
            if( !autoFOV )
            {
                zoomFactor = Mathf.Exp( _zoomExp ) / Mathf.Exp( 1 );
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
                    VesselCameraShake( v );
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
                hasDied = true;
                diedTime = Time.time;
            }

        }

        void UpdateCameraShake()
        {
            if( shakeMultiplier > 0 )
            {
                if( shakeMagnitude > 0.1f )
                {
                    Vector3 shakeAxis = UnityEngine.Random.onUnitSphere;
                    shakeOffset = Mathf.Sin( shakeMagnitude * 20 * Time.time ) * (shakeMagnitude / 10) * shakeAxis;
                }


                _flightCamera.transform.rotation = Quaternion.AngleAxis( (shakeMultiplier / 2) * shakeMagnitude / 50f, Vector3.ProjectOnPlane( UnityEngine.Random.onUnitSphere, _flightCamera.transform.forward ) ) * _flightCamera.transform.rotation;
            }

            shakeMagnitude = Mathf.Lerp( shakeMagnitude, 0, 5 * Time.fixedDeltaTime );
        }

        public void VesselCameraShake( Vessel vessel )
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
            if( !useAudioEffects )
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

            if( !useAudioEffects )
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
                hasDied = false;
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


                hasTarget = (camTarget != null) ? true : false;


                Vector3 rightAxis = -Vector3.Cross( _vessel.srf_velocity, _vessel.upAxis ).normalized;
                //Vector3 upAxis = flightCamera.transform.up;


                if( autoFlybyPosition )
                {
                    setPresetOffset = false;
                    Vector3 velocity = _vessel.srf_velocity;
                    if( referenceMode == CameraReference.Orbit ) velocity = _vessel.obt_velocity;

                    Vector3 clampedVelocity = Mathf.Clamp( (float)_vessel.srfSpeed, 0, maxRelV ) * velocity.normalized;
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


                }
                else if( manualOffset )
                {
                    setPresetOffset = false;
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
                }
                else if( setPresetOffset )
                {
                    _flightCamera.transform.position = presetOffset;
                    //setPresetOffset = false;
                }

                initialVelocity = _vessel.srf_velocity;
                initialOrbit = new Orbit();
                initialOrbit.UpdateFromStateVectors( _vessel.orbit.pos, _vessel.orbit.vel, FlightGlobals.currentMainBody, Planetarium.GetUniversalTime() );
                initialUT = Planetarium.GetUniversalTime();

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
            hasDied = false;
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
            if( OnResetCTools != null )
            {
                OnResetCTools();
            }

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
            RaycastHit hit;
            if( Physics.Raycast( ray, out hit, 15000, 557057 ) )
            {
                return hit.point - (10 * ray.direction);
            }
            else return Vector3.zero;
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
                    KeyframeEditorWindow();
                }
                if( _showPathSelectorWindow )
                {
                    PathSelectorWindow();
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
            if( autoFOV )
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
                _zoomExp = GUI.HorizontalSlider( new Rect( _leftIndent, contentTop + ((line) * _entryHeight), contentWidth - 45, _entryHeight ), _zoomExp, 1, 8 );
                GUI.Label( new Rect( _leftIndent + contentWidth - 40, contentTop + ((line - 0.15f) * _entryHeight), 40, _entryHeight ), zoomFactor.ToString( "0.0" ) + "x", leftLabel );
            }
            line++;

            if( CurrentMode != CameraMode.Pathing )
            {
                autoFOV = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), autoFOV, "Auto Zoom" );//, leftLabel);
                line++;
            }
            line++;
            useAudioEffects = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), useAudioEffects, "Use Audio Effects" );
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
                    maxRelV = float.Parse( GUI.TextField( new Rect( _leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), maxRelV.ToString() ) );
                }
                else if( referenceMode == CameraReference.InitialVelocity )
                {
                    useOrbital = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), useOrbital, " Orbital" );
                }
                line++;

                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Camera Position:", leftLabel );
                line++;
                string posButtonText = "Set Position w/ Click";
                if( setPresetOffset ) posButtonText = "Clear Position";
                if( waitingForPosition ) posButtonText = "Waiting...";
                if( FlightGlobals.ActiveVessel != null && GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight - 2 ), posButtonText ) )
                {
                    if( setPresetOffset )
                    {
                        setPresetOffset = false;
                    }
                    else
                    {
                        waitingForPosition = true;
                        mouseUp = false;
                    }
                }
                line++;


                autoFlybyPosition = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), autoFlybyPosition, "Auto Flyby Position" );
                if( autoFlybyPosition ) manualOffset = false;
                line++;

                manualOffset = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), manualOffset, "Manual Flyby Position" );
                line++;

                Color origGuiColor = GUI.color;
                if( manualOffset )
                {
                    autoFlybyPosition = false;
                }
                else
                {
                    GUI.color = new Color( 0.5f, 0.5f, 0.5f, origGuiColor.a );
                }
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), 60, _entryHeight ), "Fwd:", leftLabel );
                float textFieldWidth = 42;
                Rect fwdFieldRect = new Rect( _leftIndent + contentWidth - textFieldWidth - (3 * _incrButtonWidth), contentTop + (line * _entryHeight), textFieldWidth, _entryHeight );
                guiOffsetForward = GUI.TextField( fwdFieldRect, guiOffsetForward.ToString() );
                if( float.TryParse( guiOffsetForward, out parseResult ) )
                {
                    manualOffsetForward = parseResult;
                }
                DrawIncrementButtons( fwdFieldRect, ref manualOffsetForward );
                guiOffsetForward = manualOffsetForward.ToString();

                line++;
                Rect rightFieldRect = new Rect( fwdFieldRect.x, contentTop + (line * _entryHeight), textFieldWidth, _entryHeight );
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), 60, _entryHeight ), "Right:", leftLabel );
                guiOffsetRight = GUI.TextField( rightFieldRect, guiOffsetRight );
                if( float.TryParse( guiOffsetRight, out parseResult ) )
                {
                    manualOffsetRight = parseResult;
                }
                DrawIncrementButtons( rightFieldRect, ref manualOffsetRight );
                guiOffsetRight = manualOffsetRight.ToString();
                line++;

                Rect upFieldRect = new Rect( fwdFieldRect.x, contentTop + (line * _entryHeight), textFieldWidth, _entryHeight );
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), 60, _entryHeight ), "Up:", leftLabel );
                guiOffsetUp = GUI.TextField( upFieldRect, guiOffsetUp );
                if( float.TryParse( guiOffsetUp, out parseResult ) )
                {
                    manualOffsetUp = parseResult;
                }
                DrawIncrementButtons( upFieldRect, ref manualOffsetUp );
                guiOffsetUp = manualOffsetUp.ToString();
                GUI.color = origGuiColor;

                line++;
                line++;

                string targetText = "None";
                if( camTarget != null ) targetText = camTarget.gameObject.name;
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Camera Target: " + targetText, leftLabel );
                line++;
                string tgtButtonText = "Set Target w/ Click";
                if( waitingForTarget ) tgtButtonText = "waiting...";
                if( GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight - 2 ), tgtButtonText ) )
                {
                    waitingForTarget = true;
                    mouseUp = false;
                }
                line++;
                if( GUI.Button( new Rect( _leftIndent, contentTop + (line * _entryHeight), (contentWidth / 2) - 2, _entryHeight - 2 ), "Target Self" ) )
                {
                    camTarget = FlightGlobals.ActiveVessel.GetReferenceTransformPart();
                    hasTarget = true;
                }
                if( GUI.Button( new Rect( 2 + _leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), (contentWidth / 2) - 2, _entryHeight - 2 ), "Clear Target" ) )
                {
                    camTarget = null;
                    hasTarget = false;
                }
                line++;

                targetCoM = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight - 2 ), targetCoM, "Vessel Center of Mass" );
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
                        UpdateLoadedVessels();
                        showingVesselList = true;
                    }
                }
                line++;

                if( showingVesselList )
                {
                    foreach( var v in loadedVessels )
                    {
                        if( !v || !v.loaded ) continue;
                        if( GUI.Button( new Rect( _leftIndent + 10, contentTop + (line * _entryHeight), contentWidth - 10, _entryHeight ), v.vesselName ) )
                        {
                            dogfightTarget = v;
                            showingVesselList = false;
                        }
                        line++;
                    }
                }

                line++;
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), "Distance: " + dogfightDistance.ToString( "0.0" ) );
                line++;
                dogfightDistance = GUI.HorizontalSlider( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), dogfightDistance, 1, 100 );
                line += 1.5f;

                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Offset:" );
                line++;
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), 15, _entryHeight ), "X: " );
                dogfightOffsetX = GUI.HorizontalSlider( new Rect( _leftIndent + 15, contentTop + (line * _entryHeight) + 6, contentWidth - 45, _entryHeight ), dogfightOffsetX, -dogfightMaxOffset, dogfightMaxOffset );
                GUI.Label( new Rect( _leftIndent + contentWidth - 25, contentTop + (line * _entryHeight), 25, _entryHeight ), dogfightOffsetX.ToString( "0.0" ) );
                line++;
                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), 15, _entryHeight ), "Y: " );
                dogfightOffsetY = GUI.HorizontalSlider( new Rect( _leftIndent + 15, contentTop + (line * _entryHeight) + 6, contentWidth - 45, _entryHeight ), dogfightOffsetY, -dogfightMaxOffset, dogfightMaxOffset );
                GUI.Label( new Rect( _leftIndent + contentWidth - 25, contentTop + (line * _entryHeight), 25, _entryHeight ), dogfightOffsetY.ToString( "0.0" ) );
                line += 1.5f;
            }
            else if( CurrentMode == CameraMode.Pathing )
            {
                if( _selectedPathIndex >= 0 )
                {
                    GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Path:" );
                    CurrentPath.pathName = GUI.TextField( new Rect( _leftIndent + 34, contentTop + (line * _entryHeight), contentWidth - 34, _entryHeight ), CurrentPath.pathName );
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
                    DeletePath( _selectedPathIndex );
                }
                line++;
                if( _selectedPathIndex >= 0 )
                {
                    GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Interpolation rate: " + CurrentPath.lerpRate.ToString( "0.0" ) );
                    line++;
                    CurrentPath.lerpRate = GUI.HorizontalSlider( new Rect( _leftIndent, contentTop + (line * _entryHeight) + 4, contentWidth - 50, _entryHeight ), CurrentPath.lerpRate, 1f, 15f );
                    CurrentPath.lerpRate = Mathf.Round( CurrentPath.lerpRate * 10 ) / 10;
                    line++;
                    GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Path timescale " + CurrentPath.timeScale.ToString( "0.00" ) );
                    line++;
                    CurrentPath.timeScale = GUI.HorizontalSlider( new Rect( _leftIndent, contentTop + (line * _entryHeight) + 4, contentWidth - 50, _entryHeight ), CurrentPath.timeScale, 0.05f, 4f );
                    CurrentPath.timeScale = Mathf.Round( CurrentPath.timeScale * 20 ) / 20;
                    line++;
                    float viewHeight = Mathf.Max( 6 * _entryHeight, CurrentPath.keyframeCount * _entryHeight );
                    Rect scrollRect = new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, 6 * _entryHeight );
                    GUI.Box( scrollRect, string.Empty );
                    float viewContentWidth = contentWidth - (2 * _leftIndent);
                    keysScrollPos = GUI.BeginScrollView( scrollRect, keysScrollPos, new Rect( 0, 0, viewContentWidth, viewHeight ) );
                    if( CurrentPath.keyframeCount > 0 )
                    {
                        Color origGuiColor = GUI.color;
                        for( int i = 0; i < CurrentPath.keyframeCount; i++ )
                        {
                            if( i == currentKeyframeIndex )
                            {
                                GUI.color = Color.green;
                            }
                            else
                            {
                                GUI.color = origGuiColor;
                            }
                            string kLabel = "#" + i.ToString() + ": " + CurrentPath.GetKeyframe( i ).time.ToString( "0.00" ) + "s";
                            if( GUI.Button( new Rect( 0, (i * _entryHeight), 3 * viewContentWidth / 4, _entryHeight ), kLabel ) )
                            {
                                SelectKeyframe( i );
                            }
                            if( GUI.Button( new Rect( (3 * contentWidth / 4), (i * _entryHeight), (viewContentWidth / 4) - 20, _entryHeight ), "X" ) )
                            {
                                DeleteKeyframe( i );
                                break;
                            }
                            //line++;
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

            enableKeypad = GUI.Toggle( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), enableKeypad, "Keypad Control" );
            if( enableKeypad )
            {
                line++;

                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), "Move Speed:" );
                guiFreeMoveSpeed = GUI.TextField( new Rect( _leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), guiFreeMoveSpeed );
                if( float.TryParse( guiFreeMoveSpeed, out parseResult ) )
                {
                    freeMoveSpeed = Mathf.Abs( parseResult );
                    guiFreeMoveSpeed = freeMoveSpeed.ToString();
                }

                line++;

                GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), "Zoom Speed:" );
                guiKeyZoomSpeed = GUI.TextField( new Rect( _leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight ), guiKeyZoomSpeed );
                if( float.TryParse( guiKeyZoomSpeed, out parseResult ) )
                {
                    keyZoomSpeed = Mathf.Abs( parseResult );
                    guiKeyZoomSpeed = keyZoomSpeed.ToString();
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
            GUI.Label( new Rect( _leftIndent + 60, contentTop + (line * _entryHeight), 60, _entryHeight ), cameraKey, leftLabel );
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
                    cameraKey = inputString;
                    isRecordingInput = false;
                    isRecordingActivate = false;
                }
            }

            line++;

            //revert key binding
            GUI.Label( new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight ), "Revert: ", leftLabel );
            GUI.Label( new Rect( _leftIndent + 60, contentTop + (line * _entryHeight), 60, _entryHeight ), revertKey );
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
                    revertKey = inputString;
                    isRecordingInput = false;
                    isRecordingRevert = false;
                }
            }

            line++;
            line++;
            Rect saveRect = new Rect( _leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight );
            if( GUI.Button( saveRect, "Save" ) )
            {
                Save();
            }

            Rect loadRect = new Rect( saveRect );
            loadRect.x += contentWidth / 2;
            if( GUI.Button( loadRect, "Reload" ) )
            {
                Load();
            }

            //fix length
            _windowHeight = contentTop + (line * _entryHeight) + _entryHeight + _entryHeight;
            _windowRect.height = _windowHeight;// = new Rect(windowRect.x, windowRect.y, windowWidth, windowHeight);
        }

        public static string pathSaveURL = "GameData/CameraToolsKatnissified/paths.cfg";
        void Save()
        {
            CTPersistentField.Save();

            ConfigNode pathFileNode = ConfigNode.Load( pathSaveURL );
            ConfigNode pathsNode = pathFileNode.GetNode( "CAMERAPATHS" );
            pathsNode.RemoveNodes( "CAMERAPATH" );

            foreach( var path in _availablePaths )
            {
                path.Save( pathsNode );
            }
            pathFileNode.Save( pathSaveURL );
        }

        void Load()
        {
            CTPersistentField.Load();
            guiOffsetForward = manualOffsetForward.ToString();
            guiOffsetRight = manualOffsetRight.ToString();
            guiOffsetUp = manualOffsetUp.ToString();
            guiKeyZoomSpeed = keyZoomSpeed.ToString();
            guiFreeMoveSpeed = freeMoveSpeed.ToString();

            DeselectKeyframe();
            _selectedPathIndex = -1;
            _availablePaths = new List<CameraPath>();
            ConfigNode pathFileNode = ConfigNode.Load( pathSaveURL );
            foreach( var node in pathFileNode.GetNode( "CAMERAPATHS" ).GetNodes( "CAMERAPATH" ) )
            {
                _availablePaths.Add( CameraPath.Load( node ) );
            }
        }

        void KeyframeEditorWindow()
        {
            float width = 300;
            float height = 130;
            Rect kWindowRect = new Rect( _windowRect.x - width, _windowRect.y + 365, width, height );
            GUI.Box( kWindowRect, string.Empty );
            GUI.BeginGroup( kWindowRect );
            GUI.Label( new Rect( 5, 5, 100, 25 ), "Keyframe #" + currentKeyframeIndex );
            if( GUI.Button( new Rect( 105, 5, 180, 25 ), "Revert Pos" ) )
            {
                ViewKeyframe( currentKeyframeIndex );
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
                CurrentPath.SetTransform( currentKeyframeIndex, _flightCamera.transform, _zoomExp, currentKeyframeTime );
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

        bool _showPathSelectorWindow = false;
        Vector2 pathSelectScrollPos;
        void PathSelectorWindow()
        {
            float width = 300;
            float height = 300;
            float indent = 5;
            float scrollRectSize = width - indent - indent;
            Rect pSelectRect = new Rect( _windowRect.x - width, _windowRect.y + 290, width, height );
            GUI.Box( pSelectRect, string.Empty );
            GUI.BeginGroup( pSelectRect );

            Rect scrollRect = new Rect( indent, indent, scrollRectSize, scrollRectSize );
            float scrollHeight = Mathf.Max( scrollRectSize, _entryHeight * _availablePaths.Count );
            Rect scrollViewRect = new Rect( 0, 0, scrollRectSize - 20, scrollHeight );
            pathSelectScrollPos = GUI.BeginScrollView( scrollRect, pathSelectScrollPos, scrollViewRect );
            bool selected = false;
            for( int i = 0; i < _availablePaths.Count; i++ )
            {
                if( GUI.Button( new Rect( 0, i * _entryHeight, scrollRectSize - 90, _entryHeight ), _availablePaths[i].pathName ) )
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

        void DrawIncrementButtons( Rect fieldRect, ref float val )
        {
            Rect incrButtonRect = new Rect( fieldRect.x - _incrButtonWidth, fieldRect.y, _incrButtonWidth, _entryHeight );
            if( GUI.Button( incrButtonRect, "-" ) )
            {
                val -= 5;
            }

            incrButtonRect.x -= _incrButtonWidth;

            if( GUI.Button( incrButtonRect, "--" ) )
            {
                val -= 50;
            }

            incrButtonRect.x = fieldRect.x + fieldRect.width;

            if( GUI.Button( incrButtonRect, "+" ) )
            {
                val += 5;
            }

            incrButtonRect.x += _incrButtonWidth;

            if( GUI.Button( incrButtonRect, "++" ) )
            {
                val += 50;
            }
        }

        //AppLauncherSetup
        void AddToolbarButton()
        {
            if( !HasAddedButton )
            {
                Texture buttonTexture = GameDatabase.Instance.GetTexture( $"{DIRECTORY_NAME}/Textures/icon", false );
                ApplicationLauncher.Instance.AddModApplication( EnableGui, DisableGui, Dummy, Dummy, Dummy, Dummy, ApplicationLauncher.AppScenes.FLIGHT, buttonTexture );
                CamTools.HasAddedButton = true;
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

        void Dummy()
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
            var length = System.Enum.GetValues( typeof( CameraReference ) ).Length;
            if( forward )
            {
                referenceMode++;
                if( (int)referenceMode == length ) referenceMode = 0;
            }
            else
            {
                referenceMode--;
                if( (int)referenceMode == -1 ) referenceMode = (CameraReference)length - 1;
            }
        }

        void CycleToolMode( bool forward )
        {
            var length = System.Enum.GetValues( typeof( CameraMode ) ).Length;
            if( forward )
            {
                CurrentMode++;
                if( (int)CurrentMode == length ) CurrentMode = 0;
            }
            else
            {
                CurrentMode--;
                if( (int)CurrentMode == -1 ) CurrentMode = (CameraMode)length - 1;
            }
        }

        void OnFloatingOriginShift( Vector3d offset, Vector3d data1 )
        {
            /*
			Debug.LogWarning ("======Floating origin shifted.======");
			Debug.LogWarning ("======Passed offset: "+offset+"======");
			Debug.LogWarning ("======FloatingOrigin offset: "+FloatingOrigin.fetch.offset+"======");
			Debug.LogWarning("========Floating Origin threshold: "+FloatingOrigin.fetch.threshold+"==========");
			*/
        }

        void UpdateLoadedVessels()
        {
            if( loadedVessels == null )
            {
                loadedVessels = new List<Vessel>();
            }
            else
            {
                loadedVessels.Clear();
            }

            foreach( var v in FlightGlobals.Vessels )
            {
                if( v.loaded && v.vesselType != VesselType.Debris && !v.isActiveVessel )
                {
                    loadedVessels.Add( v );
                }
            }
        }

        void SwitchToVessel( Vessel v )
        {
            _vessel = v;

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
            _availablePaths.Add( new CameraPath() );
            _selectedPathIndex = _availablePaths.Count - 1;
        }

        void DeletePath( int index )
        {
            if( index < 0 ) return;
            if( index >= _availablePaths.Count ) return;
            _availablePaths.RemoveAt( index );
            _selectedPathIndex = -1;
        }

        void SelectPath( int index )
        {
            _selectedPathIndex = index;
        }

        void SelectKeyframe( int index )
        {
            if( _isPlayingPath )
            {
                StopPlayingPathingCamera();
            }
            currentKeyframeIndex = index;
            UpdateCurrentValues();
            _showKeyframeEditor = true;
            ViewKeyframe( currentKeyframeIndex );
        }

        void DeselectKeyframe()
        {
            currentKeyframeIndex = -1;
            _showKeyframeEditor = false;
        }

        void DeleteKeyframe( int index )
        {
            CurrentPath.RemoveKeyframe( index );
            if( index == currentKeyframeIndex )
            {
                DeselectKeyframe();
            }
            if( CurrentPath.keyframeCount > 0 && currentKeyframeIndex >= 0 )
            {
                SelectKeyframe( Mathf.Clamp( currentKeyframeIndex, 0, CurrentPath.keyframeCount - 1 ) );
            }
        }

        void UpdateCurrentValues()
        {
            if( CurrentPath == null ) return;
            if( currentKeyframeIndex < 0 || currentKeyframeIndex >= CurrentPath.keyframeCount )
            {
                return;
            }
            CameraKeyframe currentKey = CurrentPath.GetKeyframe( currentKeyframeIndex );
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

            float time = CurrentPath.keyframeCount > 0 ? CurrentPath.GetKeyframe( CurrentPath.keyframeCount - 1 ).time + 1 : 0;
            CurrentPath.AddTransform( _flightCamera.transform, _zoomExp, time );
            SelectKeyframe( CurrentPath.keyframeCount - 1 );

            if( CurrentPath.keyframeCount > 6 )
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
            CameraKeyframe currentKey = CurrentPath.GetKeyframe( index );
            _flightCamera.transform.localPosition = currentKey.position;
            _flightCamera.transform.localRotation = currentKey.rotation;
            _zoomExp = currentKey.zoom;
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
            if( _selectedPathIndex < 0 )
            {
                RevertCamera();
                return;
            }

            if( CurrentPath.keyframeCount <= 0 )
            {
                RevertCamera();
                return;
            }

            DeselectKeyframe();

            if( !cameraToolActive )
            {
                StartPathingCam();
            }

            CameraTransformation firstFrame = CurrentPath.Evaulate( 0 );
            _flightCamera.transform.localPosition = firstFrame.position;
            _flightCamera.transform.localRotation = firstFrame.rotation;
            _zoomExp = firstFrame.zoom;

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
                CameraTransformation tf = CurrentPath.Evaulate( pathTime * CurrentPath.timeScale );
                _flightCamera.transform.localPosition = Vector3.Lerp( _flightCamera.transform.localPosition, tf.position, CurrentPath.lerpRate * Time.fixedDeltaTime );
                _flightCamera.transform.localRotation = Quaternion.Slerp( _flightCamera.transform.localRotation, tf.rotation, CurrentPath.lerpRate * Time.fixedDeltaTime );
                _zoomExp = Mathf.Lerp( _zoomExp, tf.zoom, CurrentPath.lerpRate * Time.fixedDeltaTime );
            }
            else
            {
                //move
                //mouse panning, moving
                Vector3 forwardLevelAxis = _flightCamera.transform.forward;
                Vector3 rightAxis = _flightCamera.transform.right;
                if( enableKeypad )
                {
                    if( Input.GetKey( fmUpKey ) )
                    {
                        _flightCamera.transform.position += cameraUp * freeMoveSpeed * Time.fixedDeltaTime;
                    }
                    else if( Input.GetKey( fmDownKey ) )
                    {
                        _flightCamera.transform.position -= cameraUp * freeMoveSpeed * Time.fixedDeltaTime;
                    }
                    if( Input.GetKey( fmForwardKey ) )
                    {
                        _flightCamera.transform.position += forwardLevelAxis * freeMoveSpeed * Time.fixedDeltaTime;
                    }
                    else if( Input.GetKey( fmBackKey ) )
                    {
                        _flightCamera.transform.position -= forwardLevelAxis * freeMoveSpeed * Time.fixedDeltaTime;
                    }
                    if( Input.GetKey( fmLeftKey ) )
                    {
                        _flightCamera.transform.position -= _flightCamera.transform.right * freeMoveSpeed * Time.fixedDeltaTime;
                    }
                    else if( Input.GetKey( fmRightKey ) )
                    {
                        _flightCamera.transform.position += _flightCamera.transform.right * freeMoveSpeed * Time.fixedDeltaTime;
                    }

                    //keyZoom
                    if( !autoFOV )
                    {
                        if( Input.GetKey( fmZoomInKey ) )
                        {
                            _zoomExp = Mathf.Clamp( _zoomExp + (keyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                        }
                        else if( Input.GetKey( fmZoomOutKey ) )
                        {
                            _zoomExp = Mathf.Clamp( _zoomExp - (keyZoomSpeed * Time.fixedDeltaTime), 1, 8 );
                        }
                    }
                    else
                    {
                        if( Input.GetKey( fmZoomInKey ) )
                        {
                            autoZoomMargin = Mathf.Clamp( autoZoomMargin + (keyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
                        }
                        else if( Input.GetKey( fmZoomOutKey ) )
                        {
                            autoZoomMargin = Mathf.Clamp( autoZoomMargin - (keyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50 );
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
                        _flightCamera.transform.rotation *= Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * 1.7f / (_zoomExp * _zoomExp), Vector3.up );
                        _flightCamera.transform.rotation *= Quaternion.AngleAxis( -Input.GetAxis( "Mouse Y" ) * 1.7f / (_zoomExp * _zoomExp), Vector3.right );
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
            zoomFactor = Mathf.Exp( _zoomExp ) / Mathf.Exp( 1 );
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