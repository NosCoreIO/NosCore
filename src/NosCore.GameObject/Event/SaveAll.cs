using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Networking;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Event
{
    [UsedImplicitly]
    class SaveAll : IGlobalEvent
    {
        public TimeSpan Delay { get; set; } = TimeSpan.FromMinutes(5);

        public void Execution()
        {
            try
            {
                Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SAVING_ALL));
                Parallel.ForEach(Broadcaster.Instance.GetCharacters(), session => session.Save());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
