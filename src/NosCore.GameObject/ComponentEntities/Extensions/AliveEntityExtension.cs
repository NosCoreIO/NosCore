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
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using log4net.Core;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.PathFinder;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class AliveEntityExtension
    {
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
                Class = namedEntity.Class,
                Morph = namedEntity.Morph,
                HeroLevel = namedEntity.HeroLevel
            };
        }

        public static PidxSubPacket GenerateSubPidx(this IAliveEntity playableEntity, bool isMemberOfGroup = false)
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
                HpPercentage = (int) (aliveEntity.Hp / (float) aliveEntity.MaxHp * 100),
                MpPercentage = (int) (aliveEntity.Mp / (float) aliveEntity.MaxMp * 100),
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
                var time = (DateTime.Now - nonPlayableEntity.LastMove).TotalMilliseconds;

                if (time > RandomFactory.Instance.RandomNumber(400, 3200))
                {
                    short mapX = nonPlayableEntity.MapX, mapY = nonPlayableEntity.MapY;
                    if (nonPlayableEntity.MapInstance.Map.GetFreePosition(ref mapX, ref mapY,
                        (byte) RandomFactory.Instance.RandomNumber(0, 3),
                        (byte) RandomFactory.Instance.RandomNumber(0, 3)))
                    {
                        var distance = (int) Heuristic.Octile(Math.Abs(nonPlayableEntity.PositionX - mapX),
                            Math.Abs(nonPlayableEntity.PositionY - mapY));
                        var value = 1000d * distance / (2 * nonPlayableEntity.Speed);
                        Observable.Timer(TimeSpan.FromMilliseconds(value))
                            .Subscribe(
                                _ =>
                                {
                                    nonPlayableEntity.PositionX = mapX;
                                    nonPlayableEntity.PositionY = mapY;
                                });

                        nonPlayableEntity.LastMove = DateTime.Now.AddMilliseconds(value);
                        nonPlayableEntity.MapInstance.Broadcast(new BroadcastPacket(null,
                            nonPlayableEntity.GenerateMove(mapX, mapY), ReceiverType.All));
                    }
                }
            }
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

        public static MovePacket GenerateMove(this IAliveEntity aliveEntity, short? mapX = null, short? mapY = null)
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
    }
}