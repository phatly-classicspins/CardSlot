using System.Collections.Generic;
using UnityEngine;

namespace Game.Views
{
    /// <summary>
    /// The 3D board (CR-003): targets, holding area, tray and stacks as lit meshes under the scene's
    /// <c>WorldRoot</c>, seen by the orthographic GamePlay camera through a tilted board root. It renders the
    /// same <see cref="BoardVisual"/> the 2D view takes and decides nothing. Card faces reuse the approved
    /// card sprites (art v1) as textures; colours come from <see cref="DesignTokens"/>.
    /// </summary>
    /// <remarks>
    /// Placement (rule #16): anchored to the game's own space and drawn through Renderers ⇒ WorldRoot. The
    /// controller <c>Stamp</c>s this root while it is empty; children are created afterwards on the root's
    /// layer, because <c>Stamp</c> zeroes every node's local z and would flatten the meshes.
    /// </remarks>
    public sealed class Board3DView : MonoBehaviour
    {
        [SerializeField] private Sprite[] _cardFace = new Sprite[6];
        [SerializeField] private Sprite[] _cardMini = new Sprite[6];
        [SerializeField] private Sprite _ground;
        [Tooltip("Board tilt in degrees around X: 0 = straight top-down, larger = more side visible.")]
        [SerializeField] private float _tilt = 35f;   // D-025

        // board geometry (world units)
        private const float CardW = 150f, CardL = 206f, CardT = 12f, CardGap = 3f, CardRadius = 22f;
        private const float MiniW = 64f, MiniL = 138f;

        private Transform _board;
        private Material _table, _slot, _rim, _inner, _rail, _railWarn, _cell, _cellWarn, _edgeLight;
        private Material[] _cardSide, _cardTop, _miniTop, _pedestal, _cardSideCovered, _cardTopCovered;
        private Mesh _cardMesh, _miniMesh, _pedestalMesh;
        private readonly List<GameObject> _dynamic = new List<GameObject>();
        private readonly Dictionary<int, List<Renderer>> _stackRenderers = new Dictionary<int, List<Renderer>>();

        public float Tilt { get => _tilt; set { _tilt = value; if (_board != null) { ApplyTilt(); if (_last != null) Render(_last); } } }
        private BoardVisual _last;

        private float Cos => Mathf.Cos(_tilt * Mathf.Deg2Rad);

        // mock-up layout (top-left px of the 1080×1920 frame) → board-local plane coordinates
        private Vector3 P(float xTopLeft, float yTopLeft, float height = 0f) =>
            new Vector3(xTopLeft - 540f, (875f - yTopLeft) / Cos, -height);

        private void EnsureBuilt()
        {
            if (_board != null) return;
            BuildMaterials();
            _board = new GameObject("Board").transform;
            _board.SetParent(transform, false);
            SetLayer(_board.gameObject);
            ApplyTilt();
            BuildLight();
            BuildBackdrop();
        }

        private void ApplyTilt()
        {
            _board.localPosition = new Vector3(0f, 960f - 875f, 0f);
            _board.localRotation = Quaternion.Euler(_tilt, 0f, 0f);
        }

        private void SetLayer(GameObject go) => go.layer = gameObject.layer;

        // the approved ground gradient, untilted, far behind the board (z budget ends at +1000)
        private void BuildBackdrop()
        {
            if (_ground == null) return;
            var go = new GameObject("Backdrop");
            SetLayer(go);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 990f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _ground;
            var size = _ground.bounds.size;
            go.transform.localScale = new Vector3(1800f / size.x, 3200f / size.y, 1f);
        }

        private void BuildLight()
        {
            var go = new GameObject("KeyLight");
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.Euler(55f, -25f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = DesignTokens.Hex("FFF1DD");
            light.intensity = 0.95f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            light.cullingMask = 1 << gameObject.layer;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = DesignTokens.Hex("D9CBBD");
        }

        private static Material Mat(Color c, float smooth = 0.35f, Texture tex = null)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Cull", 0f);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            return m;
        }

