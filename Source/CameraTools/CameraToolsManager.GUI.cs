﻿using CameraToolsKatnissified.Cameras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified
{
    public sealed partial class CameraToolsManager
    {
        public const float WINDOW_WIDTH = 250;
        public const float GUI_MARGIN = 12;
        public const float ENTRY_HEIGHT = 20;

        public static readonly GUIStyle TitleStyle = new GUIStyle()
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            }
        };

        public static readonly GUIStyle HeaderStyle = new GUIStyle()
        {
            alignment = TextAnchor.UpperLeft,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            }
        };

        public Rect _windowRect = new Rect( 0, 0, 0, 0 );
        public float _windowHeight = 400;
        public float _draggableHeight = 40;


        // GUI - the location of the path scroll view.
        public Vector2 _pathScrollPosition;
        public Vector2 _pathSelectScrollPos;

        /// <summary>
        /// Unity Message - Draws GUI
        /// </summary>
        void OnGUI()
        {
            if( _guiWindowVisible && _uiVisible )
            {
                _windowRect = GUI.Window( 320, _windowRect, DrawGuiWindow, "" );

                if( PathKeyframeWindowVisible )
                {
                    ((PathCameraBehaviour)_behaviours[CameraMode.PathCamera]).DrawKeyframeEditorWindow();
                }
                if( PathWindowVisible )
                {
                    ((PathCameraBehaviour)_behaviours[CameraMode.PathCamera]).DrawPathSelectorWindow();
                }
            }
        }

        /// <summary>
        /// Controls how the camera Tools GUI window looks.
        /// </summary>
        void DrawGuiWindow( int windowId )
        {
            GUI.DragWindow( new Rect( 0, 0, WINDOW_WIDTH, _draggableHeight ) );

            float line = 1; // Used to calculate the position of the next line of the GUI.

            float contentWidth = WINDOW_WIDTH - (2 * GUI_MARGIN);
            float contentTop = 20;

            GUI.Label( new Rect( 0, contentTop, WINDOW_WIDTH, 40 ), "Camera Tools", TitleStyle );
            line++;

            //tool mode switcher
            GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), $"Tool: {CurrentCameraMode}", HeaderStyle );
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
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), "Autozoom Margin:" );
                line++;
                AutoZoomMargin = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + ((line) * ENTRY_HEIGHT), contentWidth - 45, ENTRY_HEIGHT ), AutoZoomMargin, 0.0f, 50.0f );
                GUI.Label( new Rect( GUI_MARGIN + contentWidth - 40, contentTop + ((line - 0.15f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), AutoZoomMargin.ToString( "0.0" ) );
            }
            else
            {
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Zoom:" );
                line++;
                Zoom = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + ((line) * ENTRY_HEIGHT), contentWidth - 45, ENTRY_HEIGHT ), Zoom, 1.0f, 8.0f );
                GUI.Label( new Rect( GUI_MARGIN + contentWidth - 40, contentTop + ((line - 0.15f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), ZoomFactor.ToString( "0.0" ) + "x" );
            }
            line++;

            if( CurrentCameraMode != CameraMode.PathCamera )
            {
                UseAutoZoom = GUI.Toggle( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), UseAutoZoom, "Auto Zoom" );
                line++;
            }
            line++;

            GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Camera Shake:" );
            line++;
            ShakeMultiplier = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth - 45, ENTRY_HEIGHT ), ShakeMultiplier, 0.0f, 1.0f );
            GUI.Label( new Rect( GUI_MARGIN + contentWidth - 40, contentTop + ((line - 0.25f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), ShakeMultiplier.ToString( "0.00" ) + "x" );
            line++;

            line++;

            // Draw Stationary Camera GUI

            if( CurrentCameraMode == CameraMode.StationaryCamera )
            {
                StationaryCameraBehaviour sb = (StationaryCameraBehaviour)_behaviours[CameraMode.StationaryCamera];

                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), $"Frame of Reference: {CurrentReferenceMode}" );
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
                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), "Max Rel. V:" );
                    MaxRelativeVelocity = float.Parse( GUI.TextField( new Rect( GUI_MARGIN + contentWidth / 2, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), MaxRelativeVelocity.ToString() ) );
                }
                else if( CurrentReferenceMode == CameraReference.InitialVelocity )
                {
                    UseOrbitalInitialVelocity = GUI.Toggle( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), UseOrbitalInitialVelocity, " Orbital" );
                }
                line++;
                line++;

                // Draw position buttons.

                string positionButtonText = sb.CameraPosition == null ? "None" : sb.CameraPosition.Value.ToString();
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Camera Position:" + positionButtonText );
                line++;

                positionButtonText = _settingPositionEnabled ? "waiting..." : "Set Position";
                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT - 2 ), positionButtonText ) )
                {
                    _settingPositionEnabled = true;
                    _wasMouseUp = false;
                }
                if( GUI.Button( new Rect( 2 + GUI_MARGIN + contentWidth / 2, contentTop + (line * ENTRY_HEIGHT), (contentWidth / 2) - 2, ENTRY_HEIGHT - 2 ), "Clear Position" ) )
                {
                    sb.CameraPosition = null;
                }
                line++;
                line++;

                // Draw target buttons.

                string targetButtonText = sb.Target == null ? "None" : sb.Target.gameObject.name;
                GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Camera Target:" + targetButtonText );
                line++;

                targetButtonText = _settingTargetEnabled ? "waiting..." : "Set Target";
                if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT - 2 ), targetButtonText ) )
                {
                    _settingTargetEnabled = true;
                    _wasMouseUp = false;
                }
                if( GUI.Button( new Rect( 2 + GUI_MARGIN + contentWidth / 2, contentTop + (line * ENTRY_HEIGHT), (contentWidth / 2) - 2, ENTRY_HEIGHT - 2 ), "Clear Target" ) )
                {
                    sb.Target = null;
                }
            }

            // Draw pathing camera GUI.

            else if( CurrentCameraMode == CameraMode.PathCamera )
            {
                PathCameraBehaviour sb = (PathCameraBehaviour)_behaviours[CameraMode.PathCamera];

                if( sb.CurrentPath != null )
                {
                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Path:" );
                    sb.CurrentPath.PathName = GUI.TextField( new Rect( GUI_MARGIN + 34, contentTop + (line * ENTRY_HEIGHT), contentWidth - 34, ENTRY_HEIGHT ), sb.CurrentPath.PathName );
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
                    sb.CreateNewPath();
                }
                if( GUI.Button( new Rect( GUI_MARGIN + (contentWidth / 2), contentTop + (line * ENTRY_HEIGHT), contentWidth / 2, ENTRY_HEIGHT ), "Delete Path" ) )
                {
                    sb.DeletePath( sb.CurrentPath );
                }
                line++;

                if( sb.CurrentPath != null )
                {
                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Interpolation Rate:" + sb.CurrentPath.LerpRate.ToString( "0.0" ) );
                    line++;
                    sb.CurrentPath.LerpRate = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT) + 4, contentWidth - 50, ENTRY_HEIGHT ), sb.CurrentPath.LerpRate, 1f, 15f );
                    sb.CurrentPath.LerpRate = Mathf.Round( sb.CurrentPath.LerpRate * 10 ) / 10;
                    line++;

                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Path Timescale:" + sb.CurrentPath.TimeScale.ToString( "0.00" ) );
                    line++;
                    sb.CurrentPath.TimeScale = GUI.HorizontalSlider( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT) + 4, contentWidth - 50, ENTRY_HEIGHT ), sb.CurrentPath.TimeScale, 0.05f, 4f );
                    sb.CurrentPath.TimeScale = Mathf.Round( sb.CurrentPath.TimeScale * 20 ) / 20;
                    line++;

                    GUI.Label( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, ENTRY_HEIGHT ), "Path Frame:" + sb.CurrentPath.Frame.ToString() );
                    line++;
                    if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), "<" ) )
                    {
                        sb.CurrentPath.Frame = Utils.CycleEnum( sb.CurrentPath.Frame, -1 );
                    }
                    if( GUI.Button( new Rect( GUI_MARGIN + 25 + 4, contentTop + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), ">" ) )
                    {
                        sb.CurrentPath.Frame = Utils.CycleEnum( sb.CurrentPath.Frame, 1 );
                    }
                    line++;

                    float viewHeight = Mathf.Max( 6 * ENTRY_HEIGHT, sb.CurrentPath.keyframeCount * ENTRY_HEIGHT );
                    Rect scrollRect = new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), contentWidth, 6 * ENTRY_HEIGHT );
                    GUI.Box( scrollRect, string.Empty );

                    float viewContentWidth = contentWidth - (2 * GUI_MARGIN);
                    _pathScrollPosition = GUI.BeginScrollView( scrollRect, _pathScrollPosition, new Rect( 0, 0, viewContentWidth, viewHeight ) );

                    // Draw path keyframe list.
                    if( sb.CurrentPath.keyframeCount > 0 )
                    {
                        Color origGuiColor = GUI.color;
                        for( int i = 0; i < sb.CurrentPath.keyframeCount; i++ )
                        {
                            if( sb.CurrentPath.GetKeyframe( i ) == sb.CurrentKeyframe )
                            {
                                GUI.color = Color.green;
                            }
                            else
                            {
                                GUI.color = origGuiColor;
                            }
                            string kLabel = "#" + i.ToString() + ": " + sb.CurrentPath.GetKeyframe( i ).Time.ToString( "0.00" ) + "s";
                            if( GUI.Button( new Rect( 0, (i * ENTRY_HEIGHT), 3 * viewContentWidth / 4, ENTRY_HEIGHT ), kLabel ) )
                            {
                                sb.SelectKeyframe( sb.CurrentPath.GetKeyframe( i ) );
                            }
                            if( GUI.Button( new Rect( (3 * contentWidth / 4), (i * ENTRY_HEIGHT), (viewContentWidth / 4) - 20, ENTRY_HEIGHT ), "X" ) )
                            {
                                sb.DeleteKeyframe( sb.CurrentPath.GetKeyframe( i ) );
                                break;
                            }
                        }
                        GUI.color = origGuiColor;
                    }

                    GUI.EndScrollView();
                    line += 6.5f;
                    if( GUI.Button( new Rect( GUI_MARGIN, contentTop + (line * ENTRY_HEIGHT), 3 * contentWidth / 4, ENTRY_HEIGHT ), "New Key" ) )
                    {
                        sb.CreateNewKeyframe();
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
    }
}