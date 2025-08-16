using System;

namespace AmeWorks.ChromaPacker.Editor
{
    [Flags]
    public enum ChannelMask
    {
        R = 1 << 1,
        G = 1 << 2,
        B = 1 << 3,
        A = 1 << 4,
    }
    
    public enum SamplingType
    {
        Once,
        Repeat,
    }
    
    public enum ElementVisibility
    {
        Collapsed,
        Hidden, 
        Visible
    }
}