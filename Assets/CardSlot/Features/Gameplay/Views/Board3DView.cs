using System.Collections.Generic;
using UnityEngine;

namespace Game.Views
{
    /// <summary>
    /// The 3D board (CR-003): targets, holding area, tray and stacks as lit meshes under the scene's
    /// <c>WorldRoot</c>, seen by the orthographic GamePlay camera through a tilted board root. It renders the
    /// same <see cref="BoardVisual"/> the 2D view takes and decides nothing. CR-007 (the reference video):
    /// targets are pegs, cards are plain rounded squares with a hole that thread onto them, the next targets
    /// wait as a back row of pegs, and covered stacks keep their colour. Colours come from <see cref="DesignTokens"/>.
    /// </summary>
    /// <remarks>
    /// Placement (rule #16): anchored to the game's own space and drawn through Renderers ⇒ WorldRoot. The
    /// controller <c>Stamp</c>s this root while it is empty; children are created afterwards on the root's
    /// layer, because <c>Stamp</c> zeroes every node's local z and would flatten the meshes.
    /// </remarks>
    public sealed class Board3DView : MonoBehaviour
    {
        [SerializeField] private Sprite _ground;
        [Tooltip("Board tilt in degrees around X: 0 = straight top-down, larger = more side visible.")]
        [SerializeField] private float _tilt = 10f;   // D-027 (was 35°, D-025)

        // board geometry (world units)
        private const float CardW = 150f, CardL = 206f;                                // the layout cell a stack rect was made for
        // CR-007: the reference card — a portrait rounded rectangle (3:4) with a white frame line and a hole
        private const float CardFaceW = 150f, CardFaceH = 200f, CardRadius = 24f, HoleRadius = 24f;
        // CR-004 / CR-006: every card is a thin card; a group of 6 is about one old card's height
        private const float ThinT = 9f, ThinGap = 1.2f;
        private const float PileLean = 1.5f;     // each card of a straight pile sits this far sideways from the one below (D-027)
        private const float FanMaxSpread = 80f, FanMaxStep = 4f;
        // CR-007 pegs: tipped up the screen toward the viewer so the pole reads as standing, as in the reference
        private const float PegLean = 32f,   // Phat set it in the Scene (was 58°)
            PegBaseR = 70f, PegBaseT = 16f, PegPoleR = 12f;
        private const float PegBaseY = 540f, BackRowRise = 175f, BackRowScale = 0.72f;

        private Transform _board;
        private Material _hint;
        private Mesh _hintMesh;
        private readonly List<Renderer> _hints = new List<Renderer>();
        private Material _rim, _inner, _rail, _cell, _pole, _pegEmpty;
        private Material[] _cardSide, _cardTop, _pedestal;
        private Mesh _thinMesh, _pegBaseMesh;
        private readonly Dictionary<int, Mesh> _poleMeshes = new Dictionary<int, Mesh>();

        // a pole is exactly as tall as its full stack: capacity cards plus the gaps between them (Phat)
        private Mesh PoleMesh(int capacity)
        {
            if (!_poleMeshes.TryGetValue(capacity, out var mesh))
                _poleMeshes[capacity] = mesh = MeshKit.Cylinder(PegPoleR, capacity * (ThinT + ThinGap) - ThinGap, 24);
            return mesh;
        }
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

        private static Material Mat(Color c, float smooth = 0.35f)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Cull", 0f);
            return m;
        }

        private void BuildMaterials()
        {
            _rim = Mat(DesignTokens.TrayRim, 0.3f);
            _inner = Mat(DesignTokens.TrayInner, 0.15f);
            _rail = Mat(DesignTokens.Rail, 0.25f);
            _cell = Mat(DesignTokens.SurfaceSunken, 0.1f);
            _pole = Mat(DesignTokens.PegPole, 0.55f);
            _pegEmpty = Mat(DesignTokens.SurfaceSunken, 0.2f);
            _cardSide = new Material[6]; _cardTop = new Material[6]; _pedestal = new Material[6];
            for (int k = 0; k < 6; k++)
            {
                var c = DesignTokens.CardFace[k];
                _cardSide[k] = Mat(Color.Lerp(c, Color.black, 0.18f), 0.45f);
                _cardTop[k] = Mat(Color.white, 0.45f);
                _cardTop[k].SetTexture("_BaseMap", FaceTexture(c));
                _pedestal[k] = Mat(c, 0.5f);
            }
            _thinMesh = MeshKit.HoledSlab(CardFaceW, CardFaceH, ThinT, CardRadius, HoleRadius);
            _hintMesh = MeshKit.HoledSlab(CardFaceW + 22f, CardFaceH + 22f, 3f, CardRadius + 11f, CardFaceW / 2f - 6f);   // a rim only: the card hole still shows what is below
            _hint = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _hint.SetColor("_BaseColor", DesignTokens.HintOutline);
            _pegBaseMesh = MeshKit.Cylinder(PegBaseR, PegBaseT);
        }

