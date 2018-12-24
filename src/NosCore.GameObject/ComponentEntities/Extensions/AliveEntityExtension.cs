//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Reactive.Linq;
using NosCore.Core;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.PathFinder;
using NosCore.Shared;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class AliveEntityExtension
    {
        public static NpcReqPacket GenerateNpcReq(this IAliveEntity namedEntity, short dialog)
        {
            return new NpcReqPacket
            {
                VisualType = namedEntity.VisualType,
                VisualId = namedEntity.VisualId,
                Dialog = dialog,
            };
        }

        public static PinitSubPacket GenerateSubPinit(this INamedEntity namedEntity, int groupPosition)
        {
            return new PinitSubPacket
            {
                VisualType = namedEntity.VisualType,
                VisualId = namedEntity.VisualId,
                GroupPosition = groupPosition,
                Level = namedEntity.Level,
                Name = namedEntity.Name,
                Gender = (namedEntity as ICharacterEntity)?.Gender ?? GenderType.Male,
                Race = namedEntity.Race,
                Morph = namedEntity.Morph,
                HeroLevel = namedEntity.HeroLevel
            };
        }

        public static PidxSubPacket GenerateSubPidx(this IAliveEntity playableEntity) => playableEntity.GenerateSubPidx(false);
        public static PidxSubPacket GenerateSubPidx(this IAliveEntity playableEntity, bool isMemberOfGroup)
        {
            return new PidxSubPacket
            {
                IsGrouped = isMemberOfGroup,
                VisualId = playableEntity.VisualId
            };
        }

        public static StPacket GenerateStatInfo(this IAliveEntity aliveEntity)
        {
            return new StPacket
            {
                Type = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                Level = aliveEntity.Level,
                HeroLvl = aliveEntity.HeroLevel,
                HpPercentage = (int)(aliveEntity.Hp / (float)aliveEntity.MaxHp * 100),
                MpPercentage = (int)(aliveEntity.Mp / (float)aliveEntity.MaxMp * 100),
                CurrentHp = aliveEntity.Hp,
                CurrentMp = aliveEntity.Mp,
                BuffIds = null
            };
        }

        public static void Move(this INonPlayableEntity nonPlayableEntity)
        {
            if (!nonPlayableEntity.IsAlive)
            {
                return;
            }

            if (nonPlayableEntity.IsMoving && nonPlayableEntity.Speed > 0)
            {
                var time = (SystemTime.Now() - nonPlayableEntity.LastMove).TotalMilliseconds;

                if (time > RandomFactory.Instance.RandomNumber(400, 3200))
                {
                    short mapX = nonPlayableEntity.MapX;
                    short mapY = nonPlayableEntity.MapY;
                    if (nonPlayableEntity.MapInstance.Map.GetFreePosition(ref mapX, ref mapY,
                        (byte)RandomFactory.Instance.RandomNumber(0, 3),
                        (byte)RandomFactory.Instance.RandomNumber(0, 3)))
                    {
                        var distance = (int)Heuristic.Octile(Math.Abs(nonPlayableEntity.PositionX - mapX),
                            Math.Abs(nonPlayableEntity.PositionY - mapY));
                        var value = 1000d * distance / (2 * nonPlayableEntity.Speed);
                        Observable.Timer(TimeSpan.FromMilliseconds(value))
                            .Subscribe(
                                _ =>
                                {
                                    nonPlayableEntity.PositionX = mapX;
                                    nonPlayableEntity.PositionY = mapY;
                                });

                        nonPlayableEntity.LastMove = SystemTime.Now().AddMilliseconds(value);
                        nonPlayableEntity.MapInstance.Sessions.SendPacket(
                            nonPlayableEntity.GenerateMove(mapX, mapY));
                    }
                }
            }
        }

        public static void Rest(this IAliveEntity aliveEntity)
        {
            aliveEntity.IsSitting = !aliveEntity.IsSitting;
            aliveEntity.MapInstance.Sessions.SendPacket(
                aliveEntity.GenerateRest());
        }

        public static CondPacket GenerateCond(this IAliveEntity aliveEntity)
        {
            return new CondPacket
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                NoAttack = aliveEntity.NoAttack,
                NoMove = aliveEntity.NoMove,
                Speed = aliveEntity.Speed
            };
        }

        public static SayPacket GenerateSay(this IAliveEntity aliveEntity, string message, SayColorType type)
        {
            return new SayPacket
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                Type = type,
                Message = message
            };
        }

        public static GenericUseItemPacket GenerateGenericUseItem(this IAliveEntity aliveEntity, UseItemPacket packet)
        {
            return new GenericUseItemPacket
            {
                VisualId = aliveEntity.VisualId,
                VisualType = aliveEntity.VisualType,
                UsePacket = packet
            };
        }

        public static PairyPacket GeneratePairy(this IAliveEntity aliveEntity, WearableInstance fairy)
        {
            bool isBuffed = false;//TODO aliveEntity.Buff.Any(b => b.Card.CardId == 131);
            return new PairyPacket
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                Unknown = fairy == null ? 0 : 4,
                Element = fairy?.Item.Element ?? 0,
                ElementRate = fairy?.ElementRate + fairy?.Item.ElementRate ?? 0,
                Morph = fairy?.Item.Morph ?? 0 + (isBuffed ? 5 : 0)
            };
        }

        public static CModePacket GenerateCMode(this IAliveEntity aliveEntity)
        {
            return new CModePacket
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                Morph = aliveEntity.Morph,
                MorphUpgrade = aliveEntity.MorphUpgrade,
                MorphDesign = aliveEntity.MorphDesign,
                MorphBonus = aliveEntity.MorphBonus
            };
        }

        public static MovePacket GenerateMove(this IAliveEntity aliveEntity) => aliveEntity.GenerateMove(null, null);
        public static MovePacket GenerateMove(this IAliveEntity aliveEntity, short? mapX, short? mapY)
        {
            return new MovePacket
            {
                VisualEntityId = aliveEntity.VisualId,
                MapX = mapX ?? aliveEntity.PositionX,
                MapY = mapY ?? aliveEntity.PositionY,
                Speed = aliveEntity.Speed,
                VisualType = aliveEntity.VisualType
            };
        }

        public static EffectPacket GenerateEff(this IAliveEntity aliveEntity, int effectid)
        {
            return new EffectPacket
            {
                EffectType = aliveEntity.VisualType,
                VisualEntityId = aliveEntity.VisualId,
                Id = effectid
            };
        }

        public static SayPacket GenerateSay(this IAliveEntity aliveEntity, SayPacket packet)
        {
            return new SayPacket
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                Type = packet.Type,
                Message = packet.Message
            };
        }

        public static RestPacket GenerateRest(this IAliveEntity aliveEntity)
        {
            return new RestPacket
            {
                VisualType = aliveEntity.VisualType,
                VisualId = aliveEntity.VisualId,
                IsSitting = aliveEntity.IsSitting
            };
        }

        public static void SetLevel(this INamedEntity experiencedEntity, byte level)
        {
            experiencedEntity.Level = level;
            experiencedEntity.LevelXp = 0;
            experiencedEntity.Hp = experiencedEntity.MaxHp;
            experiencedEntity.Mp = experiencedEntity.MaxMp;
        }
    }
}