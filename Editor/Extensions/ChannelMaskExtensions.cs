using UnityEngine;

namespace AmeWorks.ChromaPacker.Editor
{
    public static class ChannelMaskExtensions
    {
        public static Vector4 ToVector4(this ChannelMask self)
        {
            return new Vector4(
                ((int)self & (int)ChannelMask.R) > 1 ? 1.0f : 0.0f,
                ((int)self & (int)ChannelMask.G) > 1 ? 1.0f : 0.0f,
                ((int)self & (int)ChannelMask.B) > 1 ? 1.0f : 0.0f,
                ((int)self & (int)ChannelMask.A) > 1 ? 1.0f : 0.0f);
        }
    }
}