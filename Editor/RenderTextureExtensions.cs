using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AmeWorks.ChannelPacker.Editor
{
    public static class RenderTextureExtensions
    {
        public static bool TryExportToPNG(
            this RenderTexture renderTexture, 
            Vector2Int size, 
            string directory,
            string fileName)
        {
            if (!Directory.Exists(directory) || string.IsNullOrEmpty(fileName))
                return false;
            
            if (size.x <= 0 || size.y <= 0)
                return false;

            Texture2D resultTexture = new Texture2D(size.x, size.y, TextureFormat.ARGB32, false);
            try
            {
                var previousActiveRT = RenderTexture.active;
                RenderTexture.active = renderTexture;
                resultTexture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                resultTexture.Apply();
                byte[] bytes = resultTexture.EncodeToPNG();
                string path = Path.Combine(directory, $"{fileName}.png");
                File.WriteAllBytes(path, bytes);
                RenderTexture.active = previousActiveRT;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                Object.DestroyImmediate(resultTexture);
            }
            return true;
        }
    }
}