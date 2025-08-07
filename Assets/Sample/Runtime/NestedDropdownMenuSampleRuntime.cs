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
            sample.style.backgroundColor = Color.darkGray;

            var root = uiDocument.rootVisualElement;
            root.Add(sample);
        }
    }
}