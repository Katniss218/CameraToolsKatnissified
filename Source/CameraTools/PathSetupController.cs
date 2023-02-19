using CameraToolsKatnissified.Animation;
using CameraToolsKatnissified.Cameras;
using CameraToolsKatnissified.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified
{
    class PathSetupController : MonoBehaviour
    {
        public List<CameraPath> AvailablePaths { get; private set; }
        public CameraPath CurrentPath { get; private set; }

        public CameraKeyframe CurrentKeyframe { get; private set; }

        CameraToolsManager _cameraBeh;
        bool _pathKeyframeWindowVisible = false;
        bool _pathWindowVisible = false;

        // GUI - the location of the path scroll view.
        public Vector2 _pathScrollPosition;
        public Vector2 _pathSelectScrollPos;

        Vector3 _pathRootPosition;
        Quaternion _pathRootRotation;
        Matrix4x4 _pathSpaceL2W;
        Matrix4x4 _pathSpaceW2L;

        public Rect _windowRect = new Rect( 0, 0, 0, 0 );
        public float _windowHeight = 400;
        public float _draggableHeight = 40;

        public Transform Pivot { get; set; }

        void Awake()
        {
            _cameraBeh = CameraToolsManager.Instance;
            LoadPaths();
        }

        void LoadPaths()
        {
            DeselectKeyframe();
            CurrentPath = null;
            AvailablePaths = new List<CameraPath>();

            ConfigNode pathFileNode = ConfigNode.Load( PathCameraController.PATHS_FILE );

            foreach( var n in pathFileNode.GetNode( "CAMERAPATHS" ).GetNodes( "CAMERAPATH" ) )
            {
                AvailablePaths.Add( CameraPath.LoadOld( n ) );
            }
        }

        void SavePaths()
        {
            ConfigNode pathFileNode = ConfigNode.Load( PathCameraController.PATHS_FILE );

            ConfigNode pathsNode = pathFileNode.GetNode( "CAMERAPATHS" );
            pathsNode.RemoveNodes( "CAMERAPATH" );

            foreach( var path in AvailablePaths )
            {
                path.Save( pathsNode );
            }

            pathFileNode.Save( PathCameraController.PATHS_FILE );
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

        public void SelectPath( CameraPath path )
        {
            DeselectKeyframe();
            CurrentPath = path;
            _pathWindowVisible = false;
        }

        public void SelectKeyframe( CameraKeyframe kf )
        {
            CurrentKeyframe = kf;
            _pathKeyframeWindowVisible = true;
            ViewKeyframe( CurrentKeyframe );
        }

        public void DeselectKeyframe()
        {
            CurrentKeyframe = null;
            _pathKeyframeWindowVisible = false;
#warning TODO - revert to what was viewed before the keyframe?
        }

        public void CreateNewKeyframe()
        {
            _pathWindowVisible = false;

            float time = CurrentPath.keyframeCount > 0 ? CurrentPath.GetKeyframe( CurrentPath.keyframeCount - 1 ).Time + 1 : 0;

            Vector3 localPosition = _pathSpaceW2L.MultiplyPoint( this.Pivot.localPosition );
            Quaternion localRotation = Quaternion.Inverse( _pathRootRotation ) * this.Pivot.localRotation;
            CurrentPath.AddTransform( localPosition, localRotation, _cameraBeh.Zoom, time );

            SelectKeyframe( CurrentPath.GetKeyframe( CurrentPath.keyframeCount - 1 ) );

            if( CurrentPath.keyframeCount > 6 )
            {
                _pathScrollPosition.y += 20.0f;
            }
        }

        public void DeleteKeyframe( CameraKeyframe keyframe )
        {
            CurrentPath.RemoveKeyframe( keyframe );
            if( CurrentKeyframe == keyframe )
            {
                DeselectKeyframe();
                SelectKeyframe( CurrentPath.GetKeyframe( 0 ) );
            }
        }

        private void InitializePivot()
        {
            _pathRootPosition = _cameraBeh.ActiveVessel.transform.position;
            _pathRootRotation = _cameraBeh.ActiveVessel.transform.rotation;
            _pathSpaceL2W = Matrix4x4.TRS( _pathRootPosition, _pathRootRotation, new Vector3( 1, 1, 1 ) );
            _pathSpaceW2L = _pathSpaceL2W.inverse;
        }

        /// <summary>
        /// Positions the camera at a keyframe.
        /// </summary>
        public void ViewKeyframe( CameraKeyframe keyframe )
        {
            Debug.Log( $"[CameraToolsKatnissified] Viewing Keyframe: Pos: {keyframe.Position}, Rot: {keyframe.Rotation}" );

            InitializePivot();

            this.Pivot.localPosition = _pathSpaceL2W.MultiplyPoint( keyframe.Position );
            this.Pivot.localRotation = _pathRootRotation * keyframe.Rotation;
            _cameraBeh.Zoom = keyframe.Zoom;
        }

        void SaveKeyframe( CameraKeyframe keyframe )
        {
            Vector3 localPosition = _pathSpaceW2L.MultiplyPoint( this.Pivot.localPosition );
            Quaternion localRotation = Quaternion.Inverse( _pathRootRotation ) * this.Pivot.localRotation;

            CurrentPath.SetTransform( keyframe, localPosition, localRotation, _cameraBeh.Zoom, CurrentKeyframe.Time );
        }

        void TogglePathList()
        {
            _pathKeyframeWindowVisible = false;
            _pathWindowVisible = !_pathWindowVisible;
        }

        void OnGUI()
        {
            if( CameraToolsManager._guiWindowVisible && CameraToolsManager._uiVisible )
            {
                _windowRect = GUI.Window( 322, _windowRect, DrawGuiWindow, "" );

                if( _pathKeyframeWindowVisible )
                {
                    DrawKeyframeEditorWindow();
                }
                if( _pathWindowVisible )
                {
                    DrawPathSelectorWindow( AvailablePaths, DeletePath, SelectPath, _windowRect, ref _pathSelectScrollPos );
                }
            }
        }

        void FixedUpdate()
        {
            //mouse panning, moving
            Vector3 forwardLevelAxis = this.Pivot.transform.forward;

            if( Input.GetKey( KeyCode.Mouse1 ) && Input.GetKey( KeyCode.Mouse2 ) )
            {
                this.Pivot.rotation = Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * -1.7f, this.Pivot.forward ) * this.Pivot.rotation;
            }
            else
            {
                if( Input.GetKey( KeyCode.Mouse1 ) )
                {
                    this.Pivot.rotation *= Quaternion.AngleAxis( Input.GetAxis( "Mouse X" ) * 1.7f / (_cameraBeh.Zoom * _cameraBeh.Zoom), Vector3.up );
                    this.Pivot.rotation *= Quaternion.AngleAxis( -Input.GetAxis( "Mouse Y" ) * 1.7f / (_cameraBeh.Zoom * _cameraBeh.Zoom), Vector3.right );
                    this.Pivot.rotation = Quaternion.LookRotation( this.Pivot.forward, this.Pivot.transform.up );
                }
                if( Input.GetKey( KeyCode.Mouse2 ) )
                {
                    this.Pivot.position += this.Pivot.right * Input.GetAxis( "Mouse X" ) * 2;
                    this.Pivot.position += forwardLevelAxis * Input.GetAxis( "Mouse Y" ) * 2;
                }
            }
            this.Pivot.position += this.Pivot.up * 10 * Input.GetAxis( "Mouse ScrollWheel" );

#warning TODO - move duplicated code.
            float fov = 60 / (Mathf.Exp( _cameraBeh.Zoom ) / Mathf.Exp( 1 ));
            if( _cameraBeh.FlightCamera.FieldOfView != fov )
            {
                _cameraBeh.FlightCamera.SetFoV( fov );
            }
        }

        public void DrawGuiWindow( int windowId )
        {
            UILayout UILayout = new UILayout();

            GUI.DragWindow( UILayout.SetWindow( 12, 2, 12, 20 ) );

            int line = 3; // Used to calculate the position of the next line of the GUI.

            if( CurrentPath != null )
            {
                GUI.Label( UILayout.GetRectX( line, 0, 1 ), "Path:" );
                CurrentPath.PathName = GUI.TextField( UILayout.GetRectX( line, 3, 11 ), CurrentPath.PathName );
            }
            else
            {
                GUI.Label( UILayout.GetRectX( line ), "Path: None" );
            }
            line++;

            if( GUI.Button( UILayout.GetRectX( line ), "Open Path" ) )
            {
                LoadPaths();
                TogglePathList();
            }
            line++;

            if( GUI.Button( UILayout.GetRectX( line, 0, 5 ), "New Path" ) )
            {
                CreateNewPath();
            }
            if( GUI.Button( UILayout.GetRectX( line, 6, 11 ), "Delete Path" ) )
            {
                DeletePath( CurrentPath );
            }
            line++;

            if( CurrentPath != null )
            {
                GUI.Label( UILayout.GetRectX( line ), "Interpolation Rate:" + CurrentPath.LerpRate.ToString( "0.0" ) );
                line++;
                CurrentPath.LerpRate = GUI.HorizontalSlider( UILayout.GetRectX( line ), CurrentPath.LerpRate, 1f, 15f );
                CurrentPath.LerpRate = Mathf.Round( CurrentPath.LerpRate * 10 ) / 10;
                line++;

                GUI.Label( UILayout.GetRectX( line ), "Path Timescale:" + CurrentPath.TimeScale.ToString( "0.00" ) );
                line++;
                CurrentPath.TimeScale = GUI.HorizontalSlider( UILayout.GetRectX( line ), CurrentPath.TimeScale, 0.05f, 4f );
                CurrentPath.TimeScale = Mathf.Round( CurrentPath.TimeScale * 20 ) / 20;
                line++;

                GUI.Label( UILayout.GetRectX( line ), "Path Frame:" + CurrentPath.Frame.ToString() );
                line++;
                if( GUI.Button( UILayout.GetRect( 1, line ), "<" ) )
                {
                    CurrentPath.Frame = Utils.CycleEnum( CurrentPath.Frame, -1 );
                }
                if( GUI.Button( UILayout.GetRect( 2, line ), ">" ) )
                {
                    CurrentPath.Frame = Utils.CycleEnum( CurrentPath.Frame, 1 );
                }
                line++;


                float viewHeight = Mathf.Max( 6 * 20, CurrentPath.keyframeCount * 20 );
                Rect scrollRect = UILayout.GetRect( 0, line, 11, line + 6 );
                line += 6;
                GUI.Box( scrollRect, "" );

                float viewcontentWidth = 240 - (2 * 20);
                _pathScrollPosition = GUI.BeginScrollView( scrollRect, _pathScrollPosition, new Rect( 0, 0, viewcontentWidth, viewHeight ), false, true );

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

                        string keyframeText = "#" + i.ToString() + ": " + CurrentPath.GetKeyframe( i ).Time.ToString( "0.00" ) + "s";

                        if( GUI.Button( new Rect( 0, (i * 20), viewcontentWidth * 0.75f, 20 ), keyframeText ) )
                        {
                            SelectKeyframe( CurrentPath.GetKeyframe( i ) );
                        }
                        if( GUI.Button( new Rect( (viewcontentWidth * 0.75f), (i * 20), (viewcontentWidth * 0.2f), 20 ), "X" ) )
                        {
                            DeleteKeyframe( CurrentPath.GetKeyframe( i ) );
                            break;
                        }
                    }
                    GUI.color = origGuiColor;
                }

                GUI.EndScrollView();
                line++;

                if( GUI.Button( UILayout.GetRectX( line ), "New Key" ) )
                {
                    CreateNewKeyframe();
                }
                line++;

                if( GUI.Button( UILayout.GetRectX( line, 0, 5 ), "Save All Paths" ) )
                {
                    SavePaths();
                }

                if( GUI.Button( UILayout.GetRectX( line, 6, 11 ), "Reload All Paths" ) )
                {
                    LoadPaths();
                }
            }

            float width;
            (width, _windowHeight) = UILayout.GetFullPixelSize();// CONTENT_TOP + (line * ENTRY_HEIGHT) + ENTRY_HEIGHT + ENTRY_HEIGHT;
            _windowRect.width = width;
            _windowRect.height = _windowHeight;
            _windowRect.height = 800;
        }


        public static void DrawPathSelectorWindow( List<CameraPath> availablePaths, Action<CameraPath> onDeletePath, Action<CameraPath> onPathSelected, Rect windowRect, ref Vector2 pathSelectScrollPos )
        {
            float width = 300;
            float height = 300;
            float indent = 5;
            float scrollRectSize = width - indent - indent;

            Rect pSelectRect = new Rect( windowRect.x - width, windowRect.y + 290, width, height );
            GUI.Box( pSelectRect, string.Empty );

            GUI.BeginGroup( pSelectRect );

            Rect scrollRect = new Rect( indent, indent, scrollRectSize, scrollRectSize );
            float scrollHeight = Mathf.Max( scrollRectSize, 20 * availablePaths.Count );
            Rect scrollViewRect = new Rect( 0, 0, scrollRectSize - 20, scrollHeight );
            pathSelectScrollPos = GUI.BeginScrollView( scrollRect, pathSelectScrollPos, scrollViewRect );

            CameraPath selectedPath = null;

            for( int i = 0; i < availablePaths.Count; i++ )
            {
                if( GUI.Button( new Rect( 0, i * 20, scrollRectSize - 90, 20 ), availablePaths[i].PathName ) )
                {
                    selectedPath = availablePaths[i];
                }
                if( GUI.Button( new Rect( scrollRectSize - 80, i * 20, 60, 20 ), "Delete" ) )
                {
                    onDeletePath( availablePaths[i] );
                    break;
                }
            }

            GUI.EndScrollView();

            GUI.EndGroup();
            if( selectedPath != null )
            {
                onPathSelected( selectedPath );
                //_pathWindowVisible = false;
            }
        }

        public void DrawKeyframeEditorWindow()
        {
            float width = 300;
            float height = 250;

            Rect kWindowRect = new Rect( _windowRect.x - width, _windowRect.y + 365, width, height );
            GUI.Box( kWindowRect, string.Empty );

            GUI.BeginGroup( kWindowRect );

            GUI.Label( new Rect( 5, 5, 100, 25 ), $"Keyframe t={CurrentKeyframe.Time}" );

            if( GUI.Button( new Rect( 105, 5, 180, 25 ), "Revert Pos" ) )
            {
                ViewKeyframe( CurrentKeyframe );
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
                Debug.Log( $"[CameraToolsKatnissified] Applying keyframe at time: {CurrentKeyframe.Time}" );
                SaveKeyframe( CurrentKeyframe );
                isApplied = true;
            }

            if( GUI.Button( new Rect( 100, 105, 195, 20 ), "Cancel" ) )
            {
                isApplied = true;
            }

            GUI.Label( new Rect( 100, 135, 195, 20 ), "Zoom" );

            _cameraBeh.Zoom = GUI.HorizontalSlider( new Rect( 100, 165, 195, 20 ), _cameraBeh.Zoom, 1.0f, 8.0f );
            GUI.Label( new Rect( 100, 195, 195, 20 ), (Mathf.Exp( _cameraBeh.Zoom ) / Mathf.Exp( 1 )).ToString( "0.0" ) + "x" );

            GUI.EndGroup();

            if( isApplied )
            {
                DeselectKeyframe();
            }
        }
    }
}
