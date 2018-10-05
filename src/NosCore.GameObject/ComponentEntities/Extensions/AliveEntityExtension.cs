using System;
using System.Reactive.Linq;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.Randomizer;
using NosCore.Packets.ServerPackets;
using NosCore.PathFinder;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Interaction;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class AliveEntityExtension
    {
        public static void Move(this INonPlayableEntity nonPlayableEntity,RandomizerService randomizerService)
        {
            if (!nonPlayableEntity.IsAlive)
            {
                return;
            }

            if (nonPlayableEntity.IsMoving && nonPlayableEntity.Speed > 0)
            {
                var time = (DateTime.Now - nonPlayableEntity.LastMove).TotalMilliseconds;

                if (time > randomizerService.RandomNumber(400, 3200))
                {
                    short mapX = nonPlayableEntity.MapX, mapY = nonPlayableEntity.MapY;
                    if (nonPlayableEntity.MapInstance.Map.GetFreePosition(ref mapX, ref mapY, (byte)randomizerService.RandomNumber(0, 3), (byte)randomizerService.RandomNumber(0, 3)))
                    {
                        var distance = (int)Heuristic.Octile(Math.Abs(nonPlayableEntity.PositionX - mapX), Math.Abs(nonPlayableEntity.PositionY - mapY));
                        var value = 1000d * distance / (2 * nonPlayableEntity.Speed);
                        Observable.Timer(TimeSpan.FromMilliseconds(value))
                            .Subscribe(
                                x =>
                                {
                                    nonPlayableEntity.PositionX = mapX;
                                    nonPlayableEntity.PositionY = mapY;
                                });

                        nonPlayableEntity.LastMove = DateTime.Now.AddMilliseconds(value);
                        nonPlayableEntity.MapInstance.Broadcast(new BroadcastPacket(null, nonPlayableEntity.GenerateMove(mapX, mapY), ReceiverType.All));
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
                MapX =  mapX ?? aliveEntity.PositionX,
                MapY =  mapY ?? aliveEntity.PositionY,
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