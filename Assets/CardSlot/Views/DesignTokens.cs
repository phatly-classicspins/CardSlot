using UnityEngine;

namespace Game.Views
{
    /// <summary>
    /// The SKU's single source of visual truth (rule #17, G21) — a transcription of
    /// <c>docs/design/design-tokens.md</c> v1.0 (direction B · Warm Toybox, D-017) and the motion tokens of
    /// <c>docs/design/animation-list.md</c> (D-022). Views index it; nothing redeclares a colour, size or
    /// duration. Units: 1 reference px = 1 world unit on the 1080×1920 rig.
    /// </summary>
    public static class DesignTokens
    {
        public static Color Hex(string rrggbb, float alpha = 1f)
        {
            if (!ColorUtility.TryParseHtmlString("#" + rrggbb, out var c)) c = Color.magenta;
            c.a = alpha;
            return c;
        }

        // ── Palette (§1) ─────────────────────────────────────────────────────────────────────────────
        public static readonly Color GroundTop = Hex("FFF6E6");
        public static readonly Color GroundBottom = Hex("FFD9A8");
        public static readonly Color Surface = Hex("FFF8EC");
        public static readonly Color SurfaceSunken = Hex("F2C68C");
        public static readonly Color TrayRim = Hex("B9733A");
        public static readonly Color TrayInner = Hex("F6CF98");
        // CR-007: the target pegs (a warm grey-brown that reads against every card colour)
        public static readonly Color PegPole = Hex("8E7B6C");
        public static readonly Color Rail = Hex("C98B52");
        public static readonly Color Ink = Hex("5A3415");
        public static readonly Color InkSoft = Hex("9A6A3F");
        public static readonly Color Primary = Hex("21B8A6");
        public static readonly Color PrimaryEdge = Hex("138A7B");
        public static readonly Color Secondary = Hex("E8613C");
        public static readonly Color SecondaryEdge = Hex("B7411F");
        public static readonly Color Danger = Hex("E2604A");
        public static readonly Color DangerText = Hex("D2402A");
        public static readonly Color Disabled = Hex("CDBFAE");
        public static readonly Color Scrim = Hex("462308", 0.55f);
        public static readonly Color TutorialScrim = Hex("462308", 0.45f);
        public static readonly Color OnColor = Color.white;
        // CR-007: the outline on a stack that can be tapped
        public static readonly Color HintOutline = Hex("FFF6C2");
        public static readonly Color TextShadow = new Color(0f, 0f, 0f, 0.25f);

        // ── Card colours (§2): glossary color_0..7 (CR-012: 8 colours) ──────────────────────────────────
        public static readonly Color[] CardFace =
            { Hex("F2475B"), Hex("3D8BFF"), Hex("FFC93C"), Hex("3CC26B"), Hex("9B5CF6"), Hex("FF8A3D"), Hex("FF6FB5"), Hex("2FCFE0") };
        /// <summary>Covered cards: 68% brightness tint + hatch overlay (§2).</summary>
        public static readonly Color CoveredTint = new Color(0.68f, 0.68f, 0.68f, 1f);

        // ── Type (§3) — sizes in reference px ────────────────────────────────────────────────────────
        public const float TypeLogo = 150f, TypeDisplay = 84f, TypeTitleMax = 76f, TypeButton = 66f, TypeHeading = 54f,
            TypeBody = 46f, TypeHud = 44f, TypeCount = 40f, TypeLabel = 30f;
        /// <summary>Ribbon titles shrink to fit (GDD §10, CR-001): min(76, 1500 / characters).</summary>
        public static float TitleSize(int characters) => Mathf.Min(TypeTitleMax, 1500f / Mathf.Max(1, characters));

        // ── Spacing / layout (§4, §5) ────────────────────────────────────────────────────────────────
        public const float ScreenMargin = 40f;
        // Reference layouts: expose each card edge, and open a six-card fan through 60 degrees.
        public const float ReferencePileSpacing = 18f, ReferenceFanStep = 12f, ReferenceFanSpread = 60f;
        public const float HudTop = 66f, HudHeight = 104f, RoundButton = 104f;
        public const float TargetsTop = 200f, TargetsHeight = 400f, TargetSlotWidth = 300f, TargetSlotGap = 40f,
            TargetSlotWidthTwo = 470f, TargetSlotGapTwo = 60f, TargetCardTop = 120f, TargetCardStep = 34f;
        public const float BufferLabelTop = 606f, BufferTop = 650f, BufferHeight = 200f, BufferCellTop = 30f,
            BufferCellWidth = 66f, BufferCellHeight = 140f, BufferInnerWidth = 960f, BufferInset = 34f;
        // CR-005: no booster bar — the tray runs down to the bottom margin; a level smaller than it is centred
        public const float TrayTop = 900f, TrayHeight = 960f, TrayRimInset = 22f;
        // CR-011: the tray is sized to the level (padding around the stacks, never smaller than these)
        public const float TrayPadding = 36f, TrayMinWidth = 560f, TrayMinHeight = 420f;
        public const float CardWidth = 150f, CardHeight = 216f, CardBody = 206f, CardUnderHeight = 214f, CardStep = 16f;
        public const float PanelSideInset = 90f, RibbonOverhang = 30f, RibbonRise = 70f, RibbonHeight = 184f;

        // ── Motion (animation-list.md, seconds) ──────────────────────────────────────────────────────
        public static class Motion
        {
            public const float Fly = 0.32f, FlyStagger = 0.05f, FlyArc = 120f, FlyTilt = 8f, ToBuffer = 0.28f, Release = 0.32f,
                Compact = 0.15f, LandBump = 0.12f, LandBumpScale = 0.9f, Complete = 0.35f, CompleteOvershoot = 1.15f,
                Enter = 0.25f, EnterOffset = 120f, Reveal = 0.18f, Press = 0.08f, PressScale = 0.94f, Shake = 0.25f,
                ShakeAmp = 10f, WarnPeriod = 0.8f, Overflow = 0.4f, Expand = 0.35f, Rewind = 0.25f, Dialog = 0.2f,
                DialogFrom = 0.9f, BtnPress = 0.08f, BtnRelease = 0.15f, BtnScale = 0.95f, CountUp = 1.2f, Confetti = 1.2f,
                HandPeriod = 1.0f, UnlockPop = 0.4f, UnlockGiftDelay = 0.15f, RevealDelay = 1.0f, RevealDur = 0.25f;
            public const int ConfettiCount = 40, BurstCount = 16;
        }
    }
}
