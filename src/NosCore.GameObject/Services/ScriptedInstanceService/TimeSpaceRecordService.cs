//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.Infastructure;
using NosCore.Packets.ServerPackets.Event;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public class TimeSpaceRecordService(IDao<TimeSpaceRecordDto, Guid> recordDao, IDao<CharacterDto, long> characterDao)
        : ITimeSpaceRecordService, ISingletonService
    {
        private readonly SemaphoreSlim _lock = new(1, 1);

        public async Task<RbrSubPacketHighScore> GetHighScoreAsync(short timeSpaceId)
        {
            var record = await recordDao.FirstOrDefaultAsync(r => r.TimeSpaceId == timeSpaceId);
            if (record == null)
            {
                return new RbrSubPacketHighScore { Score = 0, Nickname = null };
            }

            var holder = await characterDao.FirstOrDefaultAsync(c => c.CharacterId == record.CharacterId);
            return new RbrSubPacketHighScore
            {
                Score = (short)Math.Min(record.Score, short.MaxValue),
                Nickname = holder?.Name
            };
        }

        public async Task<bool> TryBreakAsync(short timeSpaceId, long characterId, int score)
        {
            await _lock.WaitAsync();
            try
            {
                var record = await recordDao.FirstOrDefaultAsync(r => r.TimeSpaceId == timeSpaceId);
                if (score <= (record?.Score ?? 0))
                {
                    return false;
                }

                var saved = await recordDao.TryInsertOrUpdateAsync(new TimeSpaceRecordDto
                {
                    Id = record?.Id ?? Guid.NewGuid(),
                    TimeSpaceId = timeSpaceId,
                    CharacterId = characterId,
                    Score = score
                });
                return saved != null;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