        private void BuildMaterials()
        {
            _table = Mat(DesignTokens.GroundBottom, 0.1f);
            _slot = Mat(DesignTokens.Surface, 0.25f);
            _rim = Mat(DesignTokens.TrayRim, 0.3f);
            _inner = Mat(DesignTokens.TrayInner, 0.15f);
            _rail = Mat(DesignTokens.Rail, 0.25f);
            _railWarn = Mat(DesignTokens.Danger, 0.3f);
            _cell = Mat(DesignTokens.SurfaceSunken, 0.1f);
            _cellWarn = Mat(DesignTokens.Hex("F6B3A4"), 0.1f);
            _edgeLight = Mat(Color.white, 0.4f);
            _cardSide = new Material[6]; _cardTop = new Material[6]; _miniTop = new Material[6]; _pedestal = new Material[6];
            _cardSideCovered = new Material[6]; _cardTopCovered = new Material[6];
            for (int k = 0; k < 6; k++)
            {
                var c = DesignTokens.CardFace[k];
                _cardSide[k] = Mat(Color.Lerp(c, Color.black, 0.18f), 0.45f);
                _cardTop[k] = Mat(Color.white, 0.45f, _cardFace[k] != null ? _cardFace[k].texture : null);
                _miniTop[k] = Mat(Color.white, 0.45f, _cardMini[k] != null ? _cardMini[k].texture : null);
                _pedestal[k] = Mat(c, 0.5f);
                _cardSideCovered[k] = Mat(Color.Lerp(c, Color.black, 0.18f) * DesignTokens.CoveredTint, 0.3f);
                _cardTopCovered[k] = Mat(DesignTokens.CoveredTint, 0.3f, _cardFace[k] != null ? _cardFace[k].texture : null);
            }
            _cardMesh = MeshKit.RoundedSlab(CardW, CardL, CardT, CardRadius, 6, 2f);
            _miniMesh = MeshKit.RoundedSlab(MiniW, MiniL, 8f, 12f, 4, 1.5f);
            _pedestalMesh = MeshKit.Cylinder(110f, 34f);
        }

        private GameObject Piece(string name, Mesh mesh, Material top, Material sides, Vector3 pos, float yaw = 0f)
        {
            var go = new GameObject(name);
            SetLayer(go);
            go.transform.SetParent(_board, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, yaw);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = new[] { top, sides };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
            _dynamic.Add(go);
            return go;
        }

        private GameObject Slab(string name, float x, float y, float w, float h, float depth, float radius, Material m, float height = 0f)
        {
            var mesh = MeshKit.RoundedSlab(w, h / Cos, depth, radius, 6, Mathf.Min(6f, depth * 0.3f));
            return Piece(name, mesh, m, m, P(x + w / 2f, y + h / 2f, height));
        }

        /// <summary>Redraw the board from scratch (the motion of 6c replays steps on top of this).</summary>
        /// <summary>The world-space XY rectangle each stack covers on screen (orthographic camera ⇒ the
        /// projection is the XY of the bounds), or an empty rect for an empty stack. Game units, not pixels.</summary>
        public Rect[] StackWorldRects(int count)
        {
            var rects = new Rect[count];
            for (int i = 0; i < count; i++)
            {
                if (!_stackRenderers.TryGetValue(i, out var list) || list.Count == 0) continue;
                var b = list[0].bounds;
                foreach (var r in list) b.Encapsulate(r.bounds);
                rects[i] = Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
            }
            return rects;
        }

        public void Render(BoardVisual v)
        {
            EnsureBuilt();
            _last = v;
            foreach (var go in _dynamic) Destroy(go);
            _dynamic.Clear();
            _stackRenderers.Clear();

            DrawTargets(v);
            DrawBuffer(v);
            DrawTray(v);
        }

