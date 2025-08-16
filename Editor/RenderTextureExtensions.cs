using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AmeWorks.ChannelPacker.Editor
{
    public static class RenderTextureExtensions
    {
        public static bool TryExportToPNG(this RenderTexture self, Vector2Int size, string path)
        {
            if (path.Length == 0 || size.x <= 0 || size.y <= 0)
                return false;

            Texture2D resultTexture = new Texture2D(size.x, size.y, TextureFormat.ARGB32, false);
            var previousActiveRT = RenderTexture.active;
            try
            {
                RenderTexture.active = self;
                resultTexture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                resultTexture.Apply();
                byte[] bytes = resultTexture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                RenderTexture.active = previousActiveRT;
                Object.DestroyImmediate(resultTexture);
            }
            return true;
        }
    }
}