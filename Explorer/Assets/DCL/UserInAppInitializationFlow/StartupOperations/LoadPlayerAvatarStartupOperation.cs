using Arch.Core;
using Cysharp.Threading.Tasks;
using DCL.AsyncLoadReporting;
using DCL.AvatarRendering.AvatarShape.UnityInterface;
using DCL.Profiles;
using DCL.Profiles.Self;
using DCL.Utilities;
using System.Threading;
using Utility.Types;

namespace DCL.UserInAppInitializationFlow.StartupOperations
{
    /// <summary>
    ///     Resolves Player profile and waits for the avatar to be loaded
    /// </summary>
    public class LoadPlayerAvatarStartupOperation : StartUpOperationBase
    {
        private readonly ILoadingStatus loadingStatus;
        private readonly ISelfProfile selfProfile;
        private readonly ObjectProxy<AvatarBase> mainPlayerAvatarBaseProxy;
        private World world = null!;
        private Entity playerEntity;

        public LoadPlayerAvatarStartupOperation(ILoadingStatus loadingStatus, ISelfProfile selfProfile, ObjectProxy<AvatarBase> mainPlayerAvatarBaseProxy)
        {
            this.loadingStatus = loadingStatus;
            this.selfProfile = selfProfile;
            this.mainPlayerAvatarBaseProxy = mainPlayerAvatarBaseProxy;
        }

        public void AssignWorld(World newWorld, Entity newPlayerEntity)
        {
            world = newWorld;
            playerEntity = newPlayerEntity;
        }

        protected override async UniTask InternalExecuteAsync(AsyncLoadProcessReport report, CancellationToken ct)
        {
            float finalizationProgress = loadingStatus.SetCurrentStage(LoadingStatus.LoadingStage.PlayerAvatarLoading);
            var profile = await selfProfile.ProfileOrPublishIfNotAsync(ct);

            // Add the profile into the player entity so it will create the avatar in world

            if (world.Has<Profile>(playerEntity))
                world.Set(playerEntity, profile);
            else
                world.Add(playerEntity, profile);

            // Eventually it will lead to the Avatar Resolution or the entity destruction
            // if the avatar is already downloaded by the authentication screen it will be resolved immediately
            await UniTask.WaitWhile(() => !mainPlayerAvatarBaseProxy.Configured && world.IsAlive(playerEntity), PlayerLoopTiming.LastPostLateUpdate, ct);
            report.SetProgress(finalizationProgress);
        }
    }
}
