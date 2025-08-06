using System.Collections.Generic;
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


            var dropdownField = new DropdownField("Select an option",
                new List<string> { "Option 1", "Option 2", "Option 3" }, "Option 1");
      
            var container = new VisualElement()
            {
                style =
                {
                    width = 300,
                    height = 200,
                    backgroundColor = Color.grey
                }
            };
            container.Add(SampleMenu.CreateGenericDropdownMenuButton());
            container.Add(SampleMenu.CreateNestedDropdownMenuButton());
            container.Add(dropdownField);
            
            var root = uiDocument.rootVisualElement;
            root.Add(container);
        }
    }
}