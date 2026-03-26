using System;
using System.Drawing;
using System.Windows.Forms;

namespace MultiCompte
{
    internal static class UiScale
    {
        // Baseline utilisée pour calibrer l'agrandissement des interfaces.
        private const float BASE_W = 1920f;
        private const float BASE_H = 1080f;

        public static void ApplyToForm(Form form, bool enlargeOnly = true)
        {
            var wa = Screen.FromControl(form).WorkingArea;
            float fx = wa.Width / BASE_W;
            float fy = wa.Height / BASE_H;
            float factor = Math.Min(fx, fy);

            if (enlargeOnly)
                factor = Math.Max(1f, factor);

            // Garde-fou pour éviter des UI disproportionnées.
            factor = Math.Clamp(factor, 1f, 1.45f);
            if (factor <= 1.02f) return;

            form.Scale(new SizeF(factor, factor));
        }
    }
}
