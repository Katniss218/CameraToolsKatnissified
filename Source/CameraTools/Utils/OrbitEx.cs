using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraToolsKatnissified.Utils
{
    public static class OrbitEx
    {
        public static Vector3d Prograde( this Orbit orbit, double UT )
        {
            return orbit.getOrbitalVelocityAtUT( UT ).xzy.normalized;
        }

        public static Vector3d Radial( this Orbit orbit, double UT )
        {
            return Vector3d.Exclude( orbit.Prograde( UT ), orbit.Up( UT ) ).normalized;
        }

        public static Vector3d Normal( this Orbit orbit, double UT )
        {
            return -orbit.GetOrbitNormal().xzy.normalized;
        }

        public static Vector3d Up( this Orbit orbit, double UT )
        {
            return orbit.getRelativePositionAtUT( UT ).xzy.normalized;
        }

        public static Vector3d Horizontal( this Orbit orbit, double UT )
        {
            return Vector3d.Exclude( orbit.Up( UT ), orbit.Prograde( UT ) ).normalized;
        }
    }
}
