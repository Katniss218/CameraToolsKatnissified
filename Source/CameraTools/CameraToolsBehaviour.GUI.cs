using CameraToolsKatnissified.Cameras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified
{
    public sealed partial class CameraToolsBehaviour
    {
        const float WINDOW_WIDTH = 250;
        const float GUI_MARGIN = 12;
        const float ENTRY_HEIGHT = 20;

        Rect _windowRect = new Rect( 0, 0, 0, 0 );
        float _windowHeight = 400;
        float _draggableHeight = 40;


        // GUI - the location of the path scroll view.
        Vector2 _pathScrollPosition;
        Vector2 _pathSelectScrollPos;

        /// <summary>
        /// Unity Message - Draws GUI
        /// </summary>
        void OnGUI()
        {
            if( _guiWindowVisible && _uiVisible )
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

            GUI.DragWindow( new Rect( 0, 0, WINDOW_WIDTH, _draggableHeight ) );

            float line = 1; // Used to calculate the position of the next line of the GUI.

            float contentWidth = WINDOW_WIDTH - (2 * GUI_MARGIN);
            float contentTop = 20;

            GUI.Label( new Rect( 0, contentTop, WINDOW_WIDTH, 40 ), "Camera Tools", titleStyle );
            line++;

            //tool mode switcher
            GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Tool: " + CurrentCameraMode.ToString(), labelLeftBoldStyle );
            line++;
            if( !CameraToolsActive )
            {
                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), "<" ) )
                {
                    CycleToolMode( -1 );
                }
                if( GUI.Button( new Rect( GUI_MARGIN + 25 + 4, contentTop + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), ">" ) )
                {
                    CycleToolMode( 1 );
                }
            }

            line++;
            line++;
            if( UseAutoZoom )
            {
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), "Autozoom Margin: " );
                line++;
                AutoZoomMargin = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + ((line) * ENTRY_HEIGHT), contentWidth - 45, ENTRY_HEIGHT ), AutoZoomMargin, 0.0f, 50.0f );
                GUI.Label( new Rect( GUI_MARGIN + contentWidth - 40, contentTop + ((line - 0.15f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), AutoZoomMargin.ToString( "0.0" ), labelLeftStyle );
            }
            else
            {
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Zoom:", labelLeftStyle );
                line++;
                Zoom = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + ((line) * ENTRY_HEIGHT), contentWidth - 45, ENTRY_HEIGHT ), Zoom, 1.0f, 8.0f );
                GUI.Label( new Rect( GUI_MARGIN + contentWidth - 40, contentTop + ((line - 0.15f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), ZoomFactor.ToString( "0.0" ) + "x", labelLeftStyle );
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
            ShakeMultiplier = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth - 45, ENTRY_HEIGHT ), ShakeMultiplier, 0.0f, 1.0f );
            GUI.Label( new Rect( GUI_MARGIN + contentWidth - 40, contentTop + ((line - 0.25f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), ShakeMultiplier.ToString( "0.00" ) + "x" );
            line++;

            line++;

            // Draw Stationary Camera GUI

            if( CurrentCameraMode == CameraMode.StationaryCamera )
            {
                StationaryCameraBehaviour sb = (StationaryCameraBehaviour)_behaviour;

                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Frame of Reference: " + CurrentReferenceMode.ToString(), labelLeftStyle );
                line++;
                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), "<" ) )
                {
                    CycleReferenceMode( -1 );
                }
                if( GUI.Button( new Rect( GUI_MARGIN + 25 + 4, contentTop + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), ">" ) )
                {
                    CycleReferenceMode( 1 );
                }

                line++;

                if( CurrentReferenceMode == CameraReference.Surface || CurrentReferenceMode == CameraReference.Orbit )
                {
                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), "Max Rel. V: ", labelLeftStyle );
                    MaxRelativeVelocity = float.Parse( GUI.TextField( new Rect( GUI_MARGIN + contentWidth / 2, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), MaxRelativeVelocity.ToString() ) );
                }
                else if( CurrentReferenceMode == CameraReference.InitialVelocity )
                {
                    UseOrbitalInitialVelocity = GUI.Toggle( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), UseOrbitalInitialVelocity, " Orbital" );
                }
                line++;
                line++;

                // Draw position buttons.

