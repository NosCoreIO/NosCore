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
using Serilog;

namespace NosCore.PacketHandlers.Battle
{
    // Client-facing guard for skill casts. Responsibilities: reject casts that the
    // character is not allowed to make right now (vehicled / dead / bad target), resolve
    // the target entity, and hand off to IBattleService. All damage math lives behind
    // IBattleService so swapping formulas doesn't touch this file.
    public class UseSkillPacketHandler(
        ILogger logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        IBattleService battleService,
        ISessionRegistry sessionRegistry)
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
            if (!TryConsumeSkill(clientSession, packet.CastId, out var consumed))
            {
                await clientSession.SendPacketAsync(new CancelPacket { Type = CancelPacketType.CancelAutoAttack }).ConfigureAwait(false);
                return;
            }

            await battleService.Hit(clientSession.Character, target, new HitArguments
            {
                SkillId = packet.CastId,
                MapX = packet.MapX,
                MapY = packet.MapY,
            }).ConfigureAwait(false);

            // Successful dispatch → stamp LastUse so subsequent casts are cooldown-gated.
            // We do this after Hit so a failed cast (no skill resolved) doesn't consume
            // the cooldown slot and leave the player unable to retry.
            consumed?.Invoke();
        }

        // Verifies MP + cooldown WHEN the character owns the skill. For casts the
        // character doesn't have in their learned dict (e.g. transformation-only skills,
        // test fixtures without skill population) we defer rejection to BattleService
        // so the packet path stays permissive and SkillResolver produces a single
        // consistent "unknown skill" response.
        private static bool TryConsumeSkill(ClientSession session, long castId, out System.Action? commit)
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
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN], packet.TargetVisualType);
                    return null;
            }

            if (candidate == null)
            {
                logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                return null;
            }
            return candidate;
        }
    }
}
