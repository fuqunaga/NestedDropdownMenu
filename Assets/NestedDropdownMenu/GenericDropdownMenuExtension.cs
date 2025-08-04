using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem
{
    public static class GenericDropdownMenuExtension
    {
        private static MethodInfo _pointerUpMethodInfo;

        public static VisualElement GetMenuContainer(this GenericDropdownMenu menu)
        {
            return menu == null
                ? null
                : GetFirstAncestorByClassName(menu.contentContainer, GenericDropdownMenu.ussClassName);
        }

        public static VisualElement GetOuterContainer(this GenericDropdownMenu menu)
        {
            return menu == null
                ? null
                : GetFirstAncestorByClassName(menu.contentContainer, GenericDropdownMenu.containerOuterUssClassName);
        }

        public static VisualElement GetScrollView(this GenericDropdownMenu menu)
        {
            return menu == null
                ? null
                : GetFirstAncestorByClassName(menu.contentContainer, GenericDropdownMenu.containerInnerUssClassName);
        }

        public static void OnPointerUp(this GenericDropdownMenu menu, PointerUpEvent evt)
        {
            if (menu == null) return;

            if (_pointerUpMethodInfo == null)
            {
                _pointerUpMethodInfo =
                    typeof(GenericDropdownMenu).GetMethod("OnPointerUp",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(_pointerUpMethodInfo, "OnPointerUp method not found in GenericDropdownMenu");
            }

            _pointerUpMethodInfo.Invoke(menu, new object[] { evt });
        }

        // public static void Hide(this GenericDropdownMenu menu, bool giveFocusBack = false)
        // {
        //     if (menu == null) return;
        //
        //     var hideMethod =
        //         typeof(GenericDropdownMenu).GetMethod("Hide", BindingFlags.NonPublic | BindingFlags.Instance);
        //     Assert.IsNotNull(hideMethod, "Hide method not found in GenericDropdownMenu");
        //
        //     hideMethod.Invoke(menu, new object[] { giveFocusBack });
        // }

        public static EventCallback<FocusOutEvent> GetOnFocusOutDelegate(this GenericDropdownMenu menu)
        {
            if (menu == null) return null;

            var methodInfo =
                typeof(GenericDropdownMenu).GetMethod("OnFocusOut", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(methodInfo, "OnFocusOut method not found in GenericDropdownMenu");

            return (EventCallback<FocusOutEvent>)methodInfo.CreateDelegate(typeof(EventCallback<FocusOutEvent>), menu);
        }

        public static VisualElement GetFirstAncestorByClassName(this VisualElement element, string className)
        {
            return GetFirstAncestor(element, e => e.ClassListContains(className));
        }

        private static VisualElement GetFirstAncestor(VisualElement element, Func<VisualElement, bool> predicate)
        {
            while (element != null)
            {
                if (predicate(element))
                {
                    return element;
                }

                element = element.parent;
            }

            return null;
        }
    }
}