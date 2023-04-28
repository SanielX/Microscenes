using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microscenes
{
    internal static class ColorUtils
    {
        /// <summary>
        /// Color format 0xRR_GG_BB
        /// </summary>
        public static Color FromHEX(int hex)
        {
            float r = ((hex & 0xFF0000) >> 16) / 255f;
            float g = ((hex & 0x00FF00) >> 8)  / 255f;
            float b = ((hex & 0x0000FF) >> 0)  / 255f;
            return new Color(r, g, b, 1f);
        }
    }
}
