using System;
using TMPro;
using UnityEngine;

namespace Game.Views
{
    /// <summary>
    /// Home (mock-up <c>docs/mockups/home-v01.png</c>, no coins since CR-005): the live-text logo (the commercial name is not
    /// final, D-012), a fan of the six card colours and the Play button. Renders given labels; relays Play.
    /// </summary>
    public sealed class HomeView : MonoBehaviour
    {
        [SerializeField] private Sprite[] _cardFace = new Sprite[6];
        [SerializeField] private Sprite _ground;
        [SerializeField] private Sprite _buttonPrimary;
        [SerializeField] private TMP_FontAsset _font;

        public event Action PlayPressed;

        private RectTransform _root;
        private TextMeshProUGUI _title, _tagline, _play, _note;

        private float Tall => _root != null ? Mathf.Max(0f, _root.rect.height - 1920f) : 0f;

        private void EnsureBuilt()
        {
            if (_root != null) return;
            _root = UiKit.Stretch("Root", transform);
            UiKit.Fill("Ground", _root, Color.white, false).sprite = _ground;
            float tall = Tall;
            _title = UiKit.Text("Title", _root, _font, string.Empty, DesignTokens.TypeLogo, DesignTokens.Secondary, 0f, 330f + tall / 6f, 1080f, 200f);
            _tagline = UiKit.Text("Tagline", _root, _font, string.Empty, 42f, DesignTokens.InkSoft, 0f, 530f + tall / 6f, 1080f, 60f);
            // mock-up: cards rotate about a point 520 px below their top edge, 12° apart
            for (int i = 0; i < 6; i++)
            {
                var arm = UiKit.Node($"Arm{i}", _root, 540f, 760f + tall / 3f + 520f, 0f, 0f);
                arm.localRotation = Quaternion.Euler(0f, 0f, -(i - 2.5f) * 12f);
                UiKit.Image($"Card{i}", arm, _cardFace[i], -90f, -520f, 180f, 260f);
            }
            var play = UiKit.Image("Play", _root, _buttonPrimary, 540f - 320f, 1360f + tall * 0.6f, 640f, 204f, sliced: true);
            _play = UiKit.Text("Label", play.transform, _font, string.Empty, DesignTokens.TypeDisplay, DesignTokens.OnColor, 0f, 0f, 640f, 190f);
            UiKit.Button(play, () => PlayPressed?.Invoke());
            _note = UiKit.Text("Note", _root, _font, string.Empty, 34f, DesignTokens.InkSoft, 0f, 1590f + tall * 0.6f, 1080f, 50f);
        }

        /// <param name="note">Line under Play (e.g. "More levels coming soon"); null hides it.</param>
        public void Show(string title, string tagline, string playLabel, string note)
        {
            EnsureBuilt();
            _title.SetText(title ?? string.Empty);
            _tagline.SetText(tagline ?? string.Empty);
            _play.SetText(playLabel ?? string.Empty);
            _note.gameObject.SetActive(note != null);
            _note.SetText(note ?? string.Empty);
        }
    }
}
