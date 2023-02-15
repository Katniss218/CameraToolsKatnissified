using CameraToolsKatnissified.Cameras;
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
            }
        }

        public const float CONTENT_WIDTH = WINDOW_WIDTH - (2 * GUI_MARGIN);
        public const float CONTENT_TOP = 20;

        /// <summary>
        /// Controls how the camera Tools GUI window looks.
        /// </summary>
        void DrawGuiWindow( int windowId )
        {
            GUI.DragWindow( new Rect( 0, 0, WINDOW_WIDTH, _draggableHeight ) );

            float line = 1; // Used to calculate the position of the next line of the GUI.

            GUI.Label( new Rect( 0, CONTENT_TOP, WINDOW_WIDTH, 40 ), "Camera Tools", TitleStyle );
            line++;

            //tool mode switcher
            GUI.Label( new Rect( GUI_MARGIN, CONTENT_TOP + (line * ENTRY_HEIGHT), CONTENT_WIDTH, ENTRY_HEIGHT ), $"Tool: {_behaviours[0].GetType().Name}", HeaderStyle );
            line++;
            if( !CameraToolsActive )
            {
                if( GUI.Button( new Rect( GUI_MARGIN, CONTENT_TOP + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), "<" ) )
                {
                    CycleToolMode( 0, -1 );
                }
                if( GUI.Button( new Rect( GUI_MARGIN + 25 + 4, CONTENT_TOP + (line * ENTRY_HEIGHT), 25, ENTRY_HEIGHT - 2 ), ">" ) )
                {
                    CycleToolMode( 0, 1 );
                }
            }

            line++;
            line++;
            if( UseAutoZoom )
            {
                GUI.Label( new Rect( GUI_MARGIN, CONTENT_TOP + (line * ENTRY_HEIGHT), CONTENT_WIDTH / 2, ENTRY_HEIGHT ), "Autozoom Margin:" );
                line++;
                AutoZoomMargin = GUI.HorizontalSlider( new Rect( GUI_MARGIN, CONTENT_TOP + ((line) * ENTRY_HEIGHT), CONTENT_WIDTH - 45, ENTRY_HEIGHT ), AutoZoomMargin, 0.0f, 50.0f );
                GUI.Label( new Rect( GUI_MARGIN + CONTENT_WIDTH - 40, CONTENT_TOP + ((line - 0.15f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), AutoZoomMargin.ToString( "0.0" ) );
            }
            else
            {
                GUI.Label( new Rect( GUI_MARGIN, CONTENT_TOP + (line * ENTRY_HEIGHT), CONTENT_WIDTH, ENTRY_HEIGHT ), "Zoom:" );
                line++;
                Zoom = GUI.HorizontalSlider( new Rect( GUI_MARGIN, CONTENT_TOP + ((line) * ENTRY_HEIGHT), CONTENT_WIDTH - 45, ENTRY_HEIGHT ), Zoom, 1.0f, 8.0f );
                GUI.Label( new Rect( GUI_MARGIN + CONTENT_WIDTH - 40, CONTENT_TOP + ((line - 0.15f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), ZoomFactor.ToString( "0.0" ) + "x" );
            }
            line++;

            if( !(_behaviours[0] is PathCameraBehaviour) )
            {
                UseAutoZoom = GUI.Toggle( new Rect( GUI_MARGIN, CONTENT_TOP + (line * ENTRY_HEIGHT), CONTENT_WIDTH, ENTRY_HEIGHT ), UseAutoZoom, "Auto Zoom" );
                line++;
            }
            line++;

            GUI.Label( new Rect( GUI_MARGIN, CONTENT_TOP + (line * ENTRY_HEIGHT), CONTENT_WIDTH, ENTRY_HEIGHT ), "Camera Shake:" );
            line++;
            ShakeMultiplier = GUI.HorizontalSlider( new Rect( GUI_MARGIN, CONTENT_TOP + (line * ENTRY_HEIGHT), CONTENT_WIDTH - 45, ENTRY_HEIGHT ), ShakeMultiplier, 0.0f, 1.0f );
            GUI.Label( new Rect( GUI_MARGIN + CONTENT_WIDTH - 40, CONTENT_TOP + ((line - 0.25f) * ENTRY_HEIGHT), 40, ENTRY_HEIGHT ), ShakeMultiplier.ToString( "0.00" ) + "x" );
            line++;

            line++;

            // Draw Camera GUI
            _behaviours[0].DrawGui( ref line );

            line++;
            line++;
            Rect saveRect = new Rect( GUI_MARGIN, CONTENT_TOP + (line * ENTRY_HEIGHT), CONTENT_WIDTH / 2, ENTRY_HEIGHT );
            if( GUI.Button( saveRect, "Save" ) )
            {
                SaveAndSerialize();
            }

            Rect loadRect = new Rect( saveRect );
            loadRect.x += CONTENT_WIDTH / 2;
            if( GUI.Button( loadRect, "Reload" ) )
            {
                LoadAndDeserialize();
            }

            // fix length
            _windowHeight = CONTENT_TOP + (line * ENTRY_HEIGHT) + ENTRY_HEIGHT + ENTRY_HEIGHT;
            _windowRect.height = _windowHeight;
        }
    }
}