//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NosCore.Data.Dto;

namespace NosCore.GameObject.Ecs;

// The one place a character row meets the player's components, in both directions. A column
// with the same name as a bundle member needs nothing here.
public static class CharacterMapping
{
    private static readonly TypeAdapterConfig Config = Build();

    private static TypeAdapterConfig Build()
    {
        var config = new TypeAdapterConfig();
        // The row's identity is fixed when the character is created; the bundle only shows it.
        config.NewConfig<PlayerComponentBundle, CharacterDto>()
            .TwoWays()
            .Map(dest => dest.HeroXp, src => src.HeroLevelXp)
            .Map(dest => dest.Reput, src => src.Reputation)
            .Ignore(dest => dest.CharacterId, dest => dest.AccountId, dest => dest.ServerId,
                dest => dest.Name, dest => dest.Slot, dest => dest.Prefix);
        config.Compile();
        return config;
    }

    public static void Load(CharacterDto row, PlayerComponentBundle player) => row.Adapt(player, Config);

    public static void Save(PlayerComponentBundle player, CharacterDto row) => player.Adapt(row, Config);
}
