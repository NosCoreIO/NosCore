//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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

        public CharacterInitializationService(
            IMinilandService? minilandService = null,
            IMapInstanceGeneratorService? mapInstanceGeneratorService = null)
        {
            _minilandService = minilandService;
            _mapInstanceGeneratorService = mapInstanceGeneratorService;
        }

        public Task InitializeAsync(PlayerComponentBundle player)
        {
            if (_minilandService != null && _mapInstanceGeneratorService != null)
            {
                return _minilandService.InitializeAsync(player, _mapInstanceGeneratorService);
            }

            return Task.CompletedTask;
        }
    }
}
