using UnityEngine;
using UnityEngine.UIElements;

namespace AmeWorks.ChromaPacker.Editor
{
    internal static class VisualElementExtensions
    {
        public static void AddIfTrue(this VisualElement self, VisualElement child, bool condition)
        {
            if (!condition)
                return;
            self.Add(child);
        }
        
        public static void SetBorderColor(this VisualElement self, Color color)
        {
            self.style.borderLeftColor = color;
            self.style.borderRightColor = color;
            self.style.borderTopColor = color;
            self.style.borderBottomColor = color;
        }

        public static void SetBorderWidth(this VisualElement self, float width)
        {
            self.SetBorderWidth(width, width, width, width);
        }
        
        public static void SetBorderWidth(this VisualElement self, float left, float right, float top, float bottom)
        {
            self.style.borderLeftWidth = left;
            self.style.borderRightWidth = right;
            self.style.borderTopWidth = top;
            self.style.borderBottomWidth = bottom;
        }
        
        public static void SetMargin(this VisualElement self, float width)
        {
            self.SetMargin(width, width, width, width);
        }
        
        public static void SetMargin(this VisualElement self, float left, float right, float top, float bottom)
        {
            self.style.marginLeft = left;
            self.style.marginRight = right;
            self.style.marginTop = top;
            self.style.marginBottom = bottom;
        }
        
        public static void SetPadding(this VisualElement self, float width)
        {
            self.SetPadding(width, width, width, width);
        }
        
        public static void SetPadding(this VisualElement self, float left, float right, float top, float bottom)
        {
            self.style.paddingLeft = left;
            self.style.paddingRight = right;
            self.style.paddingTop = top;
            self.style.paddingBottom = bottom;
        }
        
        public static void SetVisibility(this VisualElement self, ElementVisibility visibility)
        {
            switch (visibility)
            {
                case ElementVisibility.Visible:
                    self.style.display = DisplayStyle.Flex;
                    self.visible = true;
                    break;
                case ElementVisibility.Hidden:
                    self.style.display = DisplayStyle.Flex;
                    self.visible = false;
                    break;
                case ElementVisibility.Collapsed:
                    self.style.display = DisplayStyle.None;
                    self.visible = false;
                    break;
            }
        }
    }

    internal static class ColorExtensions
    {
        public static Color NewGrayscale(float value)
        {
            return new Color(value, value, value);
        }
    }
}