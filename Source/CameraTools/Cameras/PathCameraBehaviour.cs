using CameraToolsKatnissified.Animation;
using System.Collections.Generic;
using UnityEngine;

namespace CameraToolsKatnissified.Cameras
{
    [DisallowMultipleComponent]
    public sealed class PathCameraBehaviour : CameraBehaviour
    {
        public static string PATHS_FILE = $"GameData/{CameraToolsManager.DIRECTORY_NAME}/paths.cfg";

        /// <summary>
        /// If true, the camera is currently playing out a path, instead of setting it up in free mode.
        /// </summary>
        public bool IsPlayingPath { get; private set; } = false;

        public List<CameraPath> AvailablePaths { get; private set; }
        public CameraPath CurrentPath { get; private set; }

        public CameraKeyframe CurrentKeyframe { get; private set; }

        bool _pathWindowVisible = false;
        bool _pathKeyframeWindowVisible = false;

        Vector3 _pivotPosition;
        Quaternion _pivotRotation;
        Matrix4x4 _pivotSpaceL2W;
        Matrix4x4 _pivotSpaceW2L;

        public PathCameraBehaviour( CameraToolsManager ctm ) : base( ctm )
        {

        }

        public override void OnLoad( ConfigNode node )
        {
            base.OnLoad( node );

           // DeselectKeyframe();
            CurrentPath = null;
            AvailablePaths = new List<CameraPath>();

            ConfigNode pathFileNode = ConfigNode.Load( PATHS_FILE );

            foreach( var n in pathFileNode.GetNode( "CAMERAPATHS" ).GetNodes( "CAMERAPATH" ) )
            {
                AvailablePaths.Add( CameraPath.LoadOld( n ) );
            }
        }

        public override void OnSave( ConfigNode node )
        {
            base.OnSave( node );

            ConfigNode pathFileNode = ConfigNode.Load( PATHS_FILE );

            ConfigNode pathsNode = pathFileNode.GetNode( "CAMERAPATHS" );
            pathsNode.RemoveNodes( "CAMERAPATH" );

            foreach( var path in AvailablePaths )
            {
                path.Save( pathsNode );
            }

            pathFileNode.Save( PATHS_FILE );
        }

        protected override void OnStartPlaying()
        {
            Debug.Log( "[CTK] Path Camera Active" );
            if( FlightGlobals.ActiveVessel != null )
            {
                _pivotPosition = cameraBeh.ActiveVessel.transform.position; // cache to use when setting path.
                _pivotRotation = cameraBeh.ActiveVessel.transform.rotation;
                _pivotSpaceL2W = Matrix4x4.TRS( _pivotPosition, _pivotRotation, new Vector3( 1, 1, 1 ) );
                _pivotSpaceW2L = _pivotSpaceL2W.inverse;

                cameraBeh.FlightCamera.SetTargetNone();
                cameraBeh.FlightCamera.transform.SetParent( cameraBeh.CameraPivot.transform ); // Apparently this is not assigned by the global when starting playing. Needed when viewing a single keyframe
                cameraBeh.FlightCamera.DeactivateUpdate();
            }
            else
            {
                Debug.Log( "CameraTools: Stationary Camera failed. Active Vessel is null." );
            }
        }

        public void StartPlayingPath()
        {
            Debug.Log( "Path Camera Now Playing Path" );
            if( CurrentPath == null || CurrentPath.keyframeCount <= 0 )
            {
                cameraBeh.EndCamera();
                return;
            }

           // DeselectKeyframe();
            OnStartPlaying();

            // initialize the rotation on start, but don't update it so if the rocket rolls, the camera won't follow it.
            _pivotPosition = cameraBeh.ActiveVessel.transform.position;
            _pivotRotation = cameraBeh.ActiveVessel.transform.rotation;
            _pivotSpaceL2W = Matrix4x4.TRS( _pivotPosition, _pivotRotation, new Vector3( 1, 1, 1 ) );
            _pivotSpaceW2L = _pivotSpaceL2W.inverse;

            CameraTransformation firstFrame = CurrentPath.Evaulate( 0 );
            cameraBeh.FlightCamera.transform.position = _pivotSpaceL2W.MultiplyPoint( firstFrame.position );
            cameraBeh.FlightCamera.transform.rotation = _pivotRotation * firstFrame.rotation;
            cameraBeh.Zoom = firstFrame.zoom;

            //IsPlaying = true;
            IsPlayingPath = true;
        }

