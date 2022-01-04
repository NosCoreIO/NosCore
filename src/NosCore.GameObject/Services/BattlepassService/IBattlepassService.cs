using NosCore.Data.Dto;
using NosCore.GameObject.ComponentEntities.Interfaces;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BattlepassService
{
    public interface IBattlepassService
    {
        Task IncrementQuestObjectives(ICharacterEntity character, long questId, int toAdd);
    }
}