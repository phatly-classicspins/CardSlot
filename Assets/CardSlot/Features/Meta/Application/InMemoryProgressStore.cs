namespace Game.Application
{
    /// <summary>
    /// Session-only progress (milestone 6a: play the 30 levels in the Editor). Nothing reaches disk; 6b
    /// replaces this registration with the adapter over the framework's <c>IUserData</c>.
    /// </summary>
    public sealed class InMemoryProgressStore : IProgressStore
    {
        public ProgressModel Progress { get; } = new ProgressModel();
        public SettingsModel Settings { get; } = new SettingsModel();
        public void Save() { }
    }
}
