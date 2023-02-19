using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified.UI
{
    public class UILayout
    {
        public float CellSize { get; set; } = 20.0f;
        /// <summary>
        /// The width of the grid in grid cells (excluding margins).
        /// </summary>
        public int Width { get; set; } = 5;
        /// <summary>
        /// The height of the grid in grid cells (excluding margins).
        /// </summary>
        public int Height { get; set; } = 5;
        public float Margin { get; set; } = 12.0f;

        private int overwrittenHeight = 5;

        public (int width, int height) GetOverwrittenDimensions()
        {
            return (Width, overwrittenHeight);
        }

        public (float width, float height) GetFullPixelSize()
        {
#warning TODO - seems to be 1 grid cell to narrow on the right?
            return ((Width * CellSize) + (2 * Margin), (overwrittenHeight * CellSize) + (2 * Margin)); // +CellSize is to include last cell.
        }

        public Rect SetWindow( int width, int height, float margins = 12.0f, float cellSize = 20.0f )
        {
            Margin = margins;
            CellSize = cellSize;
            Width = width;
            Height = height;
            overwrittenHeight = height;
            return new Rect( 0, 0, Width * CellSize + 2 * margins, Height * CellSize + 2 * margins ); // margin, grid, margin
        }

        /// <summary>
        /// Rect arbitrary, takes up the specified grid cells.
        /// </summary>
        public Rect GetRect( int minX, int minY, int maxX, int maxY )
        {
            if( maxY > overwrittenHeight )
            {
                overwrittenHeight = maxY;
            }
            // Without adding 1 to max, min = 1, max = 1 would result in 0-width.
            return new Rect( minX * CellSize + Margin, minY * CellSize + Margin, ((maxX + 1) - minX) * CellSize, ((maxY + 1) - minY) * CellSize );
        }

        /// <summary>
        /// Single cell
        /// </summary>
        public Rect GetRect( int x, int y )
        {
            if( y > overwrittenHeight )
            {
                overwrittenHeight = y;
            }
            return new Rect( x * CellSize + Margin, y * CellSize + Margin, CellSize, CellSize );
        }

        /// <summary>
        /// Rect horizontal, takes up the specified grid cells.
        /// </summary>
        public Rect GetRectX( int y, int minX, int maxX )
        {
            if( y > overwrittenHeight )
            {
                overwrittenHeight = y;
            }
            // Without adding 1 to max, min = 1, max = 1 would result in 0-width.
            return new Rect( minX * CellSize + Margin, y * CellSize + Margin, ((maxX + 1) - minX) * CellSize, CellSize );
        }

        /// <summary>
        /// Rect horizontal, takes up the entire grid width.
        /// </summary>
        public Rect GetRectX( int y )
        {
            if( y > overwrittenHeight )
            {
                overwrittenHeight = y;
            }
            return new Rect( Margin, y * CellSize + Margin, Width * CellSize, CellSize );
        }

        /// <summary>
        /// Rect vertical, takes up the entire grid height.
        /// </summary>
        public Rect GetRectY( int x )
        {
            return new Rect( x * CellSize + Margin, Margin, CellSize, Width * CellSize );
        }
    }
}
