//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Ecs;

public readonly record struct PlayerSpawn(
    CharacterDto Character,
    AccountDto Account,
    ScriptDto? Script,
    int ServerId,
    Guid MapInstanceId,
    short PositionX,
    short PositionY,
    int MaxHp,
    int MaxMp,
    byte Speed);
