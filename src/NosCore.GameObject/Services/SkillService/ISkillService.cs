using NosCore.GameObject.Ecs.Interfaces;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SkillService
{
    public interface ISkillService
    {
        Task LoadSkill(ICharacterEntity character);

        Task LoadSpecialistSkillsAsync(ICharacterEntity character, short morph, byte spLevel);

        /// <summary>Removes the specialist skills and sends the class list back.</summary>
        Task UnloadSpecialistSkillsAsync(ICharacterEntity character);

        Task<bool> LearnClassSkillsAsync(ICharacterEntity character);

        /// <summary>
        /// Deletes the skills the character cannot learn right now - wrong class, or a job level
        /// they no longer have - from memory and from the database both.
        /// </summary>
        /// <remarks>
        /// A class change already emptied the in-memory list; the rows behind it stayed, and
        /// came back on the next login. See the implementation for what that did.
        /// </remarks>
        Task ForgetUnlearnableSkillsAsync(ICharacterEntity character);
    }
}
