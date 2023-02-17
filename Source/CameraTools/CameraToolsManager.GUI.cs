﻿using CameraToolsKatnissified.Cameras;
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


        // GUI - the location of the path scroll view.
        public Vector2 _pathScrollPosition;
        public Vector2 _pathSelectScrollPos;

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
            foreach( var beh in _behaviours )
            {
                beh.OnGUI();
            }
        }

        // public const float CONTENT_WIDTH = WINDOW_WIDTH - (2 * GUI_MARGIN);
        // public const float CONTENT_TOP = 20;

        /// <summary>
        /// Controls how the camera Tools GUI window looks.
        /// </summary>
        void DrawGuiWindow( int windowId )
        {
            GUI.DragWindow( UILayout.SetWindow( 12, 2, 12, 20 ) );

            GUI.Label( UILayout.GetRect( 0, 0, 11, 1 ), "Camera Tools", TitleStyle );

            int line = 2; // Used to calculate the position of the next line of the GUI.

            if( UseAutoZoom )
            {
                GUI.Label( UILayout.GetRectX( line ), "Autozoom Margin:" );
                line++;

                AutoZoomMargin = GUI.HorizontalSlider( UILayout.GetRectX( line, 0, 9 ), AutoZoomMargin, 1.0f, 75.0f );
                GUI.Label( UILayout.GetRectX( line, 9, 11 ), AutoZoomMargin.ToString( "0.0" ) );
            }
            else
            {
                GUI.Label( UILayout.GetRectX( line ), "Zoom:" );
                line++;

                Zoom = GUI.HorizontalSlider( UILayout.GetRectX( line, 0, 9 ), Zoom, 1.0f, 8.0f );
                GUI.Label( UILayout.GetRectX( line, 9, 11 ), ZoomFactor.ToString( "0.0" ) + "x" );
            }
            line++;

            UseAutoZoom = GUI.Toggle( UILayout.GetRectX( line ), UseAutoZoom, "Auto Zoom" );
            line++;
            line++;

            GUI.Label( UILayout.GetRectX( line ), "Camera Shake:" );
            line++;

            ShakeMultiplier = GUI.HorizontalSlider( UILayout.GetRectX( line, 0, 9 ), ShakeMultiplier, 0.0f, 1.0f );
            GUI.Label( UILayout.GetRectX( line, 9, 11 ), ShakeMultiplier.ToString( "0.00" ) + "x" );

            //Rect scrollRect = new Rect( GUI_MARGIN, CONTENT_TOP + (line * ENTRY_HEIGHT), CONTENT_WIDTH, 6 * ENTRY_HEIGHT );
            //GUI.Box( scrollRect, string.Empty );

            // float viewcontentWidth = CONTENT_WIDTH - (2 * GUI_MARGIN);
            //float viewHeight = Mathf.Max( 6 * ENTRY_HEIGHT, 10 * _behaviours.Count * ENTRY_HEIGHT ); // needs a method in the behaviours that returns the GUI height for each behaviour.
            //_pathScrollPosition = GUI.BeginScrollView( scrollRect, _behaviourScrollPos, new Rect( 0, 0, viewcontentWidth, viewHeight ) );

            for( int i = 0; i < _behaviours.Count; i++ )
            {
                line++;
                //tool mode switcher
                GUI.Label( UILayout.GetRectX( line ), $"Tool: {_behaviours[i].GetType().Name}", HeaderStyle );
                line++;
                if( !CameraToolsActive )
                {
                    if( GUI.Button( UILayout.GetRect( 0, line ), "<" ) )
                    {
                        CycleToolMode( i, -1 );
                    }
                    if( GUI.Button( UILayout.GetRect( 1, line ), ">" ) )
                    {
                        CycleToolMode( i, 1 );
                    }
                }

                line++;

                // Draw Camera GUI
                _behaviours[i].DrawGui( 12 * 20, ref line );
                line++;
            }

            //GUI.EndScrollView();

            line++;
            if( GUI.Button( UILayout.GetRect( 0, line ), "+" ) )
            {
                _behaviours.Add( new PathCameraBehaviour( this ) );
            }
            if( GUI.Button( UILayout.GetRect( 1, line ), "-" ) )
            {
                _behaviours.RemoveAt( _behaviours.Count - 1 );
            }
            line++;
            line++;

            if( GUI.Button( UILayout.GetRectX( line, 0, 6 ), "Save" ) )
            {
                SaveAndSerialize();
            }

            if( GUI.Button( UILayout.GetRectX( line, 6, 11 ), "Reload" ) )
            {
                LoadAndDeserialize();
            }

            // fix length
            (_, _windowHeight) = UILayout.GetFullPixelSize();// CONTENT_TOP + (line * ENTRY_HEIGHT) + ENTRY_HEIGHT + ENTRY_HEIGHT;
            _windowRect.height = _windowHeight;
        }
    }
}