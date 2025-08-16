using System.Runtime.InteropServices;

namespace AmeWorks.ChannelPacker.Editor
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelData
    {
        public int mask;
        public int width;
        public int height;
        public int samplingType;
        public int invertValue;
        public float scaler;
        public float min;
        public float max;
        public float defaultValue;
    }
}