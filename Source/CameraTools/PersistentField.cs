using System;
using UnityEngine;

namespace CameraToolsKatnissified
{
    /// <summary>
    /// A marker attribute to persist the value of a field.
    /// </summary>
    [AttributeUsage( AttributeTargets.Field )]
    public class PersistentField : Attribute
    {
        public PersistentField()
        { }
    }
}