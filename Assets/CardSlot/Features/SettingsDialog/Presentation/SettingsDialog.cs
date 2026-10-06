using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Domain;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Application;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    public sealed record SettingsArgs(string Title, string Sound, string Music, string Haptics, string On, string Off) : DialogArgs;

    /// <summary>features/pause-settings.md — Sound / Music / Haptics, saved immediately on each toggle.</summary>
    public sealed class SettingsDialog : DialogBase<Unit>
    {
        private readonly IProgressStore _store;
        private SettingsDialogView _view;
        private SettingsArgs _args;

        public SettingsDialog(IAssetService assets, IProgressStore store) : base(assets) { _store = store; }

        public override async UniTask OnCreateAsync(DialogArgs args, CancellationToken ct)
        {
            _args = (SettingsArgs)args;
            _view = await LoadViewAsync<SettingsDialogView>(AssetKeys.SettingsDialog, ct);
            _view.Toggled += OnToggled;
            _view.ClosePressed += () => Close(Unit.Default);
            _view.CloseRequested += () => Close(Unit.Default);
            Render();
        }

        private void OnToggled(int row)
        {
            var s = _store.Settings;
            if (row == 0) s.Sound = !s.Sound;
            else if (row == 1) s.Music = !s.Music;
            else s.Haptics = !s.Haptics;
            try { _store.Save(); } catch (System.Exception) { /* a preference: keep the in-memory value, retry on the next save */ }
            Render();
        }

        private void Render()
        {
            var s = _store.Settings;
            _view.Show(_args.Title, new[] { _args.Sound, _args.Music, _args.Haptics }, new[] { s.Sound, s.Music, s.Haptics }, _args.On, _args.Off);
        }
    }
}
