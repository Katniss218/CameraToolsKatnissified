using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified
{
    public static class Utils
    {
        public static Part GetPartFromMouse()
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

        public static Vector3? GetPosFromMouse()
        {
            Vector3 mouseAim = new Vector3( Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height, 0 );
            Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay( mouseAim );

            const int layerMask = 0b10001000000000000001;

            if( Physics.Raycast( ray, out RaycastHit hit, 15000.0f, layerMask ) )
            {
                return hit.point - (10 * ray.direction);
            }

            return null;
        }
    }
}