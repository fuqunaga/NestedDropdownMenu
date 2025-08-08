#nullable enable

using System;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem
{
    public static class VisualElementExtensions
    {
        public static VisualElement GetFirstAncestorByClassName(this VisualElement element, string className)
        {
            while (element != null)
            {
                if (element.ClassListContains(className))
                {
                    return element;
                }

                element = element.parent;
            }

            throw new InvalidOperationException($"Ancestor with class name '{className}' not found.");
        }

#if !UNITY_6000_OR_NEWER
        public static void RegisterCallbackOnce<TEventType>(
            this VisualElement element,
            EventCallback<TEventType> callback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEventType : EventBase<TEventType>, new()
        {
            element.RegisterCallback<TEventType>(evt =>
            {
                callback.Invoke(evt);
                element.UnregisterCallback(callback, useTrickleDown);
            }, useTrickleDown);
        }
#endif
    }
}