#warning SB is null here?
                string positionButtonText = HasPosition ? StationaryCameraPosition.Value.ToString() : "None";
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Camera Position: " + positionButtonText, labelLeftStyle );
                line++;

                positionButtonText = _settingPositionEnabled ? "waiting..." : "Set Position";
                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT - 2 ), positionButtonText ) )
                {
                    _settingPositionEnabled = true;
                    _wasMouseUp = false;
                }
                if( GUI.Button( new Rect( 2 + GUI_MARGIN + contentWidth / 2, contentTop + (line * ENTRY_HEIGHT), (contentWidth / 2) - 2, ENTRY_HEIGHT - 2 ), "Clear Position" ) )
                {
                    StationaryCameraPosition = null;
                }
                line++;
                line++;

                // Draw target buttons.

                string targetButtonText = HasTarget ? StationaryCameraTarget.gameObject.name : "None";
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Camera Target: " + targetButtonText, labelLeftStyle );
                line++;

                targetButtonText = _settingTargetEnabled ? "waiting..." : "Set Target";
                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT - 2 ), targetButtonText ) )
                {
                    _settingTargetEnabled = true;
                    _wasMouseUp = false;
                }
                if( GUI.Button( new Rect( 2 + GUI_MARGIN + contentWidth / 2, contentTop + (line * ENTRY_HEIGHT), (contentWidth / 2) - 2, ENTRY_HEIGHT - 2 ), "Clear Target" ) )
                {
                    StationaryCameraTarget = null;
                }
            }

            // Draw pathing camera GUI.

            else if( CurrentCameraMode == CameraMode.Pathing )
            {
                PathCameraBehaviour pb = (PathCameraBehaviour)_behaviour;

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
                    _pathScrollPosition = GUI.BeginScrollView( scrollRect, _pathScrollPosition, new Rect( 0, 0, viewContentWidth, viewHeight ) );

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

        void DrawKeyframeEditorWindow()
        {
            float width = 300;
            float height = 130;

            Rect kWindowRect = new Rect( _windowRect.x - width, _windowRect.y + 365, width, height );
            GUI.Box( kWindowRect, string.Empty );

            GUI.BeginGroup( kWindowRect );

            GUI.Label( new Rect( 5, 5, 100, 25 ), $"Keyframe {_currentKeyframeIndex}" );

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

            bool isApplied = false;

            if( GUI.Button( new Rect( 100, 65, 195, 25 ), "Apply" ) )
            {
                Debug.Log( $"Applying keyframe at time: {_currentKeyframeTime}" );
                CurrentCameraPath.SetTransform( _currentKeyframeIndex, FlightCamera.transform, Zoom, _currentKeyframeTime );
                isApplied = true;
            }

            if( GUI.Button( new Rect( 100, 105, 195, 20 ), "Cancel" ) )
            {
                isApplied = true;
            }

            GUI.EndGroup();

            if( isApplied )
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

            bool isAnyPathSelected = false;

            for( int i = 0; i < _availableCameraPaths.Count; i++ )
            {
                if( GUI.Button( new Rect( 0, i * ENTRY_HEIGHT, scrollRectSize - 90, ENTRY_HEIGHT ), _availableCameraPaths[i].pathName ) )
                {
                    SelectPath( i );
                    isAnyPathSelected = true;
                }
                if( GUI.Button( new Rect( scrollRectSize - 80, i * ENTRY_HEIGHT, 60, ENTRY_HEIGHT ), "Delete" ) )
                {
                    DeletePath( i );
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