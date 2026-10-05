using ColossalFramework.UI;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// A small atlas of our own: a rounded filled rectangle, a thin rounded frame and a plain solid sprite (all white,
    /// tinted through the colour of the component). Plus the yellow selection frame of buttons.
    /// </summary>
    internal static class Flat
    {
        public const string Round = "QtRound";
        public const string FrameSprite = "QtFrame";
        public const string Solid = "EmptySprite";

        private const int Cell = 32;
        private const float Radius = 3.2f;   // small rounding
        private const float Thickness = 1.3f; // thin frame
        private static UITextureAtlas _atlas;

        public static UITextureAtlas Atlas
        {
            get
            {
                if (_atlas == null) _atlas = Build();
                return _atlas;
            }
        }

        private static UITextureAtlas Build()
        {
            Texture2D tex = new Texture2D(Cell * 3, Cell, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[Cell * 3 * Cell];
            float half = Cell * 0.5f;
            for (int y = 0; y < Cell; y++)
            {
                for (int x = 0; x < Cell; x++)
                {
                    float qx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - Radius), 0f);
                    float qy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - Radius), 0f);
                    float dist = Mathf.Sqrt(qx * qx + qy * qy) - Radius;
                    float fill = Mathf.Clamp01(0.5f - dist);
                    float inner = Mathf.Clamp01(0.5f - (dist + Thickness));
                    px[y * Cell * 3 + x] = new Color(1f, 1f, 1f, fill);
                    px[y * Cell * 3 + Cell + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(fill - inner));
                    px[y * Cell * 3 + Cell * 2 + x] = new Color32(255, 255, 255, 255);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false);

            UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            atlas.name = "QuayToolsAtlas";
            Material m = new Material(UIView.GetAView().defaultAtlas.material);
            m.mainTexture = tex;
            atlas.material = m;

            atlas.sprites.Add(Info(Round, 0, tex, 6));
            atlas.sprites.Add(Info(FrameSprite, 1, tex, 6));
            atlas.sprites.Add(Info(Solid, 2, tex, 0));
            atlas.RebuildIndexes();
            return atlas;
        }

        private static UITextureAtlas.SpriteInfo Info(string name, int cell, Texture2D tex, int border)
        {
            UITextureAtlas.SpriteInfo s = new UITextureAtlas.SpriteInfo();
            s.name = name;
            s.texture = tex;
            s.region = new Rect(cell / 3f, 0f, 1f / 3f, 1f);
            s.border = new RectOffset(border, border, border, border);
            return s;
        }

        /// <summary>Gives a button a thin yellow frame that shows while the mouse is over it (and while "kept").</summary>
        public static void AddFrame(UIButton b)
        {
            if (b.objectUserData is UIPanel) return;
            UIPanel frame = b.AddUIComponent<UIPanel>();
            frame.atlas = Atlas;
            frame.backgroundSprite = FrameSprite;
            frame.color = new Color32(250, 200, 40, 255);
            frame.isInteractive = false;
            frame.isVisible = false;
            b.objectUserData = frame;

            b.eventMouseEnter += delegate (UIComponent c, UIMouseEventParameter p) { Show(b, b.isEnabled); };
            b.eventMouseLeave += delegate (UIComponent c, UIMouseEventParameter p) { Show(b, b.stringUserData == "k"); };
        }

        private static void Show(UIButton b, bool on)
        {
            UIPanel frame = b.objectUserData as UIPanel;
            if (frame == null) return;
            if (on)
            {
                frame.size = b.size;
                frame.relativePosition = Vector3.zero;
                frame.zOrder = int.MaxValue / 2;
            }
            frame.isVisible = on;
        }

        /// <summary>Keeps the frame visible (the button is the chosen one) or not.</summary>
        public static void Keep(UIButton b, bool on)
        {
            if (b == null) return;
            b.stringUserData = on ? "k" : null;
            Show(b, on);
        }
    }
}
