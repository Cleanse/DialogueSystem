using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// Attribute to mark portrait string fields for custom property drawer.
    /// This provides enhanced editor experience with portrait dropdowns.
    /// </summary>
    public class PortraitAttribute : PropertyAttribute
    {
        public PortraitAttribute()
        {
        }
    }
}