        // the card face: its colour with a thin white frame line inset from the edge (the reference card's face)
        private static Texture2D FaceTexture(Color c)
        {
            const int W = 96, H = 128;
            float inset = 9f, line = 3.2f, r = 14f;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[W * H];
            var frame = Color.Lerp(c, Color.white, 0.75f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    // distance to a rounded rectangle inset from the edge (negative inside)
                    float qx = Mathf.Abs(x + 0.5f - W / 2f) - (W / 2f - inset - r), qy = Mathf.Abs(y + 0.5f - H / 2f) - (H / 2f - inset - r);
                    float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                    float a = Mathf.Clamp01(line / 2f + 0.5f - Mathf.Abs(d));
                    px[y * W + x] = Color.Lerp(c, frame, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private GameObject Piece(string name, Mesh mesh, Material top, Material sides, Vector3 pos, float yaw = 0f, Transform parent = null)
        {
            var go = new GameObject(name);
            SetLayer(go);
            go.transform.SetParent(parent != null ? parent : _board, false);
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

        /// <summary>True while cards are flying or a full peg is leaving (the controller waits on it before a result dialog).</summary>
        public bool IsAnimating => _ghosts.Count > 0 || _exits.Count > 0;

        /// <summary>Redraw the board from scratch.</summary>
        public void Render(BoardVisual v) => Render(v, null);

        /// <summary>Redraw the final state, then fly <paramref name="flights"/> into it card by card (CR-007):
        /// each landing card stays hidden in the new state until its ghost arrives. Whatever the previous
        /// change was still animating is finished first, so a quick next tap never leaves stale cards behind.</summary>
        public void Render(BoardVisual v, IReadOnlyList<CardFlight> flights)
        {
            EnsureBuilt();
            FinishAnimations();
            // sources are read from the state drawn before this change
            var from = new List<(Vector3 pos, Quaternion rot)>();
            if (flights != null)
                foreach (var f in flights)
                {
                    if (f.FromStack >= 0) { var p = StackCardPose(f.FromStack, f.FromDepth); from.Add((p.pos, p.rot)); }
                    else from.Add(HeldPose(f.FromHeld, v.BufferCapacity));
                }
            _last = v;
            // per slot, how many times its peg fills up in this change (a column can fill twice in one tap)
            var fills = new Dictionary<int, int>();
            if (flights != null)
                foreach (var f in flights)
                    if (f.Completes) fills[f.ToSlot] = Mathf.Max(fills.TryGetValue(f.ToSlot, out int c) ? c : 0, f.SwapGen + 1);
            // the peg that fills first is the one on screen now: keep it out of the redraw so it can linger and leave
            var leaving = new Dictionary<int, Transform>();
            foreach (var slot in fills.Keys)
            {
                var old = _dynamic.Find(go => go != null && go.name == $"Peg{slot}");
                if (old == null) continue;
                _dynamic.RemoveAll(go => go != null && go.transform.IsChildOf(old.transform));
                old.name = $"LeavingPeg{slot}";
                leaving[slot] = old.transform;
            }
            foreach (var go in _dynamic) Destroy(go);
            _dynamic.Clear();
            _hints.Clear();
            _stackRenderers.Clear();

            DrawTargets(v);
            DrawBuffer(v);
            DrawTray(v);
            var chains = new Dictionary<int, List<PegExit>>();
            foreach (var kv in fills)
                if (leaving.TryGetValue(kv.Key, out var first)) chains[kv.Key] = Chain(v, kv.Key, kv.Value, first, flights);
            if (flights != null) Launch(v, flights, from, chains);
        }

        // ── card flights and peg swaps (CR-007, CR-009) ───────────────────────────────────────────────
        private const float FlightInterval = 0.045f, FlightTime = 0.34f, FlightArc = 160f;
        // a full peg lingers, then sinks away; the peg behind moves up (Phat: "delay tí rồi mới biến mất")
        private const float PegLinger = 0.55f, PegLeaveTime = 0.3f, PegEnterTime = 0.35f, BackAppearTime = 0.2f;

        private sealed class Ghost
        {
            public GameObject Go;
            public Vector3 P0, P1, S0, S1;
            public Quaternion R0, R1;
            public float Start;
            public Renderer Reveal;
            public PegExit Ride;            // a card of a peg that fills up: it stays on that peg and leaves with it
            public bool ShowWhileWaiting;   // a card leaving a holding groove stays in it until it flies
        }

        // one full peg leaving and the peg behind it moving up; a column that fills twice has two in a row
        private sealed class PegExit
        {
            public Transform Old;
            public GameObject New, AfterNew;     // the peg moving up, and what then appears behind it
            public Vector3 Front, Back;
            public float Leave = float.MaxValue; // set from the landing time of the last card that fills Old
            public float Enter = -1f;
            public float Arrive => Leave + PegLeaveTime + PegEnterTime;
        }

        private readonly List<PegExit> _exits = new List<PegExit>();
        private readonly List<Ghost> _ghosts = new List<Ghost>();

        // the swaps of one slot: the peg on screen is replaced count times; the pegs in between are built here
        private List<PegExit> Chain(BoardVisual v, int slot, int count, Transform first, IReadOnlyList<CardFlight> flights)
        {
            int n = v.Targets.Length, capacity = BackCapacity(v);
            var back = P(PegX(slot, n), PegBaseY - BackRowRise, 30f);
            var chain = new List<PegExit>();
            var cur = first;
            for (int g = 1; g <= count; g++)
            {
                GameObject next;
                if (g == count) next = _dynamic.Find(go => go != null && go.name == $"Peg{slot}");
                else
                {
                    int color = -1;
                    foreach (var f in flights) if (f.ToSlot == slot && f.SwapGen == g) { color = f.Color; break; }
                    next = Peg($"GenPeg{slot}_{g}", PegX(slot, n), PegBaseY, color, 0, capacity, 1f, 60f).gameObject;
                }
                var e = new PegExit { Old = cur, New = next, Back = back };
                if (next != null)
                {
                    e.Front = next.transform.localPosition;
                    next.transform.localPosition = back;
                    next.transform.localScale = Vector3.one * BackRowScale;
                    next.SetActive(g == 1);                       // only the first successor waits in the back row now
                }
                chain.Add(e);
                _exits.Add(e);
                cur = next != null ? next.transform : null;
            }
            for (int k = 0; k < chain.Count; k++)
                chain[k].AfterNew = k + 1 < chain.Count ? chain[k + 1].New : _dynamic.Find(go => go != null && go.name == $"NextPeg{slot}");
            if (chain[chain.Count - 1].AfterNew != null) chain[chain.Count - 1].AfterNew.SetActive(false);
            return chain;
        }

        // end whatever is still moving: the next redraw starts from a still board
        private void FinishAnimations()
        {
            foreach (var g in _ghosts) if (g.Go != null) Destroy(g.Go);
            _ghosts.Clear();
            foreach (var e in _exits) if (e.Old != null) Destroy(e.Old.gameObject);
            _exits.Clear();
        }

        private (Vector3 pos, Quaternion rot) StackCardPose(int stack, int depth)
        {
            string name = $"Stack{stack}Card{depth}";
            foreach (var go in _dynamic)
                if (go != null && go.name == name) return (go.transform.position, go.transform.rotation);
            return (_board.TransformPoint(P(540f, DesignTokens.TrayTop + 300f, 40f)), _board.rotation);
        }

        // a held card lies on its long edge (Phat: "nằm ngang"): its length runs along the groove and fits it exactly,
        // its width stands up toward the camera, leaning a little so the near top-down camera sees its face
        private const float HeldLean = 17f;
        private static readonly float HeldScale = DesignTokens.BufferCellHeight / CardFaceH;
        private static readonly Quaternion HeldTurn = Quaternion.AngleAxis(HeldLean, Vector3.up) * Quaternion.LookRotation(Vector3.right, Vector3.up);
        private Vector3 HeldLocal(float x, float y) => P(x, y, 34f + CardFaceW * HeldScale / 2f * Mathf.Cos(HeldLean * Mathf.Deg2Rad));

        private (float x, float y, float w) Groove(int groove, int capacity)
        {
            float m = DesignTokens.ScreenMargin;
            int cap = Mathf.Max(1, capacity);
            float cw = Mathf.Min(DesignTokens.BufferCellWidth, Mathf.Floor((DesignTokens.BufferInnerWidth - 8f * (cap - 1)) / cap));
            float step = cap > 1 ? cw + Mathf.Floor((DesignTokens.BufferInnerWidth - cw * cap) / (cap - 1)) : 0f;
            return (m + DesignTokens.BufferInset + Mathf.Clamp(groove, 0, cap - 1) * step, DesignTokens.BufferTop + DesignTokens.BufferCellTop, cw);
        }

        private (Vector3 pos, Quaternion rot) HeldPose(int groove, int capacity)
        {
            var c = Groove(groove, capacity);
            return (_board.TransformPoint(HeldLocal(c.x + c.w / 2f, c.y + DesignTokens.BufferCellHeight / 2f)), _board.rotation * HeldTurn);
        }

        private (Vector3 pos, Quaternion rot) PegCardPose(int slot, int slots, int index)
        {
            var rot = _board.rotation * Quaternion.Euler(PegLean, 0f, 0f);
            var root = _board.TransformPoint(P(PegX(slot, slots), PegBaseY, 60f));
            return (root + rot * new Vector3(0f, 0f, -PegBaseT - index * (ThinT + ThinGap)), rot);
        }

        private Renderer Find(string parent, string child)
        {
            foreach (var go in _dynamic)
            {
                if (go == null || go.name != parent) continue;
                if (child == null) return go.GetComponent<Renderer>();
                var t = go.transform.Find(child);
                return t != null ? t.GetComponent<Renderer>() : null;
            }
            return null;
        }

        private void Launch(BoardVisual v, IReadOnlyList<CardFlight> flights, List<(Vector3 pos, Quaternion rot)> from, Dictionary<int, List<PegExit>> chains)
        {
            float now = Time.time;
            var starts = new float[flights.Count];
            var queued = new Dictionary<(int slot, int gen), int>();   // cards already waiting for the same peg
            for (int i = 0; i < flights.Count; i++)
            {
                var f = flights[i];
                float start = now + i * FlightInterval;
                if (f.After >= 0 && f.After < i) start = Mathf.Max(start, starts[f.After] + FlightTime + 0.02f);
                chains.TryGetValue(f.ToSlot, out var chain);
                if (f.SwapGen > 0 && chain != null && f.SwapGen - 1 < chain.Count)
                {
                    // a peg that replaced a full one takes cards only once that one has left and it has moved up
                    queued.TryGetValue((f.ToSlot, f.SwapGen), out int k);
                    queued[(f.ToSlot, f.SwapGen)] = k + 1;
                    start = Mathf.Max(start, chain[f.SwapGen - 1].Arrive + k * FlightInterval);
                }
                starts[i] = start;
                var g = new Ghost { Start = start, S0 = Vector3.one };
                if (f.Completes && chain != null && f.SwapGen < chain.Count)
                {
                    g.Ride = chain[f.SwapGen];
                    float leave = start + FlightTime + PegLinger;
                    g.Ride.Leave = g.Ride.Leave == float.MaxValue ? leave : Mathf.Max(g.Ride.Leave, leave);
                }
                g.P0 = from[i].pos; g.R0 = from[i].rot;
                if (f.FromHeld >= 0) { g.S0 = Vector3.one * HeldScale; g.ShowWhileWaiting = true; }
                if (f.ToSlot >= 0)
                {
                    var d = PegCardPose(f.ToSlot, v.Targets.Length, f.ToIndex);
                    g.P1 = d.pos; g.R1 = d.rot; g.S1 = Vector3.one;
                    if (!f.Completes) g.Reveal = Find($"Peg{f.ToSlot}", $"Card{f.ToIndex}");
                }
                else
                {
                    var d = HeldPose(f.ToHeld, v.BufferCapacity);
                    g.P1 = d.pos; g.R1 = d.rot; g.S1 = Vector3.one * HeldScale;
                    if (f.ToHeldFinal >= 0) g.Reveal = Find($"Held{f.ToHeldFinal}", null);
                }
                if (g.Reveal != null) g.Reveal.enabled = false;
                var go = new GameObject("Flight");
                SetLayer(go);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _thinMesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterials = new[] { _cardTop[f.Color], _cardSide[f.Color] };
                go.SetActive(false);
                g.Go = go;
                _ghosts.Add(g);
            }
        }

        private void Update()
        {
            float now = Time.time;
            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                var g = _ghosts[i];
                if (g.Go == null) { _ghosts.RemoveAt(i); continue; }
                float t = (now - g.Start) / FlightTime;
                if (t < 0f)
                {
                    if (g.ShowWhileWaiting && !g.Go.activeSelf) { g.Go.SetActive(true); g.Go.transform.SetPositionAndRotation(g.P0, g.R0); g.Go.transform.localScale = g.S0; }
                    continue;
                }
                g.Go.SetActive(true);
                var tr = g.Go.transform;
                if (t < 1f)
                {
                    float e = 1f - (1f - t) * (1f - t);                       // ease out
                    tr.position = Vector3.Lerp(g.P0, g.P1, e) + Vector3.back * (FlightArc * 4f * t * (1f - t));
                    tr.rotation = Quaternion.Slerp(g.R0, g.R1, e);
                    tr.localScale = Vector3.Lerp(g.S0, g.S1, e);
                    continue;
                }
                if (g.Reveal != null) g.Reveal.enabled = true;
                if (g.Ride != null && g.Ride.Old != null)
                {
                    // landed on a peg that is about to leave: ride along with it
                    tr.position = g.P1; tr.rotation = g.R1; tr.localScale = g.S1;
                    tr.SetParent(g.Ride.Old, true);
                }
                else Destroy(g.Go);
                _ghosts.RemoveAt(i);
            }
            UpdateExits(now);
        }

        // a full peg lingers, sinks away, the peg behind moves up, and the next one appears behind it
        private void UpdateExits(float now)
        {
            for (int i = _exits.Count - 1; i >= 0; i--)
            {
                var e = _exits[i];
                if (now < e.Leave) continue;
                if (e.Old != null)
                {
                    float t = (now - e.Leave) / PegLeaveTime;
                    if (t < 1f) { e.Old.localScale = Vector3.one * (1f - t * t); continue; }
                    Destroy(e.Old.gameObject);
                    e.Old = null;
                    e.Enter = now;
                }
                float u = Mathf.Clamp01((now - e.Enter) / PegEnterTime), k = 1f - (1f - u) * (1f - u);
                if (e.New != null)
                {
                    e.New.transform.localPosition = Vector3.Lerp(e.Back, e.Front, k);
                    e.New.transform.localScale = Vector3.one * Mathf.Lerp(BackRowScale, 1f, k);
                }
                if (u < 1f) continue;
                float w = (now - e.Enter - PegEnterTime) / BackAppearTime;
                if (e.AfterNew != null) { e.AfterNew.SetActive(true); e.AfterNew.transform.localScale = Vector3.one * BackRowScale * Mathf.Clamp01(w); }
                if (w >= 1f) _exits.RemoveAt(i);
            }
        }

        /// <summary>Screen x of each target peg (the label row in <see cref="BoardView"/> uses the same columns).</summary>
        public static float PegX(int slot, int slots)
        {
            float w = slots == 2 ? DesignTokens.TargetSlotWidthTwo : DesignTokens.TargetSlotWidth;
            float gap = slots == 2 ? DesignTokens.TargetSlotGapTwo : DesignTokens.TargetSlotGap;
            return 540f - (slots * w + (slots - 1) * gap) / 2f + slot * (w + gap) + w / 2f;
        }

        // a waiting peg is drawn as tall as the front pegs (every shipped target holds the same 18)
        private static int BackCapacity(BoardVisual v)
        {
            int c = 0;
            foreach (var t in v.Targets) c = Mathf.Max(c, t.Capacity);
            return c > 0 ? c : 18;
        }

        private void DrawTargets(BoardVisual v)
        {
            int n = v.Targets.Length;
            // the queue waits behind the front row, nearest first in the middle of the row (CR-007)
            for (int u = 0; u < v.Upcoming.Length && u < n; u++)
                if (v.Upcoming[u] >= 0) Peg($"NextPeg{u}", PegX(u, n), PegBaseY - BackRowRise, v.Upcoming[u], 0, BackCapacity(v), BackRowScale, 30f);
            for (int i = 0; i < n; i++)
            {
                var t = v.Targets[i];
                if (t.Has) Peg($"Peg{i}", PegX(i, n), PegBaseY, t.Color, t.Filled, t.Capacity, 1f, 60f);   // a finished column shows nothing (Phat)
            }
        }

        // a peg: coloured base, pole, and the cards threaded on it (colour -1 = an empty slot)
        private Transform Peg(string name, float cx, float baseY, int color, int filled, int capacity, float scale, float height)
        {
            var root = new GameObject(name).transform;
            SetLayer(root.gameObject);
            root.SetParent(_board, false);
            root.localPosition = P(cx, baseY, height);
            root.localRotation = Quaternion.Euler(PegLean, 0f, 0f);
            root.localScale = Vector3.one * scale;
            _dynamic.Add(root.gameObject);
            var baseMat = color >= 0 ? _pedestal[color] : _pegEmpty;
            Piece("Base", _pegBaseMesh, baseMat, baseMat, Vector3.zero, 0f, root);
            if (color < 0) return root;
            Piece("Pole", PoleMesh(capacity), _pole, _pole, new Vector3(0f, 0f, -PegBaseT), 0f, root);
            for (int j = 0; j < filled; j++)
                Piece($"Card{j}", _thinMesh, _cardTop[color], _cardSide[color], new Vector3(0f, 0f, -PegBaseT - j * (ThinT + ThinGap)), 0f, root);
            return root;
        }

        private void DrawBuffer(BoardVisual v)
        {
            float m = DesignTokens.ScreenMargin;
            Slab("Rail", m, DesignTokens.BufferTop, 1080f - 2f * m, DesignTokens.BufferHeight, 30f, 48f, _rail, 4f);   // never turns red (Phat)
            int cap = Mathf.Max(1, v.BufferCapacity);
            float inner = DesignTokens.BufferInnerWidth;
            float cw = Mathf.Min(DesignTokens.BufferCellWidth, Mathf.Floor((inner - 8f * (cap - 1)) / cap));
            float step = cap > 1 ? cw + Mathf.Floor((inner - cw * cap) / (cap - 1)) : 0f;
            for (int i = 0; i < cap; i++)
            {
                float x = m + DesignTokens.BufferInset + i * step;
                float y = DesignTokens.BufferTop + DesignTokens.BufferCellTop;
                Slab($"Cell{i}", x, y, cw, DesignTokens.BufferCellHeight, 4f, 12f, _cell, 34f);
            }
            // one card per groove, lying along it; a card keeps its groove until it leaves (no re-sorting, Phat)
            for (int i = 0; i < v.Buffer.Length; i++)
            {
                int groove = v.BufferGrooves != null && i < v.BufferGrooves.Length ? v.BufferGrooves[i] : i;
                var c = Groove(groove, v.BufferCapacity);
                var held = Piece($"Held{groove}", _thinMesh, _cardTop[v.Buffer[i]], _cardSide[v.Buffer[i]], HeldLocal(c.x + c.w / 2f, c.y + DesignTokens.BufferCellHeight / 2f));
                held.transform.localRotation = HeldTurn;
                held.transform.localScale = Vector3.one * HeldScale;
            }
        }

        private void DrawTray(BoardVisual v)
        {
            float inset = DesignTokens.TrayRimInset, pad = DesignTokens.TrayPadding;
            // CR-011: the tray fits the level's layout, centred under the holding area (the reference, video IMG_3750)
            float left = float.MaxValue, topY = float.MaxValue, right = 0f, bottom = 0f;
            foreach (var st in v.Stacks)
            {
                int cards = st.Colors != null ? st.Colors.Length : 0;
                left = Mathf.Min(left, st.X); topY = Mathf.Min(topY, st.Y);
                right = Mathf.Max(right, st.X + st.W + Mathf.Max(0, cards - 1) * PileLean);   // cards lean to the right
                bottom = Mathf.Max(bottom, st.Y + st.H);
            }
            if (v.Stacks.Length == 0) { left = topY = 0f; right = CardW; bottom = CardL; }
            // a tall stack reads higher on screen (the board is tilted toward the camera): keep room above for it
            float tallest = 0f;
            foreach (var st in v.Stacks) tallest = Mathf.Max(tallest, 22f + st.Layer * 60f + (st.Colors != null ? st.Colors.Length : 0) * (ThinT + ThinGap));
            float lift = tallest * Mathf.Sin(_tilt * Mathf.Deg2Rad);
            float contentW = right - left, contentH = bottom - topY;
            float trayW = Mathf.Clamp(contentW + 2f * (pad + inset), DesignTokens.TrayMinWidth, 1080f - 2f * DesignTokens.ScreenMargin);
            float trayH = Mathf.Clamp(contentH + lift + 2f * (pad + inset), DesignTokens.TrayMinHeight, DesignTokens.TrayHeight);
            float tx = 540f - trayW / 2f, ty = DesignTokens.TrayTop;
            // CR-007: a flat tray — a low rim and a plate barely above it (the reference has no wooden box)
            Slab("TrayRim", tx, ty, trayW, trayH, 12f, 48f, _rim, 4f);
            Slab("TrayInner", tx + inset, ty + inset, trayW - 2f * inset, trayH - 2f * inset, 6f, 34f, _inner, 12f);
            float ox = Mathf.Round(540f - contentW / 2f - left), oy = Mathf.Round(ty + (trayH + lift) / 2f - contentH / 2f - topY);   // centred below the room kept for tall stacks
            for (int i = 0; i < v.Stacks.Length; i++)
            {
                var s = v.Stacks[i];
                if (s.Colors == null || s.Colors.Length == 0) continue;
                int n = s.Colors.Length;                       // cards, bottom (b = 0) to top (b = n - 1)
                float pitch = ThinT + ThinGap;
                float baseHeight = 22f + s.Layer * 60f;     // higher layers physically sit above lower ones
                float step = n > 1 ? Mathf.Min(FanMaxStep, FanMaxSpread / (n - 1)) : 0f;
                var list = new List<Renderer>();
                _stackRenderers[i] = list;
                for (int b = 0; b < n; b++)
                {
                    int d = n - 1 - b;                         // depth from the top (0 = top card)
                    int k = s.Colors[d];                       // CR-007: covered stacks keep their colour
                    GameObject card;
                    if (s.Fan)
                    {
                        // a hand of cards: each turns around a pivot at the bottom-centre of the stack's rect,
                        // the top card at the right end of the arc
                        float a = (-0.5f * step * (n - 1) + b * step) * Mathf.Deg2Rad;
                        float px = ox + s.X + s.W / 2f, py = oy + s.Y + s.H - 30f;
                        float r = CardFaceH / 2f + 20f;
                        card = Piece($"Stack{i}Card{d}", _thinMesh, _cardTop[k], _cardSide[k],
                            P(px + r * Mathf.Sin(a), py - r * Mathf.Cos(a), baseHeight + b * pitch), -a * Mathf.Rad2Deg);
                    }
                    else
                    {
                        // each card a little to the right of the one below (the reference)
                        float shift = b * PileLean;
                        card = Piece($"Stack{i}Card{d}", _thinMesh, _cardTop[k], _cardSide[k], P(ox + s.X + CardW / 2f + shift, oy + s.Y + CardL / 2f, baseHeight + b * pitch));
                    }
                    list.Add(card.GetComponent<Renderer>());
                    if (s.Hint && b == n - 1)
                    {
                        // a bright rim around the top card: a slightly bigger plate just under its face
                        var rim = Piece($"Stack{i}Hint", _hintMesh, _hint, _hint, card.transform.localPosition + new Vector3(0f, 0f, -ThinT + 4.5f)   /* its face 1.5 under the card face */, card.transform.localEulerAngles.z);
                        _hints.Add(rim.GetComponent<Renderer>());
                    }
                }
            }
        }
    }
}
