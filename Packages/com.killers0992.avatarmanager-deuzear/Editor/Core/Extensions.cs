using UnityEngine;
using UnityEngine.UIElements;

namespace AvatarManager.Core
{
    public static class Extensions
    {
        public static void Hide(this VisualElement element)
        {
            if (element == null) return;

            element.style.display = DisplayStyle.None;
        }

        public static void Show(this VisualElement element)
        {
            if (element == null) return;

            element.style.display = DisplayStyle.Flex;
        }

        public static void SetBorderColor(this Button button, Color color)
        {
            if (button == null) return;

            button.style.borderRightColor = color;
            button.style.borderLeftColor = color;
            button.style.borderTopColor = color;
            button.style.borderBottomColor = color;
        }

        public static void SetBorderColor(this VisualElement element, Color color)
        {
            if (element == null) return;

            element.style.borderRightColor = color;
            element.style.borderLeftColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
        }

        public static void RedirectToUrl(this Button button, string url)
        {
            button.clickable = null;
            button.clicked += () =>
            {
                Application.OpenURL(url);
            };
        }
    }
}