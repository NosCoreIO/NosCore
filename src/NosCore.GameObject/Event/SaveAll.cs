using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Networking;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Event
{
    [UsedImplicitly]
    public class SaveAll : IGlobalEvent
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public TimeSpan Delay { get; set; } = TimeSpan.FromMinutes(5);

        public void Execution()
        {
            try
            {
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SAVING_ALL));
                Parallel.ForEach(Broadcaster.Instance.GetCharacters(), session => session.Save());
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
            }
        }
    }
}
