using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Ecs;
using NosCore.Data.StaticEntities;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SkillService
{
    public interface ISkillService
    {
        Task LoadSkill(PlayerComponentBundle character);

        Task LoadSpecialistSkillsAsync(PlayerComponentBundle character, short morph, byte spLevel);

        /// <summary>Removes the specialist skills and sends the class list back.</summary>
        Task UnloadSpecialistSkillsAsync(PlayerComponentBundle character);

        /// <summary>
        /// Teaches every class skill the job level allows. With <paramref name="announce"/>, as on a
        /// job level-up, a newly learnt skill is told in the chat and across the screen and the
        /// quick bar is redrawn.
        /// </summary>
        Task<bool> LearnClassSkillsAsync(PlayerComponentBundle character, bool announce = false);

        /// <summary>
        /// Deletes the skills the character cannot learn right now - wrong class, or a job level
        /// they no longer have - from memory and from the database both.
        /// </summary>
        /// <remarks>
        /// A class change already emptied the in-memory list; the rows behind it stayed, and
        /// came back on the next login. See the implementation for what that did.
        /// </remarks>
        Task ForgetUnlearnableSkillsAsync(PlayerComponentBundle character);

        /// <summary>Teaches a skill bought from a skill shop, charging its price.</summary>
        Task BuyAsync(PlayerComponentBundle character, SkillDto skill);

        /// <summary>Stores a tattoo the character was just inked with; the caller sends the skill list.</summary>
        Task LearnTattooAsync(PlayerComponentBundle character, SkillDto tattoo);

        /// <summary>Teaches the passive a skill book holds, for free; false when it is refused.</summary>
        Task<bool> LearnFromBookAsync(PlayerComponentBundle character, short skillVNum);
    }
}
