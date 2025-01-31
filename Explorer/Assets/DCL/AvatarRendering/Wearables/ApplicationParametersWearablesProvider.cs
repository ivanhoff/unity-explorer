using Arch.Core;
using CommunicationData.URLHelpers;
using Cysharp.Threading.Tasks;
using DCL.AvatarRendering.Loading.Components;
using DCL.AvatarRendering.Wearables.Components;
using DCL.AvatarRendering.Wearables.Helpers;
using ECS.Prioritization.Components;
using ECS.StreamableLoading.Common;
using Global.AppArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WearablePromise = ECS.StreamableLoading.Common.AssetPromise<DCL.AvatarRendering.Wearables.Components.WearablesResolution,
    DCL.AvatarRendering.Wearables.Components.Intentions.GetWearablesByPointersIntention>;

namespace DCL.AvatarRendering.Wearables
{
    public class ApplicationParametersWearablesProvider : IWearablesProvider
    {
        private readonly IAppArgs appArgs;
        private readonly IWearablesProvider source;
        private readonly World world;
        private readonly string[] allWearableCategories = WearablesConstants.CATEGORIES_PRIORITY.ToArray();
        private readonly List<IWearable> resultWearablesBuffer = new ();

        public ApplicationParametersWearablesProvider(IAppArgs appArgs,
            IWearablesProvider source,
            World world)
        {
            this.appArgs = appArgs;
            this.source = source;
            this.world = world;
        }

        public async UniTask<(IReadOnlyList<IWearable> results, int totalAmount)> GetAsync(int pageSize, int pageNumber, CancellationToken ct,
            IWearablesProvider.SortingField sortingField = IWearablesProvider.SortingField.Date,
            IWearablesProvider.OrderBy orderBy = IWearablesProvider.OrderBy.Descending,
            string? category = null,
            IWearablesProvider.CollectionType collectionType = IWearablesProvider.CollectionType.All,
            string? name = null,
            List<IWearable>? results = null)
        {
            if (!appArgs.TryGetValue(AppArgsFlags.SELF_PREVIEW_WEARABLES, out string? wearablesCsv))
                return await source.GetAsync(pageSize, pageNumber, ct, sortingField, orderBy, category, collectionType, name, results);

            URN[] pointers = wearablesCsv!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(s => new URN(s))
                                          .ToArray();

            (IReadOnlyCollection<IWearable>? maleWearables, IReadOnlyCollection<IWearable>? femaleWearables) =
                await UniTask.WhenAll(RequestPointersAsync(pointers, BodyShape.MALE, ct),
                    RequestPointersAsync(pointers, BodyShape.FEMALE, ct));

            results ??= new List<IWearable>();

            lock (resultWearablesBuffer)
            {
                resultWearablesBuffer.Clear();

                if (maleWearables != null)
                    resultWearablesBuffer.AddRange(maleWearables);

                if (femaleWearables != null)
                    resultWearablesBuffer.AddRange(femaleWearables);

                int pageIndex = pageNumber - 1;
                results.AddRange(resultWearablesBuffer.Skip(pageIndex * pageSize).Take(pageSize));
                return (results, resultWearablesBuffer.Count);
            }
        }

        private async UniTask<IReadOnlyCollection<IWearable>?> RequestPointersAsync(IReadOnlyCollection<URN> pointers,
            BodyShape bodyShape,
            CancellationToken ct)
        {
            var promise = WearablePromise.Create(world,

                // We pass all categories as force renderer to force the download of all of them
                // Otherwise they will be skipped if any wearable is hiding the category
                WearableComponentsUtils.CreateGetWearablesByPointersIntention(bodyShape, pointers, allWearableCategories),
                PartitionComponent.TOP_PRIORITY);

            promise = await promise.ToUniTaskAsync(world, cancellationToken: ct);

            if (!promise.TryGetResult(world, out var result))
                return null;

            if (!result.Succeeded)
                return null;

            return result.Asset.Wearables;
        }
    }
}
