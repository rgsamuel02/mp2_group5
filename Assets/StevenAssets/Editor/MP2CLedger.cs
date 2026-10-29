using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Steven.MP2A.EditorTools
{
    /// <summary>
    /// Tools > Steven > Restyle Pantry Ledger  (open StevenScene first)
    /// Turns the chalkboard Pantry Board into an open bookkeeping ledger: leather-bound cover, cream ruled paper with red
    /// money columns, a book gutter, IM Fell English handwriting-style headings and Courier Prime figures. Gold and
    /// Reputation get double-height highlighted lines at the top of the first page. The rows of each column are packed
    /// to the top (no gaps left by removed rows). Safe to run again. Delete this script afterwards if you like.
    /// </summary>
    public static class MP2CLedger
    {
        const string FontFolder = "Assets/StevenAssets/Fonts";
        const string TexFolder = "Assets/StevenAssets/Textures";
        const string MatFolder = "Assets/StevenAssets/Materials";

        static readonly Color Ink = new Color(0.10f, 0.12f, 0.28f);
        static readonly Color Figures = new Color(0.08f, 0.08f, 0.10f);
        static readonly Color RuleRed = new Color(0.67f, 0.16f, 0.16f);
        static readonly Color RuleBlue = new Color(0.55f, 0.69f, 0.83f);
        static readonly Color GoldInk = new Color(0.58f, 0.40f, 0.02f);
        static readonly Color GoldHighlight = new Color(0.98f, 0.92f, 0.62f);
        static readonly Color RepInk = new Color(0.58f, 0.10f, 0.16f);
        static readonly Color RepHighlight = new Color(0.96f, 0.84f, 0.84f);

        [MenuItem("Tools/Steven/Restyle Pantry Ledger")]
        public static void Restyle()
        {
            var scene = SceneManager.GetActiveScene();
            var boardRoot = Find(scene, "Pantry Board");
            var board = boardRoot != null ? boardRoot.GetComponentInChildren<ResourceBoard>(true) : null;
            if (board == null)
            {
                EditorUtility.DisplayDialog("Pantry Ledger", "No Pantry Board found. Open StevenScene first.", "OK");
                return;
            }

            var fell = LoadFont("IMFellEnglish-Regular.ttf");
            var fellSc = LoadFont("IMFellEnglishSC-Regular.ttf");
            var figures = LoadFont("CourierPrime-Bold.ttf");
            var paper = LoadSprite(TexFolder + "/PantryLedger_Paper.png");
            var gutter = LoadSprite(TexFolder + "/PantryLedger_Gutter.png");
            if (fell == null || fellSc == null || figures == null || paper == null)
            {
                EditorUtility.DisplayDialog("Pantry Ledger", "Fonts or textures missing. Extract the zip into Assets/ first (StevenAssets/Fonts and StevenAssets/Textures).", "OK");
                return;
            }

            var canvas = (RectTransform)board.transform;

            // Leather cover.
            var frame = boardRoot.Find("Frame");
            if (frame != null && frame.GetComponent<MeshRenderer>() != null)
                frame.GetComponent<MeshRenderer>().sharedMaterial = Leather();

            // Paper page (the old chalkboard surface).
            var chalk = canvas.Find("Chalk");
            if (chalk != null && chalk.GetComponent<Image>() != null)
            {
                var img = chalk.GetComponent<Image>();
                img.sprite = paper;
                img.type = Image.Type.Simple;
                img.color = Color.white;
                chalk.name = "Paper";
            }
            else if (canvas.Find("Paper") == null)
            {
                var p = NewImage(canvas, "Paper", paper, Color.white);
                Stretch(p);
                p.SetAsFirstSibling();
            }

            // Title and the double red line under it.
            var title = canvas.Find("Title");
            if (title != null && title.GetComponent<Text>() != null)
                Style(title.GetComponent<Text>(), "Pantry Ledger", fell, 190, new Color(0.24f, 0.12f, 0.08f), TextAnchor.MiddleCenter, FontStyle.Normal);
            Destroy(canvas, "Ledger Title Rule");
            var titleRule = new GameObject("Ledger Title Rule", typeof(RectTransform)).GetComponent<RectTransform>();
            titleRule.SetParent(canvas, false);
            titleRule.SetSiblingIndex(PaperIndex(canvas) + 1);
            TopStretch(titleRule, 248f, 30f, 40f);
            HLine(titleRule, "Line A", 0f, 7f, RuleRed);
            HLine(titleRule, "Line B", 18f, 7f, RuleRed);

            // Book gutter down the middle.
            Destroy(canvas, "Ledger Gutter");
            if (gutter != null)
            {
                var g = NewImage(canvas, "Ledger Gutter", gutter, Color.white);
                g.anchorMin = new Vector2(0.5f, 0f);
                g.anchorMax = new Vector2(0.5f, 1f);
                g.sizeDelta = new Vector2(170f, 0f);
                g.anchoredPosition = Vector2.zero;
                g.SetSiblingIndex(PaperIndex(canvas) + 1);
            }

            // Which rows are on show (the Kitchen Simplifier hides the rest at runtime).
            var simplifier = Object.FindAnyObjectByType<KitchenSimplifier>(FindObjectsInactive.Include);
            var shown = simplifier != null && simplifier.shownResources != null && simplifier.shownResources.Count > 0
                ? new HashSet<ResourceId>(simplifier.shownResources) : null;

            // Group the board's rows by column.
            var byColumn = new Dictionary<RectTransform, List<KeyValuePair<ResourceId, RectTransform>>>();
            foreach (var row in board.rows)
            {
                if (row == null || row.value == null)
                    continue;
                var rowRt = row.value.transform.parent as RectTransform;
                var col = rowRt != null ? rowRt.parent as RectTransform : null;
                if (col == null || (shown != null && !shown.Contains(row.id)))
                    continue;
                if (!byColumn.TryGetValue(col, out var list))
                    byColumn[col] = list = new List<KeyValuePair<ResourceId, RectTransform>>();
                list.Add(new KeyValuePair<ResourceId, RectTransform>(row.id, rowRt));
            }

            // Shrink the board to fit its longest column (plus one spare ruled line) and hang it at eye level.
            var maxSlots = 1;
            var slotHeight = 155.8f;
            foreach (var kv in byColumn)
            {
                var n = 0;
                foreach (var r in kv.Value)
                    n += r.Key == ResourceId.Gold || r.Key == ResourceId.Reputation ? 2 : 1;
                maxSlots = Mathf.Max(maxSlots, n);
                if (kv.Value.Count > 0 && kv.Value[0].Value.sizeDelta.y > 1f && kv.Value[0].Value.sizeDelta.y < 200f)
                    slotHeight = kv.Value[0].Value.sizeDelta.y;
            }
            // Column top inset 280 (title) + header 180 + lines + bottom inset 40 + margin 40.
            var canvasHeight = 280f + 180f + slotHeight * (maxSlots + spareLines) + 80f;
            var unit = canvas.localScale.y > 0f ? canvas.localScale.y : 0.0005f;
            var border = frame != null ? frame.localScale.y - canvas.rect.height * unit : 0.1f;
            canvas.sizeDelta = new Vector2(canvas.sizeDelta.x, canvasHeight);
            var lp = canvas.localPosition;
            canvas.localPosition = new Vector3(lp.x, boardCenterHeight, lp.z);
            if (frame != null)
            {
                var fs = frame.localScale;
                frame.localScale = new Vector3(fs.x, canvasHeight * unit + Mathf.Clamp(border, 0.06f, 0.1f), fs.z);
                frame.localPosition = new Vector3(frame.localPosition.x, boardCenterHeight, frame.localPosition.z);
            }

            var rowCount = 0;
            foreach (var kv in byColumn)
            {
                var col = kv.Key;
                var rows = kv.Value;
                rows.Sort((a, b) => b.Value.anchoredPosition.y.CompareTo(a.Value.anchoredPosition.y));   // top first
                var h = slotHeight;
                const float firstTop = 180f;   // rows start under the column header

                // Header.
                var header = col.Find("Header");
                if (header != null && header.GetComponent<Text>() != null)
                {
                    var t = header.GetComponent<Text>();
                    Style(t, TitleCase(t.text), fellSc, 110, RuleRed, TextAnchor.MiddleCenter, FontStyle.Normal);
                }

                // Business column: Gold and Reputation first, double height.
                rows.Sort((a, b) => Priority(a.Key).CompareTo(Priority(b.Key)));
                var slot = 0;
                foreach (var r in rows)
                {
                    var big = r.Key == ResourceId.Gold || r.Key == ResourceId.Reputation;
                    var slots = big ? 2 : 1;
                    var rt = r.Value;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, h * slots);
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -(firstTop + h * slot + h * slots / 2f));
                    slot += slots;
                    rowCount++;

                    var name = rt.Find("Name") != null ? rt.Find("Name").GetComponent<Text>() : null;
                    var value = rt.Find("Value") != null ? rt.Find("Value").GetComponent<Text>() : null;
                    Destroy(rt, "Highlight");
                    if (big)
                    {
                        var hl = NewImage(rt, "Highlight", null, r.Key == ResourceId.Gold ? GoldHighlight : RepHighlight);
                        Stretch(hl);
                        hl.offsetMin = new Vector2(6f, 8f);
                        hl.offsetMax = new Vector2(-6f, -8f);
                        hl.SetAsFirstSibling();
                        var ink = r.Key == ResourceId.Gold ? GoldInk : RepInk;
                        if (name != null)
                        {
                            Style(name, name.text, fell, 140, ink, TextAnchor.MiddleLeft, FontStyle.Normal);
                            SetX((RectTransform)name.transform, 0.04f, 0.58f);
                        }
                        if (value != null)
                        {
                            // Wider than a normal line so 4-digit Gold still shows at full size.
                            Style(value, value.text, figures, 210, ink, TextAnchor.MiddleRight, FontStyle.Normal);
                            SetX((RectTransform)value.transform, 0.45f, 0.98f);
                        }
                    }
                    else
                    {
                        if (name != null)
                            Style(name, name.text, fell, 90, Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
                        if (value != null)
                            Style(value, value.text, figures, 100, Figures, TextAnchor.MiddleRight, FontStyle.Normal);
                    }
                }

                // Ruled lines and the red money column, behind the rows.
                Destroy(col, "Ledger Lines");
                var lines = new GameObject("Ledger Lines", typeof(RectTransform)).GetComponent<RectTransform>();
                lines.SetParent(col, false);
                Stretch(lines);
                lines.SetAsFirstSibling();
                var colHeight = col.rect.height > 10f ? col.rect.height : 3180f;
                for (var y = firstTop; y <= colHeight - 20f; y += h)
                    HLine(lines, "Rule", y, 4f, RuleBlue);
                VLine(lines, "Money Line A", 0.725f, -5f, firstTop - 10f, RuleRed);
                VLine(lines, "Money Line B", 0.725f, 9f, firstTop - 10f, RuleRed);
                EditorUtility.SetDirty(col);
            }

            EditorUtility.SetDirty(board);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = boardRoot.gameObject;
            EditorUtility.DisplayDialog("Pantry Ledger", "Restyled the Pantry Board as a ledger (" + rowCount + " lines, Gold and Reputation highlighted). Scene saved.", "OK");
        }

        const int spareLines = 1;
        const float boardCenterHeight = 1.45f;   // metres above the floor

        static int Priority(ResourceId id) => id == ResourceId.Gold ? 0 : id == ResourceId.Reputation ? 1 : 2;

        // ------------------------------------------------------------------ helpers

        static void Style(Text t, string text, Font font, int size, Color color, TextAnchor anchor, FontStyle style)
        {
            t.text = text;
            t.font = font;
            t.fontStyle = style;
            t.color = color;
            t.alignment = anchor;
            t.fontSize = size;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(10, size / 3);
            t.resizeTextMaxSize = size;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            EditorUtility.SetDirty(t);
        }

        static void SetX(RectTransform rt, float min, float max)
        {
            rt.anchorMin = new Vector2(min, rt.anchorMin.y);
            rt.anchorMax = new Vector2(max, rt.anchorMax.y);
            rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
        }

        static RectTransform NewImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void TopStretch(RectTransform rt, float top, float height, float sideInset)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-2f * sideInset, height);
            rt.anchoredPosition = new Vector2(0f, -top);
        }

        /// <summary>A horizontal line <paramref name="top"/> units below the parent's top edge.</summary>
        static void HLine(RectTransform parent, string name, float top, float thickness, Color color)
        {
            var rt = NewImage(parent, name, null, color);
            TopStretch(rt, top, thickness, 0f);
        }

        /// <summary>A vertical line at a fraction of the parent's width, from <paramref name="top"/> down to the bottom.</summary>
        static void VLine(RectTransform parent, string name, float xFraction, float xOffset, float top, Color color)
        {
            var rt = NewImage(parent, name, null, color);
            rt.anchorMin = new Vector2(xFraction, 0f);
            rt.anchorMax = new Vector2(xFraction, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(xOffset - 2f, 20f);
            rt.offsetMax = new Vector2(xOffset + 2f, -top);
        }

        static int PaperIndex(Transform canvas)
        {
            var p = canvas.Find("Paper");
            return p != null ? p.GetSiblingIndex() : 0;
        }

        static void Destroy(Transform parent, string childName)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
                if (parent.GetChild(i).name == childName)
                    Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        static string TitleCase(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            var words = s.ToLowerInvariant().Split(' ');
            for (var i = 0; i < words.Length; i++)
            {
                var w = words[i];
                if (w.Length == 0 || (i > 0 && (w == "en" || w == "of" || w == "and")))
                    continue;
                words[i] = char.ToUpperInvariant(w[0]) + w.Substring(1);
            }
            return string.Join(" ", words);
        }

        static Font LoadFont(string file)
        {
            var path = FontFolder + "/" + file;
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Font>(path);
        }

        static Sprite LoadSprite(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return null;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Material Leather()
        {
            var path = MatFolder + "/PantryLedger_Leather.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                return mat;
            var src = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/White.mat");
            mat = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var c = new Color(0.30f, 0.11f, 0.08f);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", c);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static Transform Find(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name)
                        return t;
            return null;
        }
    }
}
