using System.Runtime.InteropServices;
using UnityEngine;

namespace AmeWorks.ChromaPacker.Editor
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelData
    {
        public int mask;
        public int width;
        public int height;
        public int samplingType;
        public int invert;
        public float scaler;
        public Vector2 clamp;
        public Vector2 clip;
        public float defaultValue;
    }
}