        private void DrawTargets(BoardVisual v)
        {
            int n = v.Targets.Length;
            float w = n == 2 ? DesignTokens.TargetSlotWidthTwo : DesignTokens.TargetSlotWidth;
            float gap = n == 2 ? DesignTokens.TargetSlotGapTwo : DesignTokens.TargetSlotGap;
            float x0 = 540f - (n * w + (n - 1) * gap) / 2f;
            for (int i = 0; i < n; i++)
            {
                float sx = x0 + i * (w + gap);
                Slab($"Slot{i}", sx, DesignTokens.TargetsTop, w, DesignTokens.TargetsHeight, 10f, 40f, _slot, 4f);
                var t = v.Targets[i];
                if (!t.Has) continue;
                float cx = sx + w / 2f, cy = DesignTokens.TargetsTop + 250f;
                Piece($"Pedestal{i}", _pedestalMesh, _pedestal[t.Color], _pedestal[t.Color], P(cx, cy, 14f));
                for (int j = 0; j < t.Filled; j++)
                    Piece($"Target{i}Card{j}", _cardMesh, _cardTop[t.Color], _cardSide[t.Color], P(cx, cy - 20f, 48f + j * (CardT + CardGap)));
            }
        }

        private void DrawBuffer(BoardVisual v)
        {
            float m = DesignTokens.ScreenMargin;
            Slab("Rail", m, DesignTokens.BufferTop, 1080f - 2f * m, DesignTokens.BufferHeight, 30f, 48f, v.BufferWarn ? _railWarn : _rail, 4f);
            int cap = Mathf.Max(1, v.BufferCapacity);
            float inner = DesignTokens.BufferInnerWidth;
            float cw = Mathf.Min(DesignTokens.BufferCellWidth, Mathf.Floor((inner - 8f * (cap - 1)) / cap));
            float step = cap > 1 ? cw + Mathf.Floor((inner - cw * cap) / (cap - 1)) : 0f;
            for (int i = 0; i < cap; i++)
            {
                float x = m + DesignTokens.BufferInset + i * step;
                float y = DesignTokens.BufferTop + DesignTokens.BufferCellTop;
                Slab($"Cell{i}", x, y, cw, DesignTokens.BufferCellHeight, 4f, 12f, v.BufferWarn ? _cellWarn : _cell, 34f);
                if (i < v.Buffer.Length)
                {
                    var card = Piece($"Held{i}", _miniMesh, _miniTop[v.Buffer[i]], _cardSide[v.Buffer[i]], P(x + cw / 2f, y + DesignTokens.BufferCellHeight / 2f, 38f));
                    card.transform.localScale = new Vector3(cw / MiniW, 1f / Cos, 1f);
                }
            }
        }

        private void DrawTray(BoardVisual v)
        {
            float m = DesignTokens.ScreenMargin, inset = DesignTokens.TrayRimInset;
            float trayH = DesignTokens.TrayHeight;
            // wooden base, with the felt plate raised on top of it (the base shows as a frame around the plate)
            Slab("TrayRim", m, DesignTokens.TrayTop, 1080f - 2f * m, trayH, 30f, 48f, _rim, 4f);
            Slab("TrayInner", m + inset, DesignTokens.TrayTop + inset, 1080f - 2f * (m + inset), trayH - 2f * inset, 10f, 34f, _inner, 34f);
            float ox = m + inset, oy = DesignTokens.TrayTop + inset;
            for (int i = 0; i < v.Stacks.Length; i++)
            {
                var s = v.Stacks[i];
                if (s.Colors == null || s.Colors.Length == 0) continue;
                int count = s.Colors.Length;
                float baseHeight = 44f + s.Layer * 60f;     // higher layers physically sit above lower ones
                for (int d = count - 1; d >= 0; d--)
                {
                    int k = s.Colors[d];
                    float h = baseHeight + (count - 1 - d) * (CardT + CardGap);
                    // lower cards peek out toward the viewer, as in the 2D mock-up
                    float peek = d * DesignTokens.CardStep;
                    bool covered = s.Covered;
                    var top = d == 0 ? (covered ? _cardTopCovered[k] : _cardTop[k]) : (covered ? _cardSideCovered[k] : _cardSide[k]);
                    var side = covered ? _cardSideCovered[k] : _cardSide[k];
                    var card = Piece($"Stack{i}Card{d}", _cardMesh, top, side, P(ox + s.X + CardW / 2f, oy + s.Y + CardL / 2f + peek, h));
                    if (!_stackRenderers.TryGetValue(i, out var list)) _stackRenderers[i] = list = new List<Renderer>();
                    list.Add(card.GetComponent<Renderer>());
                }
            }
        }
    }
}
