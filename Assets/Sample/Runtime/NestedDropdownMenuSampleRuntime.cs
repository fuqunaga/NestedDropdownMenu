using UnityEngine;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem.Sample.Runtime
{
    public class NestedDropdownMenuSample : MonoBehaviour
    {
        private void Start() => CreateUI();

        private void CreateUI()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("UIDocument component not found on the GameObject.");
                return;
            }


            var sample = SampleMenu.CreateElement();
#if UNITY_6000_1_OR_NEWER
            sample.style.backgroundColor = Color.darkGray;
#else
            sample.style.backgroundColor = new Color(0.6627451f, 0.6627451f, 0.6627451f, 1f);
#endif

            var textInput = sample.Q(className: TextField.inputUssClassName);
            textInput.style.backgroundColor = new Color(0.13f, 0.13f, 0.13f);

            var root = uiDocument.rootVisualElement;
            root.Add(sample);
        }
    }
}