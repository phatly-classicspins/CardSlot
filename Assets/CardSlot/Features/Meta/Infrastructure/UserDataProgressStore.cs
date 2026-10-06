using ClassicSpins.PrototypeFramework.Application;
using Game.Application;

namespace Game.Infrastructure
{
    /// <summary>
    /// <see cref="IProgressStore"/> over the framework's <see cref="IUserData"/> envelope (D-023): the models
    /// are the live instances the save loaded (registered as <c>IUserModel</c> defaults at Root, loaded by the
    /// framework's UserDataLoad boot node). <see cref="Save"/> requests the framework's debounced write,
    /// flushed at the pause/quit safe point.
    /// </summary>
    public sealed class UserDataProgressStore : IProgressStore
    {
        private readonly IUserData _userData;

        public UserDataProgressStore(IUserData userData) { _userData = userData; }

        public ProgressModel Progress => _userData.Get<ProgressModel>();
        public SettingsModel Settings => _userData.Get<SettingsModel>();
        public void Save() => _userData.Save();
    }
}
