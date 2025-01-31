using Cysharp.Threading.Tasks;
using DCL.Character;
using DCL.Diagnostics;
using DCL.Multiplayer.Connections.Archipelago.AdapterAddress.Current;
using DCL.Multiplayer.Connections.Archipelago.LiveConnections;
using DCL.Multiplayer.Connections.Archipelago.SignFlow;
using DCL.Multiplayer.Connections.Rooms.Connective;
using DCL.Multiplayer.Connections.Typing;
using DCL.Web3.Identities;
using LiveKit.Internal.FFIClients.Pools;
using LiveKit.Internal.FFIClients.Pools.Memory;
using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using UnityEngine;
using Utility.Multithreading;

namespace DCL.Multiplayer.Connections.Archipelago.Rooms
{
    public class ArchipelagoIslandRoom : ConnectiveRoom
    {
        private readonly IWeb3IdentityCache web3IdentityCache;
        private readonly IArchipelagoSignFlow signFlow;
        private readonly ICharacterObject characterObject;

        private readonly ICurrentAdapterAddress currentAdapterAddress;

        private string? newConnectionString;

        public ArchipelagoIslandRoom(ICharacterObject characterObject, IWeb3IdentityCache web3IdentityCache, IMultiPool multiPool, ICurrentAdapterAddress currentAdapterAddress) : this(
            web3IdentityCache,
            // We cannot use ArrayPool<byte>.Shared since some operations might not be thread safe (like the handshake)
            // producing unexpected errors when sending the data through the websocket
            new LiveConnectionArchipelagoSignFlow(
                new WebSocketArchipelagoLiveConnection(
                    () => new ClientWebSocket(),
                    new ArrayMemoryPool(ArrayPool<byte>.Create())
                ).WithLog(),
                new ArrayMemoryPool(ArrayPool<byte>.Create()),
                multiPool
            ).WithLog(),
            characterObject,
            currentAdapterAddress
        ) { }

        public ArchipelagoIslandRoom(
            IWeb3IdentityCache web3IdentityCache,
            IArchipelagoSignFlow signFlow,
            ICharacterObject characterObject,
            ICurrentAdapterAddress currentAdapterAddress
        )
        {
            this.web3IdentityCache = web3IdentityCache;
            this.signFlow = signFlow;
            this.characterObject = characterObject;
            this.currentAdapterAddress = currentAdapterAddress;
        }

        protected override async UniTask PrewarmAsync(CancellationToken token)
        {
            await ConnectToArchipelagoAsync(token);
            signFlow.StartListeningForConnectionStringAsync(OnNewConnectionString, token).Forget();
        }

        protected override async UniTask CycleStepAsync(CancellationToken token)
        {
            if (newConnectionString != null)
            {
                string connectionString = newConnectionString;
                newConnectionString = null;

                await TryConnectToRoomAsync(connectionString, token);
            }

            await UniTask.SwitchToMainThread(token);
            Vector3 position = characterObject.Position;
            await using ExecuteOnThreadPoolScope _ = await ExecuteOnThreadPoolScope.NewScopeWithReturnOnMainThreadAsync();

            var result = await signFlow.SendHeartbeatAsync(position, token);

            if (result.Success == false)
                ReportHub.LogWarning(ReportCategory.COMMS_SCENE_HANDLER, $"Cannot send heartbeat, connection is closed: {result.ErrorMessage}");
        }

        private void OnNewConnectionString(string connectionString)
        {
            newConnectionString = connectionString;
        }

        private async UniTask ConnectToArchipelagoAsync(CancellationToken token)
        {
            string adapterUrl = currentAdapterAddress.AdapterUrl();
            LightResult<string> welcomePeerId = await WelcomePeerIdAsync(adapterUrl, token);
            welcomePeerId.EnsureSuccess("Cannot authorize with current address and signature, peer id is invalid");
        }

        private async UniTask<LightResult<string>> WelcomePeerIdAsync(string adapterUrl, CancellationToken token)
        {
            await using ExecuteOnThreadPoolScope _ = await ExecuteOnThreadPoolScope.NewScopeWithReturnOnMainThreadAsync();
            IWeb3Identity identity = web3IdentityCache.EnsuredIdentity();
            await signFlow.EnsureConnectedAsync(adapterUrl, token);
            string ethereumAddress = identity.Address;
            var messageForSignResult = await signFlow.MessageForSignAsync(ethereumAddress, token);

            if (messageForSignResult.Success == false ||
                !HandshakePayloadIsValid(messageForSignResult.Result))
            {
                ReportHub.LogError(ReportCategory.COMMS_SCENE_HANDLER, "Cannot obtain a message to sign a welcome peer");
                return LightResult<string>.FAILURE;
            }

            string signedMessage = identity.Sign(messageForSignResult.Result).ToJson();
            ReportHub.Log(ReportCategory.COMMS_SCENE_HANDLER, $"Signed message: {signedMessage}");
            return await signFlow.WelcomePeerIdAsync(signedMessage, token);
        }

        private bool HandshakePayloadIsValid(string payload)
        {
            if (!payload.StartsWith("dcl-"))
                return false;

            ReadOnlySpan<char> span = payload.AsSpan(4);
            return span.IndexOf(':') == -1;
        }
    }
}
