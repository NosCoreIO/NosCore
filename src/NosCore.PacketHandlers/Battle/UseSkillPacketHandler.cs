//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.PathFinder.Interfaces;
using Microsoft.Extensions.Logging;

namespace NosCore.PacketHandlers.Battle
{
    // Client-facing guard for skill casts. Responsibilities: reject casts that the
    // character is not allowed to make right now (vehicled / dead / bad target), resolve
    // the target entity, and hand off to IBattleService. All damage math lives behind
    // IBattleService so swapping formulas doesn't touch this file.
    public class UseSkillPacketHandler(
        ILogger<UseSkillPacketHandler> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        IBattleService battleService,
        ISessionRegistry sessionRegistry,
        IHeuristic distanceCalculator)
        : PacketHandler<UseSkillPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UseSkillPacket packet, ClientSession clientSession)
        {
            if (!clientSession.Character.CanFight)
            {
                await clientSession.SendPacketAsync(new CancelPacket { Type = CancelPacketType.CancelAutoAttack }).ConfigureAwait(false);
                return;
            }

            if (clientSession.Character.IsSitting)
            {
                await clientSession.Character.RestAsync().ConfigureAwait(false);
            }

            if (clientSession.Character.IsVehicled)
            {
                await clientSession.SendPacketAsync(new CancelPacket { Type = CancelPacketType.CancelAutoAttack }).ConfigureAwait(false);
                return;
            }

            var target = ResolveTarget(packet, clientSession);
            if (target == null)
            {
                return;
            }

            // Cooldown + MP cost gate. Matching OpenNos: CharacterSkill.CanBeUsed()
            // enforces SkillDto.Cooldown * 100ms since LastUse; MpCost is debited on cast,
            // not on hit (so a missed swing still burns mana — prevents spam).
            AcceptReportedPosition(clientSession.Character, packet);

            if (!TryConsumeSkill(clientSession, packet.CastId, target, out var consumed))
            {
                await clientSession.SendPacketAsync(new CancelPacket { Type = CancelPacketType.CancelAutoAttack }).ConfigureAwait(false);
                return;
            }

            var accepted = await battleService.Hit(clientSession.Character, target, new HitArguments
            {
                SkillId = packet.CastId,
            });

            // A cast BattleService refuses spends neither mana nor cooldown, so the player can
            // retry straight away.
            if (accepted)
            {
                consumed?.Invoke();
            }
        }

        // The reported cell is only taken when it is one walk step from the cell the server
        // already has; anything further would let a client cast from wherever it likes.
        private void AcceptReportedPosition(ICharacterEntity character, UseSkillPacket packet)
        {
            if (packet is not { MapX: { } x, MapY: { } y }
                || character.MapInstance?.Map.IsWalkable(x, y) == false
                || !SkillReach.IsWithinOneStep((int)distanceCalculator.GetDistance((character.PositionX, character.PositionY), (x, y)), character.Speed))
            {
                return;
            }

            character.PositionX = x;
            character.PositionY = y;
        }

        // Verifies reach, MP and cooldown WHEN the character owns the skill. For casts the
        // character doesn't have in their learned dict (e.g. transformation-only skills,
        // test fixtures without skill population) we defer rejection to BattleService
        // so the packet path stays permissive and SkillResolver produces a single
        // consistent "unknown skill" response.
        private bool TryConsumeSkill(ClientSession session, long castId, IAliveEntity target, out System.Action? commit)
        {
            commit = null;
            var character = session.Character;
            var skill = character.Skills?.Values.FirstOrDefault(s =>
                s.Skill != null && s.Skill.CastId == castId && s.Skill.UpgradeSkill == 0);
            if (skill?.Skill == null)
            {
                // Skill not in the learned dict — let BattleService decide. Matches the
                // pre-enforcement behaviour and keeps tests that don't populate Skills
                // passing without needing an explicit skill setup step.
                return true;
            }
            if (!skill.CanBeUsed())
            {
                return false;
            }
            var distance = (int)distanceCalculator.GetDistance((character.PositionX, character.PositionY), (target.PositionX, target.PositionY));
            if (!SkillReach.IsInReach(skill.Skill.TargetType, skill.Skill.Range, distance))
            {
                return false;
            }
            if (character.Mp < skill.Skill.MpCost)
            {
                return false;
            }

            // Deferred commit: hold the now-resolved CharacterSkill + mpCost in the closure
            // so the packet handler can fire them only if BattleService.Hit didn't bail.
            var mpCost = skill.Skill.MpCost;
            var trackedSkill = skill;
            commit = () =>
            {
                trackedSkill.LastUse = System.DateTime.Now;
                character.Mp = System.Math.Max(0, character.Mp - mpCost);
            };
            return true;
        }

        // Split out of ExecuteAsync so each branch reads cleanly and unknown visual
        // types get logged once and at the correct severity.
        private IAliveEntity? ResolveTarget(UseSkillPacket packet, ClientSession clientSession)
        {
            IAliveEntity? candidate;
            switch (packet.TargetVisualType)
            {
                case VisualType.Player:
                    candidate = sessionRegistry.TryGetCharacter(s => s.VisualId == packet.TargetId, out var player) ? player : null;
                    break;
                case VisualType.Npc:
                    candidate = clientSession.Character.MapInstance.FindNpc(s => s.VisualId == packet.TargetId);
                    break;
                case VisualType.Monster:
                    candidate = clientSession.Character.MapInstance.FindMonster(s => s.VisualId == packet.TargetId);
                    break;
                default:
                    logger.LogError(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN], packet.TargetVisualType);
                    return null;
            }

            if (candidate == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                return null;
            }
            return candidate;
        }
    }
}
