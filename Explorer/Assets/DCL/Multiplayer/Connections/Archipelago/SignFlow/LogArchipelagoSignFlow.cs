using Cysharp.Threading.Tasks;
using DCL.Diagnostics;
using DCL.Multiplayer.Connections.Typing;
using System;
using System.Threading;
using UnityEngine;
using Utility.Types;

namespace DCL.Multiplayer.Connections.Archipelago.SignFlow
{
    public class LogArchipelagoSignFlow : IArchipelagoSignFlow
    {
        private const string PREFIX = "LogArchipelagoSignFlow:";
        private readonly IArchipelagoSignFlow origin;
        private readonly Action<string> log;

        public LogArchipelagoSignFlow(IArchipelagoSignFlow origin) : this(
            origin,
            m => ReportHub.Log(ReportCategory.COMMS_SCENE_HANDLER, m)
        ) { }

        public LogArchipelagoSignFlow(IArchipelagoSignFlow origin, Action<string> log)
        {
            this.origin = origin;
            this.log = log;
        }

        public async UniTask EnsureConnectedAsync(string adapterUrl, CancellationToken token)
        {
            log($"{PREFIX} Connect start for {adapterUrl}");
            await origin.EnsureConnectedAsync(adapterUrl, token);
            log($"{PREFIX} Connect finish for {adapterUrl}");
        }

        public async UniTask<LightResult<string>> MessageForSignAsync(string ethereumAddress, CancellationToken token)
        {
            log($"{PREFIX} MessageForSignAsync start for address {ethereumAddress}");
            var result = await origin.MessageForSignAsync(ethereumAddress, token);
            log($"{PREFIX} MessageForSignAsync finish for address {ethereumAddress} with result success: {result.Success}");
            return result;
        }

        public async UniTask<LightResult<string>> WelcomePeerIdAsync(string signedMessageAuthChainJson, CancellationToken token)
        {
            log($"{PREFIX} WelcomePeerIdAsync start for json {signedMessageAuthChainJson}");
            LightResult<string> result = await origin.WelcomePeerIdAsync(signedMessageAuthChainJson, token);
            log($"{PREFIX} WelcomePeerIdAsync finish for json {result} with result {result}");
            return result;
        }

        public async UniTask<Result> SendHeartbeatAsync(Vector3 playerPosition, CancellationToken token)
        {
            log($"{PREFIX} SendHeartbeatAsync start for position {playerPosition}");
            var result = await origin.SendHeartbeatAsync(playerPosition, token);
            log($"{PREFIX} SendHeartbeatAsync finish for position {playerPosition} with success: {result.Success}");
            return result;
        }

        public UniTaskVoid StartListeningForConnectionStringAsync(Action<string> onNewConnectionString, CancellationToken token)
        {
            log($"{PREFIX} StartListeningForConnectionStringAsync start");

            return origin.StartListeningForConnectionStringAsync(newString =>
                {
                    log($"{PREFIX} StartListeningForConnectionStringAsync received string {newString}");
                    onNewConnectionString(newString);
                },
                token
            );
        }

        public async UniTask DisconnectAsync(CancellationToken token)
        {
            log($"{PREFIX} DisconnectAsync start");
            await origin.DisconnectAsync(token);
            log($"{PREFIX} DisconnectAsync finish");
        }
    }
}
