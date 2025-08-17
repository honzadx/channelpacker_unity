inline int4 UnpackMask(int packedMask)
{
    return int4
    (
        (packedMask & 2) >> 1 & 1,
        (packedMask & 4) >> 2 & 1,
        (packedMask & 8) >> 3 & 1,
        (packedMask & 16) >> 4 & 1
    );
}

inline int PackMask(int4 unpackedMask)
{
    return (unpackedMask.x << 1) + (unpackedMask.y << 2) + (unpackedMask.z << 3) + (unpackedMask.w << 4);
}