using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using OutpostZero.Shell;

namespace OutpostZero.UI
{
    /// <summary>
    /// Gives every UI Toolkit panel a font that covers the current language's script. Latin languages keep the
    /// default font; Cyrillic and CJK languages get an installed OS font first, with the other scripts chained behind it.
    /// </summary>
    public static class FontFallback
    {
        private static readonly List<VisualElement> roots = new List<VisualElement>();
        private static readonly Dictionary<string, FontAsset> loaded = new Dictionary<string, FontAsset>();
        private static string appliedFor;
        private static FontAsset primary;
        private static string[] installed;

        public static void Dress(VisualElement root)
        {
            if (root == null) return;
            if (!roots.Contains(root)) roots.Add(root);
            Style(root);
        }

        public static void Apply(string language)
        {
            string code = FontChain.Base(language);
            if (code == appliedFor) return;
            appliedFor = code;
            primary = FontChain.Needed(code) ? Build(code) : null;
            for (int i = roots.Count - 1; i >= 0; i--)
            {
                if (roots[i] == null) roots.RemoveAt(i);
                else Style(roots[i]);
            }
        }

        private static void Style(VisualElement root)
        {
            if (primary == null) root.style.unityFontDefinition = StyleKeyword.Null;
            else root.style.unityFontDefinition = FontDefinition.FromSDFFont(primary);
        }

        private static FontAsset Build(string language)
        {
            FontAsset head = null;
            var tail = new List<FontAsset>();
            foreach (var script in FontChain.Scripts(language))
            {
                var asset = Load(script, language);
                if (asset == null) continue;
                if (head == null) head = asset;
                else if (asset != head && !tail.Contains(asset)) tail.Add(asset);
            }
            if (head == null)
            {
                Debug.LogWarning("[FontFallback] No installed font covers " + language + "; text uses the default font.");
                return null;
            }
            head.fallbackFontAssetTable = tail;
            return head;
        }

        private static FontAsset Load(TextScript script, string language)
        {
            if (installed == null) installed = Font.GetOSInstalledFontNames() ?? new string[0];
            string probe = FontChain.Probe(script, language);
            foreach (var family in FontChain.Families(script, language))
            {
                if (FontChain.Pick(installed, new[] { family }).Length == 0) continue;
                if (!loaded.TryGetValue(family, out var asset))
                {
                    asset = FontAsset.CreateFontAsset(family, "Regular");
                    loaded[family] = asset;
                }
                if (asset != null && asset.HasCharacters(probe, out uint[] _, false, true)) return asset;
            }
            return null;
        }
    }
}
