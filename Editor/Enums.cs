using System;

namespace AmeWorks.ChromaPacker.Editor
{
    [Flags]
    internal enum ChannelMask
    {
        R = 1 << 1,
        G = 1 << 2,
        B = 1 << 3,
        A = 1 << 4,
    }
    
    internal enum SamplingType
    {
        Once,
        Repeat,
    }
    
    internal enum ElementVisibility
    {
        Collapsed,
        Hidden, 
        Visible
    }
}