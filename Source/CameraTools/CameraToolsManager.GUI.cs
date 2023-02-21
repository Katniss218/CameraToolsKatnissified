﻿using CameraToolsKatnissified.CameraControllers;
using CameraToolsKatnissified.UI;
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
        // public const float WINDOW_WIDTH = 250;
        // public const float GUI_MARGIN = 12;
        // public const float ENTRY_HEIGHT = 20;

        public static readonly GUIStyle TitleStyle = new GUIStyle()
        {
            fontSize = 20,
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



        public Vector2 _behaviourScrollPos;

        /// <summary>
        /// Unity Message - Draws GUI
        /// </summary>
        void OnGUI()
        {
            if( _guiWindowVisible && _uiVisible )
            {
                _windowRect = GUI.Window( 320, _windowRect, DrawGuiWindow, "" );
            }
        }

        // public const float CONTENT_WIDTH = WINDOW_WIDTH - (2 * GUI_MARGIN);
        // public const float CONTENT_TOP = 20;

        /// <summary>
        /// Controls how the camera Tools GUI window looks.
        /// </summary>
        void DrawGuiWindow( int windowId )
        {
            UILayout UILayout = new UILayout();

            GUI.DragWindow( UILayout.SetWindow( 12, 2, 12, 20 ) );

            GUI.Label( UILayout.GetRect( 0, 0, 11, 1 ), "Camera Tools", TitleStyle );

            int line = 3; // Used to calculate the position of the next line of the GUI.

            if( CurrentController is CameraSetupController )
            {
                if( GUI.Button( UILayout.GetRectX( line ), "Exit Path Editor" ) )
                {
                    SetController<CameraPlayerController>();
                }
            }
            else if( CurrentController is CameraPlayerController cpc )
            {
#warning TODO - move this to the behaviour gui?.
                /*if( UseAutoZoom )
                {
                    GUI.Label( UILayout.GetRectX( line, 0, 2 ), "Autozoom" );
                    AutoZoomMargin = GUI.HorizontalSlider( UILayout.GetRectX( line, 3, 9 ), AutoZoomMargin, 1.0f, 75.0f );
                    GUI.Label( UILayout.GetRectX( line, 10, 11 ), AutoZoomMargin.ToString( "0.0" ) );
                }
                else
                {*/
                if( GUI.Button( UILayout.GetRectX( line ), "Path Editor" ) )
                {
                    //StartEditingPath();
                    SetController<CameraSetupController>();
                    CurrentController.StartPlaying();
                }
                line++;
                GUI.Label( UILayout.GetRectX( line, 0, 1 ), "Zoom" );
                cpc.Zoom = GUI.HorizontalSlider( UILayout.GetRectX( line, 2, 9 ), cpc.Zoom, 1.0f, 8.0f );
                GUI.Label( UILayout.GetRectX( line, 10, 11 ), (Mathf.Exp( cpc.Zoom ) / Mathf.Exp( 1 )).ToString( "0.0" ) + "x" );
                //}
                line++;

                //UseAutoZoom = GUI.Toggle( UILayout.GetRectX( line ), UseAutoZoom, "Auto Zoom" );
                //line++;
                line++;

                GUI.Label( UILayout.GetRectX( line ), "Camera Shake:" );
                line++;

                cpc.ShakeMultiplier = GUI.HorizontalSlider( UILayout.GetRectX( line, 0, 9 ), cpc.ShakeMultiplier, 0.0f, 1.0f );
                GUI.Label( UILayout.GetRectX( line, 10, 11 ), cpc.ShakeMultiplier.ToString( "0.00" ) + "x" );

                //Rect scrollRect = new Rect( GUI_MARGIN, CONTENT_TOP + (line * ENTRY_HEIGHT), CONTENT_WIDTH, 6 * ENTRY_HEIGHT );
                //GUI.Box( scrollRect, string.Empty );

                // float viewcontentWidth = CONTENT_WIDTH - (2 * GUI_MARGIN);
                //float viewHeight = Mathf.Max( 6 * ENTRY_HEIGHT, 10 * _behaviours.Count * ENTRY_HEIGHT ); // needs a method in the behaviours that returns the GUI height for each behaviour.
                //_pathScrollPosition = GUI.BeginScrollView( scrollRect, _behaviourScrollPos, new Rect( 0, 0, viewcontentWidth, viewHeight ) );

                for( int i = 0; i < cpc.Behaviours.Count; i++ )
                {
                    line++;
                    //tool mode switcher
                    GUI.Label( UILayout.GetRectX( line, 0, 9 ), $"Tool: {cpc.Behaviours[i].GetType().Name}", HeaderStyle );
                    if( !IsActive )
                    {
                        if( GUI.Button( UILayout.GetRect( 10, line ), "<" ) )
                        {
                            cpc.CycleToolMode( i, -1 );
                        }
                        if( GUI.Button( UILayout.GetRect( 11, line ), ">" ) )
                        {
                            cpc.CycleToolMode( i, 1 );
                        }
                    }

                    line++;
                    // Draw Camera GUI
                    cpc.Behaviours[i].DrawGui( UILayout, ref line );
                    line++;
                }

                //GUI.EndScrollView();

                line++;
                if( GUI.Button( UILayout.GetRect( 0, line ), "+" ) )
                {
                    cpc.Behaviours.Add( CameraBehaviour.GetBehaviour( CameraBehaviour.GetDefaultType(), cpc ) );
                }
                if( GUI.Button( UILayout.GetRect( 1, line ), "-" ) )
                {
                    cpc.Behaviours.RemoveAt( cpc.Behaviours.Count - 1 );
                }
                line++;
                line++;

                if( GUI.Button( UILayout.GetRectX( line, 0, 5 ), "Save" ) )
                {
                    cpc.Save();
                }

                if( GUI.Button( UILayout.GetRectX( line, 6, 11 ), "Reload" ) )
                {
                    cpc.Load();
                }
            }

            // fix length
            float width;
            (width, _windowHeight) = UILayout.GetFullPixelSize();// CONTENT_TOP + (line * ENTRY_HEIGHT) + ENTRY_HEIGHT + ENTRY_HEIGHT;
            _windowRect.width = width;
            _windowRect.height = _windowHeight;
        }
    }
}