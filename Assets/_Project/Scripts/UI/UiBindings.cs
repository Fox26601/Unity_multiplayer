using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Reads and writes UI fields whether the scene uses TMP or legacy uGUI.</summary>
    public static class UiBindings
    {
        public static string GetInputText(Component field)
        {
            if (field is TMP_InputField tmp) return tmp.text;
            if (field is InputField legacy) return legacy.text;
            return string.Empty;
        }

        public static void SetInputText(Component field, string value)
        {
            if (field is TMP_InputField tmp) tmp.text = value;
            else if (field is InputField legacy) legacy.text = value;
        }

        public static void SetInputInteractable(Component field, bool interactable)
        {
            if (field is TMP_InputField tmp) tmp.interactable = interactable;
            else if (field is InputField legacy) legacy.interactable = interactable;
        }

        public static void SetLabelText(Component label, string value)
        {
            if (label is TMP_Text tmp) tmp.text = value;
            else if (label is Text legacy) legacy.text = value;
        }

        public static void SetLabelColor(Component label, Color color)
        {
            if (label is TMP_Text tmp) tmp.color = color;
            else if (label is Text legacy) legacy.color = color;
        }

        public static void SetLabelActive(Component label, bool active)
        {
            if (label == null) return;
            label.gameObject.SetActive(active);
        }

        public static T FindComponent<T>(Transform root, string path) where T : Component
        {
            if (root == null) return null;
            var tr = string.IsNullOrEmpty(path) ? root : root.Find(path);
            return tr != null ? tr.GetComponent<T>() : null;
        }
    }
}
