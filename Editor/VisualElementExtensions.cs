using UnityEngine.UIElements;

namespace AmeWorks.ChannelPacker.Editor
{
    public static class VisualElementExtensions
    {
        public static void SetDisplayOption(this VisualElement self, ElementDisplayOption displayOption)
        {
            switch (displayOption)
            {
                case ElementDisplayOption.Visible:
                    self.style.display = DisplayStyle.Flex;
                    self.visible = true;
                    break;
                case ElementDisplayOption.Hidden:
                    self.style.display = DisplayStyle.Flex;
                    self.visible = false;
                    break;
                case ElementDisplayOption.Collapsed:
                    self.style.display = DisplayStyle.None;
                    self.visible = false;
                    break;
            }
        }
    }
}