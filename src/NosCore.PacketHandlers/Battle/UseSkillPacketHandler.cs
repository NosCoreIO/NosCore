//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MovementService;
using NosCore.GameObject.Ecs;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
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
        ISkillCastService skillCastService,
        IHeuristic distanceCalculator,
        IWalkService walkService)
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

            clientSession.Character.CancelDelay();
            var listed = clientSession.Character.ListedTargets;
            clientSession.Character.ListedTargets = null;

            var target = ResolveTarget(packet, clientSession);
            if (target == null)
            {
                return;
            }

            // Reach, cooldown (SkillDto.Cooldown * 100ms since LastUse) and MP gate. Mana and
            // cooldown are only spent once BattleService accepts the cast.
            // The reported cell only sticks when the cast goes through, so refused casts can't
            // be used to step the character around.
            var character = clientSession.Character;
            var (previousX, previousY) = (character.PositionX, character.PositionY);
            var moved = AcceptReportedPosition(character, packet);

            if (!TryConsumeSkill(clientSession, packet.CastId, target, out var consumed))
            {
                (character.PositionX, character.PositionY) = (previousX, previousY);
                await clientSession.SendPacketAsync(new CancelPacket { Type = CancelPacketType.CancelAutoAttack }).ConfigureAwait(false);
                return;
            }

            var accepted = await battleService.Hit(character, target, new HitArguments
            {
                SkillId = packet.CastId,
                ListedTargets = ListedTargetsInReach(clientSession, packet.CastId, listed),
            });

            // A cast BattleService refuses spends neither mana nor cooldown, so the player can
            // retry straight away.
            if (accepted)
            {
                if (consumed != null)
                {
                    await consumed();
                }
                if (moved)
                {
                    await character.MapInstance.SendPacketAsync(character.GenerateMove(),
                        new EveryoneBut(clientSession.ConnectedChannel.Id));
                }
            }
            else
            {
                (character.PositionX, character.PositionY) = (previousX, previousY);
            }
        }

        // The reported cell is only taken when the character could have walked there since its
        // last accepted move; anything further would let a client cast from wherever it likes.
        private bool AcceptReportedPosition(PlayerComponentBundle character, UseSkillPacket packet)
        {
            if (packet is not { MapX: { } x, MapY: { } y }
                || (x == character.PositionX && y == character.PositionY)
                || !walkService.TryStep(character, x, y))
            {
                return false;
            }

            character.PositionX = x;
            character.PositionY = y;
            return true;
        }

        // Verifies reach, MP and cooldown WHEN the character owns the skill. For casts the
        // character doesn't have in their learned dict (e.g. transformation-only skills,
        // test fixtures without skill population) we defer rejection to BattleService
        // so the packet path stays permissive and SkillResolver produces a single
        // consistent "unknown skill" response.
        private bool TryConsumeSkill(ClientSession session, long castId, IAliveEntity target, out Func<Task>? commit)
        {
            commit = null;
            var character = session.Character;
            var skill = skillCastService.FindLearned(character, castId);
            if (skill?.Skill == null)
            {
                return true;
            }
            var distance = (int)distanceCalculator.GetDistance((character.PositionX, character.PositionY), (target.PositionX, target.PositionY));
            var targetArea = target is INonPlayableEntity { NpcMonster: { } npcMonster } ? npcMonster.BasicArea : (byte)0;
            if (!SkillReach.IsInReach(skill.Skill.TargetType, skill.Skill.Range, skill.Skill.TargetRange, distance, targetArea))
            {
                return false;
            }
            if (skillCastService.Check(character, skill, out var mpCost) != SkillCastRefusal.None)
            {
                return false;
            }

            commit = () => skillCastService.SpendAsync(character, skill, mpCost);
            return true;
        }

        private List<IAliveEntity>? ListedTargetsInReach(ClientSession session, long castId,
            IReadOnlyList<(VisualType VisualType, long VisualId)>? listed)
        {
            var character = session.Character;
            if (listed == null || skillCastService.FindLearned(character, castId)?.Skill is not { } skill)
            {
                return null;
            }

            return listed
                .Select(t => FindTarget(t.VisualType, t.VisualId, session))
                .OfType<IAliveEntity>()
                .Where(t => SkillReach.IsTargetInReach(skill.Range,
                    (int)distanceCalculator.GetDistance((character.PositionX, character.PositionY), (t.PositionX, t.PositionY)),
                    t is INonPlayableEntity { NpcMonster: { } npcMonster } ? npcMonster.BasicArea : (byte)0))
                .ToList();
        }

        private IAliveEntity? FindTarget(VisualType visualType, long visualId, ClientSession clientSession) => visualType switch
        {
            VisualType.Player => sessionRegistry.TryGetCharacter(s => s.VisualId == visualId, out var player) ? player : null,
            VisualType.Npc => clientSession.Character.MapInstance.FindNpc(s => s.VisualId == visualId),
            VisualType.Monster => clientSession.Character.MapInstance.FindMonster(s => s.VisualId == visualId),
            _ => null,
        };

        // Split out of ExecuteAsync so each branch reads cleanly and unknown visual
        // types get logged once and at the correct severity.
        private IAliveEntity? ResolveTarget(UseSkillPacket packet, ClientSession clientSession)
        {
            if (packet.TargetVisualType is not (VisualType.Player or VisualType.Npc or VisualType.Monster))
            {
                logger.LogError(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN], packet.TargetVisualType);
                return null;
            }

            var candidate = FindTarget(packet.TargetVisualType, packet.TargetId, clientSession);
            if (candidate == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                return null;
            }
            return candidate;
        }
    }
}
