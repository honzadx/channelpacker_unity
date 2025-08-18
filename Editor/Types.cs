using System.Runtime.InteropServices;
using UnityEngine;

namespace AmeWorks.ChromaPacker.Editor
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ChannelData
    {
        public Vector2Int size;
        public Vector2Int offset;
        public Vector2 clamp;
        public Vector2 clip;
        public int mask;
        public int samplingType;
        public int invert;
        public float multiply;
        public float defaultValue;
    }
}