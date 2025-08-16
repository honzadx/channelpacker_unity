using UnityEngine.UIElements;

namespace AmeWorks.ChannelPacker.Editor
{
    public static class VisualElementExtensions
    {
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
}