//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BazaarService;
using NosCore.GameObject.Services.MailService;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.MasterServer
{
    public class MasterServer(IOptions<MasterConfiguration> masterConfiguration, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IBazaarService bazaarService,
            IMailService mailService,
            IDao<BazaarItemDto, long> bazaarItemDao,
            IDao<MailDto, long> mailDao,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<CharacterDto, long> characterDao,
            List<ItemDto> items)
        : BackgroundService
    {
        private readonly MasterConfiguration _masterConfiguration = masterConfiguration.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bazaarService.Initialize(bazaarItemDao, itemInstanceDao, characterDao);
            await mailService.InitializeAsync(characterDao, mailDao, items, itemInstanceDao).ConfigureAwait(false);
            logger.Information(logLanguage[LogLanguageKey.SUCCESSFULLY_LOADED]);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title += $@" - WebApi : {_masterConfiguration.WebApi}";
            }
        }
    }
}