        private void UpdateRecalculatePivot()
        {
            // Update the frame of reference's position to follow the vessel.
            //if( CurrentPath.Frame == CameraPath.ReferenceFrame.Free )
            //{
            //}
            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixPosition )
            {
                _pivotPosition = cameraBeh.ActiveVessel.transform.position;
            }
            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixRotation )
            {
                _pivotRotation = cameraBeh.ActiveVessel.transform.rotation;
            }
            if( CurrentPath.Frame == CameraPath.ReferenceFrame.FixPositionAndRotation )
            {
                _pivotPosition = cameraBeh.ActiveVessel.transform.position;
                _pivotRotation = cameraBeh.ActiveVessel.transform.rotation;
            }
            _pivotSpaceL2W = Matrix4x4.TRS( _pivotPosition, _pivotRotation, new Vector3( 1, 1, 1 ) );
            _pivotSpaceW2L = _pivotSpaceL2W.inverse;
        }

        protected override void OnPlaying()
        {
            UpdateRecalculatePivot();

            if( IsPlayingPath )
            {
                CameraTransformation camTransf = CurrentPath.Evaulate( cameraBeh.TimeSinceStart * CurrentPath.TimeScale );
                cameraBeh.FlightCamera.transform.position = _pivotSpaceL2W.MultiplyPoint( Vector3.Lerp( _pivotSpaceW2L.MultiplyPoint( cameraBeh.FlightCamera.transform.position ), camTransf.position, CurrentPath.LerpRate * Time.fixedDeltaTime ) );
                cameraBeh.FlightCamera.transform.rotation = _pivotRotation * Quaternion.Slerp( Quaternion.Inverse( _pivotRotation ) * cameraBeh.FlightCamera.transform.rotation, camTransf.rotation, CurrentPath.LerpRate * Time.fixedDeltaTime );
                cameraBeh.Zoom = Mathf.Lerp( cameraBeh.Zoom, camTransf.zoom, CurrentPath.LerpRate * Time.fixedDeltaTime );
            }
            else // this is to set up the path.
            {
                //mouse panning, moving
                Vector3 forwardLevelAxis = cameraBeh.FlightCamera.transform.forward;

                if( Input.GetKey( KeyCode.Mouse1 ) && Input.GetKey( KeyCode.Mouse2 ) )
                {
                    cameraBeh.FlightCamera.transform.rotation = Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * -1.7f, cameraBeh.FlightCamera.transform.forward ) * cameraBeh.FlightCamera.transform.rotation;
                }
                else
                {
                    if( Input.GetKey( KeyCode.Mouse1 ) )
                    {
                        cameraBeh.FlightCamera.transform.rotation *= Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * 1.7f / (cameraBeh.Zoom * cameraBeh.Zoom), Vector3.up );
                        cameraBeh.FlightCamera.transform.rotation *= Quaternion.AngleAxis( -Input.GetAxis( "Mouse Y" ) * 1.7f / (cameraBeh.Zoom * cameraBeh.Zoom), Vector3.right );
                        cameraBeh.FlightCamera.transform.rotation = Quaternion.LookRotation( cameraBeh.FlightCamera.transform.forward, cameraBeh.FlightCamera.transform.up );
                    }
                    if( Input.GetKey( KeyCode.Mouse2 ) )
                    {
                        cameraBeh.FlightCamera.transform.position += cameraBeh.FlightCamera.transform.right * Input.GetAxis( "Mouse X" ) * 2;
                        cameraBeh.FlightCamera.transform.position += forwardLevelAxis * Input.GetAxis( "Mouse Y" ) * 2;
                    }
                }
                cameraBeh.FlightCamera.transform.position += cameraBeh.FlightCamera.transform.up * 10 * Input.GetAxis( "Mouse ScrollWheel" );

            }

            //zoom
            cameraBeh.ZoomFactor = Mathf.Exp( cameraBeh.Zoom ) / Mathf.Exp( 1 );
            cameraBeh.ManualFov = 60 / cameraBeh.ZoomFactor;

            if( cameraBeh.CurrentFov != cameraBeh.ManualFov )
            {
                cameraBeh.CurrentFov = Mathf.Lerp( cameraBeh.CurrentFov, cameraBeh.ManualFov, 0.1f );
                cameraBeh.FlightCamera.SetFoV( cameraBeh.CurrentFov );
            }

            //vessel camera shake
            if( cameraBeh.ShakeMultiplier > 0 )
            {
                foreach( var vessel in FlightGlobals.Vessels )
                {
                    if( !vessel || !vessel.loaded || vessel.packed )
                    {
                        continue;
                    }

                    cameraBeh.DoCameraShake( vessel );
                }

                cameraBeh.UpdateCameraShakeMagnitude();
            }
        }

        protected override void OnStopPlaying()
        {
            IsPlayingPath = false;
           // DeselectKeyframe();
        }
        
        public void CreateNewPath()
        {
            _pathKeyframeWindowVisible = false;
            CameraPath path = new CameraPath();
            AvailablePaths.Add( path );
            CurrentPath = path;
        }

        public void DeletePath( CameraPath path )
        {
            AvailablePaths.Remove( path );
            CurrentPath = null;
        }
        /*
        public void SelectKeyframe( CameraKeyframe kf )
        {
            cameraBeh.CurrentBehaviour.StopPlaying();

            CurrentKeyframe = kf;
            _pathKeyframeWindowVisible = true;
            ViewKeyframe( CurrentKeyframe );
        }*/
        /*
        public void DeselectKeyframe()
        {
            CurrentKeyframe = null;
            _pathKeyframeWindowVisible = false;
        }*/
        /*
        public void CreateNewKeyframe()
        {
            cameraBeh.CurrentBehaviour.StopPlaying();
            cameraBeh.SetBehaviour<PathCameraBehaviour>();
            cameraBeh.CurrentBehaviour.StartPlaying();

            _pathWindowVisible = false;

            float time = CurrentPath.keyframeCount > 0 ? CurrentPath.GetKeyframe( CurrentPath.keyframeCount - 1 ).Time + 1 : 0;
            CurrentPath.AddTransform( cameraBeh.FlightCamera.transform, cameraBeh.Zoom, time );
            SelectKeyframe( CurrentPath.GetKeyframe( CurrentPath.keyframeCount - 1 ) );

            if( CurrentPath.keyframeCount > 6 )
            {
                cameraBeh._pathScrollPosition.y += CameraToolsManager.ENTRY_HEIGHT;
            }
        }*/
        /*
        public void DeleteKeyframe( CameraKeyframe keyframe )
        {
            CurrentPath.RemoveKeyframe( keyframe );
            if( CurrentKeyframe == keyframe )
            {
                DeselectKeyframe();
                SelectKeyframe( CurrentPath.GetKeyframe( 0 ) );
            }
        }*/
        /*
        /// <summary>
        /// Positions the camera at a keyframe.
        /// </summary>
        public void ViewKeyframe( CameraKeyframe keyframe )
        {
            if( cameraBeh.CurrentBehaviour is PathCameraBehaviour pb )
            {
                pb.IsPlayingPath = false; // otherwise it deselects the current keyframe. Maybe it shouldn't, idk.
            }
            else
            {
                cameraBeh.EndCamera();
            }
#warning TODO - the path editor previewing cam needs to be its own thing.
            cameraBeh.SetBehaviour<PathCameraBehaviour>();
            cameraBeh.CurrentBehaviour.StartPlaying();

            Debug.Log( $"Pos: {keyframe.Position}, Rot: {keyframe.Rotation}" );
            cameraBeh.FlightCamera.transform.position = _pivotSpaceL2W.MultiplyPoint( keyframe.Position );
            cameraBeh.FlightCamera.transform.rotation = _pivotRotation * keyframe.Rotation;
            cameraBeh.Zoom = keyframe.Zoom;
        }
        */
        void TogglePathList()
        {
            _pathKeyframeWindowVisible = false;
            _pathWindowVisible = !_pathWindowVisible;
        }

        public override void DrawGui( ref float line )
        {
            if( CameraToolsManager._guiWindowVisible && CameraToolsManager._uiVisible )
            {
                if( _pathKeyframeWindowVisible )
                {
                    DrawKeyframeEditorWindow();
                }
                if( _pathWindowVisible )
                {
                    DrawPathSelectorWindow();
                }
            }

            if( CurrentPath != null )
            {
                GUI.Label( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, CameraToolsManager.ENTRY_HEIGHT ), "Path:" );
                CurrentPath.PathName = GUI.TextField( new Rect( CameraToolsManager.GUI_MARGIN + 34, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH - 34, CameraToolsManager.ENTRY_HEIGHT ), CurrentPath.PathName );
            }
            else
            {
                GUI.Label( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, CameraToolsManager.ENTRY_HEIGHT ), "Path: None" );
            }
            line += 1.25f;

            if( GUI.Button( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, CameraToolsManager.ENTRY_HEIGHT ), "Open Path" ) )
            {
                TogglePathList();
            }
            line++;

            if( GUI.Button( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH / 2, CameraToolsManager.ENTRY_HEIGHT ), "New Path" ) )
            {
                CreateNewPath();
            }
            if( GUI.Button( new Rect( CameraToolsManager.GUI_MARGIN + (CameraToolsManager.CONTENT_WIDTH / 2), CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH / 2, CameraToolsManager.ENTRY_HEIGHT ), "Delete Path" ) )
            {
                DeletePath( CurrentPath );
            }
            line++;

            if( CurrentPath != null )
            {
                GUI.Label( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, CameraToolsManager.ENTRY_HEIGHT ), "Interpolation Rate:" + CurrentPath.LerpRate.ToString( "0.0" ) );
                line++;
                CurrentPath.LerpRate = GUI.HorizontalSlider( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT) + 4, CameraToolsManager.CONTENT_WIDTH - 50, CameraToolsManager.ENTRY_HEIGHT ), CurrentPath.LerpRate, 1f, 15f );
                CurrentPath.LerpRate = Mathf.Round( CurrentPath.LerpRate * 10 ) / 10;
                line++;

                GUI.Label( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, CameraToolsManager.ENTRY_HEIGHT ), "Path Timescale:" + CurrentPath.TimeScale.ToString( "0.00" ) );
                line++;
                CurrentPath.TimeScale = GUI.HorizontalSlider( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT) + 4, CameraToolsManager.CONTENT_WIDTH - 50, CameraToolsManager.ENTRY_HEIGHT ), CurrentPath.TimeScale, 0.05f, 4f );
                CurrentPath.TimeScale = Mathf.Round( CurrentPath.TimeScale * 20 ) / 20;
                line++;

                GUI.Label( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, CameraToolsManager.ENTRY_HEIGHT ), "Path Frame:" + CurrentPath.Frame.ToString() );
                line++;
                if( GUI.Button( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), 25, CameraToolsManager.ENTRY_HEIGHT - 2 ), "<" ) )
                {
                    CurrentPath.Frame = Utils.CycleEnum( CurrentPath.Frame, -1 );
                }
                if( GUI.Button( new Rect( CameraToolsManager.GUI_MARGIN + 25 + 4, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), 25, CameraToolsManager.ENTRY_HEIGHT - 2 ), ">" ) )
                {
                    CurrentPath.Frame = Utils.CycleEnum( CurrentPath.Frame, 1 );
                }
                line++;

                float viewHeight = Mathf.Max( 6 * CameraToolsManager.ENTRY_HEIGHT, CurrentPath.keyframeCount * CameraToolsManager.ENTRY_HEIGHT );
                Rect scrollRect = new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), CameraToolsManager.CONTENT_WIDTH, 6 * CameraToolsManager.ENTRY_HEIGHT );
                GUI.Box( scrollRect, string.Empty );

                float viewcontentWidth = CameraToolsManager.CONTENT_WIDTH - (2 * CameraToolsManager.GUI_MARGIN);
                cameraBeh._pathScrollPosition = GUI.BeginScrollView( scrollRect, cameraBeh._pathScrollPosition, new Rect( 0, 0, viewcontentWidth, viewHeight ) );

                // Draw path keyframe list.
                if( CurrentPath.keyframeCount > 0 )
                {
                    Color origGuiColor = GUI.color;
                    for( int i = 0; i < CurrentPath.keyframeCount; i++ )
                    {
                        if( CurrentPath.GetKeyframe( i ) == CurrentKeyframe )
                        {
                            GUI.color = Color.green;
                        }
                        else
                        {
                            GUI.color = origGuiColor;
                        }
                        string kLabel = "#" + i.ToString() + ": " + CurrentPath.GetKeyframe( i ).Time.ToString( "0.00" ) + "s";
                        if( GUI.Button( new Rect( 0, (i * CameraToolsManager.ENTRY_HEIGHT), 3 * viewcontentWidth / 4, CameraToolsManager.ENTRY_HEIGHT ), kLabel ) )
                        {
#warning TODO - for some reason, clicking this doesn't bring up the keyframe editor window.
                            //SelectKeyframe( CurrentPath.GetKeyframe( i ) );
                        }
                        if( GUI.Button( new Rect( (3 * CameraToolsManager.CONTENT_WIDTH / 4), (i * CameraToolsManager.ENTRY_HEIGHT), (viewcontentWidth / 4) - 20, CameraToolsManager.ENTRY_HEIGHT ), "X" ) )
                        {
                            //DeleteKeyframe( CurrentPath.GetKeyframe( i ) );
                            break;
                        }
                    }
                    GUI.color = origGuiColor;
                }

                GUI.EndScrollView();
                line += 6.5f;
                if( GUI.Button( new Rect( CameraToolsManager.GUI_MARGIN, CameraToolsManager.CONTENT_TOP + (line * CameraToolsManager.ENTRY_HEIGHT), 3 * CameraToolsManager.CONTENT_WIDTH / 4, CameraToolsManager.ENTRY_HEIGHT ), "New Key" ) )
                {
                    //CreateNewKeyframe();
                }
            }
        }

        public void DrawKeyframeEditorWindow()
        {
            Debug.Log( "draw keyframe editor" );

            float width = 300;
            float height = 130;

            Rect kWindowRect = new Rect( cameraBeh._windowRect.x - width, cameraBeh._windowRect.y + 365, width, height );
            GUI.Box( kWindowRect, string.Empty );

            GUI.BeginGroup( kWindowRect );

            GUI.Label( new Rect( 5, 5, 100, 25 ), $"Keyframe t={CurrentKeyframe.Time}" );

            if( GUI.Button( new Rect( 105, 5, 180, 25 ), "Revert Pos" ) )
            {
               // ViewKeyframe( CurrentKeyframe );
            }

            GUI.Label( new Rect( 5, 35, 80, 25 ), "Time: " );
            string s = GUI.TextField( new Rect( 100, 35, 195, 25 ), CurrentKeyframe.Time.ToString(), 16 );

            if( float.TryParse( s, out float parsed ) )
            {
                CurrentKeyframe.Time = parsed;
            }

            bool isApplied = false;

            if( GUI.Button( new Rect( 100, 65, 195, 25 ), "Apply" ) )
            {
                Debug.Log( $"Applying keyframe at time: {CurrentKeyframe.Time}" );
                CurrentPath.SetTransform( CurrentKeyframe, cameraBeh.FlightCamera.transform, cameraBeh.Zoom, CurrentKeyframe.Time );
                isApplied = true;
            }

            if( GUI.Button( new Rect( 100, 105, 195, 20 ), "Cancel" ) )
            {
                isApplied = true;
            }

            GUI.EndGroup();

            if( isApplied )
            {
               // DeselectKeyframe();
            }
        }

        public void DrawPathSelectorWindow()
        {
            float width = 300;
            float height = 300;
            float indent = 5;
            float scrollRectSize = width - indent - indent;

            Rect pSelectRect = new Rect( cameraBeh._windowRect.x - width, cameraBeh._windowRect.y + 290, width, height );
            GUI.Box( pSelectRect, string.Empty );

            GUI.BeginGroup( pSelectRect );

            Rect scrollRect = new Rect( indent, indent, scrollRectSize, scrollRectSize );
            float scrollHeight = Mathf.Max( scrollRectSize, CameraToolsManager.ENTRY_HEIGHT * AvailablePaths.Count );
            Rect scrollViewRect = new Rect( 0, 0, scrollRectSize - 20, scrollHeight );
            cameraBeh._pathSelectScrollPos = GUI.BeginScrollView( scrollRect, cameraBeh._pathSelectScrollPos, scrollViewRect );

            bool isAnyPathSelected = false;

            for( int i = 0; i < AvailablePaths.Count; i++ )
            {
                if( GUI.Button( new Rect( 0, i * CameraToolsManager.ENTRY_HEIGHT, scrollRectSize - 90, CameraToolsManager.ENTRY_HEIGHT ), AvailablePaths[i].PathName ) )
                {
                    CurrentPath = AvailablePaths[i];
                    isAnyPathSelected = true;
                }
                if( GUI.Button( new Rect( scrollRectSize - 80, i * CameraToolsManager.ENTRY_HEIGHT, 60, CameraToolsManager.ENTRY_HEIGHT ), "Delete" ) )
                {
                    DeletePath( AvailablePaths[i] );
                    break;
                }
            }

            GUI.EndScrollView();

            GUI.EndGroup();
            if( isAnyPathSelected )
            {
                _pathWindowVisible = false;
            }
        }
    }
}