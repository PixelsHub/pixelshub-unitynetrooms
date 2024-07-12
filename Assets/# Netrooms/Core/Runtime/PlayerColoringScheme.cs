using System.Collections.Generic;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class PlayerColoringScheme : MonoBehaviour
    {
        public static IReadOnlyList<Color> Colors => colors;

        public static readonly Color undefinedColor = Color.white;

        private static List<Color> colors;

        private static readonly string[] defaultColors = new string[]
        {
            "#4E6EF0",
            "#2A9E69",
            "#F0DB5A",
            "#F08055",
            "#C869DA",
            "#DB5364",
            "#A36641",
            "#6A2BDB"
        };

        [SerializeField]
        private List<Color> availableColors;

        static PlayerColoringScheme() 
        {
            colors = new(defaultColors.Length);

            foreach(string colorHtml in defaultColors)
            {
                if(ColorUtility.TryParseHtmlString(colorHtml, out var color))
                    colors.Add(color);
                else
                    Debug.Assert(false);
            }
        }

        public static Color GetColor(int index)
        {
            if(index < 0)
                return undefinedColor;

            if(index >= colors.Count)
            {
                index = colors.Count - 1;
                Debug.LogError("Requested color exceeded available configured number. Returning last indexed color.");
            }

            return colors[index];
        }

        private void Awake()
        {
            colors = availableColors;
            Destroy(this);
        }
    }
}
