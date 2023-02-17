using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.UI
{
    public static class UILayout
    {
        public static float CellSize { get; set; } = 20.0f;
        /// <summary>
        /// The width of the grid in grid cells (excluding margins).
        /// </summary>
        public static int Width { get; set; } = 5;
        /// <summary>
        /// The height of the grid in grid cells (excluding margins).
        /// </summary>
        public static int Height { get; set; } = 5;
        public static float Margin { get; set; } = 12.0f;

        private static int overwrittenHeight = 5;

        public static (int width, int height) GetOverwrittenDimensions()
        {
            return (Width, overwrittenHeight);
        }

        public static (float width, float height) GetFullPixelSize()
        {
#warning TODO - seems to be 1 grid cell to narrow on the right?
            return ((Width * CellSize) + (2 * Margin) + CellSize, (overwrittenHeight * CellSize) + (2 * Margin) + CellSize); // +CellSize is to include last cell.
        }

        public static Rect SetWindow( int width, int height, float margins = 12.0f, float cellSize = 20.0f )
        {
            Margin = margins;
            CellSize = cellSize;
            Width = width;
            Height = height;
            overwrittenHeight = height;
            return new Rect( 0, 0, Width * CellSize + 2 * margins, Height * CellSize + 2 * margins ); // margin, grid, margin
        }

        public static Rect GetRect( int minX, int minY, int maxX, int maxY )
        {
            if( maxY > overwrittenHeight )
            {
                overwrittenHeight = maxY;
            }
            return new Rect( minX * CellSize + Margin, minY * CellSize + Margin, (maxX - minX) * CellSize, (maxY - minY) * CellSize );
        }

        public static Rect GetRect( int x, int y )
        {
            if( y > overwrittenHeight )
            {
                overwrittenHeight = y;
            }
            return new Rect( x * CellSize + Margin, y * CellSize + Margin, CellSize, CellSize );
        }

        /// <summary>
        /// Rect horizontal.
        /// </summary>
        public static Rect GetRectX( int y, int minX, int maxX )
        {
            if( y > overwrittenHeight )
            {
                overwrittenHeight = y;
            }
            return new Rect( minX * CellSize + Margin, y * CellSize + Margin, (maxX - minX) * CellSize, CellSize );
        }

        /// <summary>
        /// Rect horizontal.
        /// </summary>
        public static Rect GetRectX( int y )
        {
            if( y > overwrittenHeight )
            {
                overwrittenHeight = y;
            }
            return new Rect( Margin, y * CellSize + Margin, Width * CellSize, CellSize );
        }

        /// <summary>
        /// Rect vertical.
        /// </summary>
        public static Rect GetRectY( int x )
        {
            return new Rect( x * CellSize + Margin, Margin, CellSize, Width * CellSize );
        }
    }
}
