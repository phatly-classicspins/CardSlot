using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Domain;

namespace Game.Presentation
{
    /// <summary>Where level data comes from (the shipped files behind Addressables). A port, so the
    /// controller never sees the storage; the adapter lives in Game.Infrastructure.</summary>
    public interface ILevelSource
    {
        /// <summary>Number of shipped levels.</summary>
        UniTask<int> CountAsync(CancellationToken ct);
        /// <summary>The 1-based level, validated (R-1, R-1b); throws if the shipped file is broken —
        /// <c>ShippedLevelTests</c> keeps that from reaching a build.</summary>
        UniTask<LevelData> LoadAsync(int level, CancellationToken ct);
    }
}
