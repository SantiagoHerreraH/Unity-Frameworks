using UnityEngine;

namespace SilverPillar.Core
{
    public static class StringExtensions
    {
        // Extensión para añadir color mediante un string (ej. "red")
        public static string Color(this string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }

        // Extensión para usar la clase Color de Unity directamente
        public static string Color(this string text, Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGBA(color);
            return $"<color=#{hex}>{text}</color>";
        }
    }

}
