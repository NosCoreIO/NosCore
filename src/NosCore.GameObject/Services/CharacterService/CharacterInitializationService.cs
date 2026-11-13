//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.CharacterService
{
    public class CharacterInitializationService : ICharacterInitializationService
    {
        private readonly IMinilandService? _minilandService;
        private readonly IMapInstanceGeneratorService? _mapInstanceGeneratorService;
        private readonly IClock? _clock;

        public CharacterInitializationService(
            IMinilandService? minilandService = null,
            IMapInstanceGeneratorService? mapInstanceGeneratorService = null,
            IClock? clock = null)
        {
            _minilandService = minilandService;
            _mapInstanceGeneratorService = mapInstanceGeneratorService;
            _clock = clock;
        }

        public async Task InitializeAsync(PlayerComponentBundle player)
        {
            if (_minilandService == null || _mapInstanceGeneratorService == null)
            {
                return;
            }

            var miniland = await _minilandService.InitializeAsync(player, _mapInstanceGeneratorService);
            if (_clock != null && miniland.RefreshDailyPoints(_clock.GetCurrentInstant()) is > 0 and var earned)
            {
                player.Reputation += earned;
            }
        }
    }
}
