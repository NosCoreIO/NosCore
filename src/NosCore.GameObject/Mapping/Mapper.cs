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

using NosCore.Packets.Interfaces;
using Mapster;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject.Mapping
{
    public class Mapper
    {
        //TODO cleanup
        public Mapper()
        {
            TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;
            TypeAdapterConfig.GlobalSettings.Default.IgnoreAttribute(typeof(I18NFromAttribute));
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IPacket>().Ignore(s => s.ValidationResult!);
            /*GO to Dto*/
            TypeAdapterConfig<ItemInstance?, WearableInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstance?, SpecialistInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstance?, BoxInstanceDto?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstance?, SpecialistInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<WearableInstance?, BoxInstanceDto?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<SpecialistInstance?, BoxInstanceDto?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstance?, BoxInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance?, SpecialistInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance?, WearableInstanceDto?>.NewConfig()
                .MapWith(src => null);

            /*Dto to GO*/
            TypeAdapterConfig<ItemInstanceDto?, WearableInstance?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstanceDto?, SpecialistInstance?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstanceDto?, BoxInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<ItemInstanceDto?, SpecialistInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<ItemInstanceDto?, BoxInstance?>.NewConfig()
                .MapWith(src => null);


            TypeAdapterConfig<WearableInstanceDto?, BoxInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstanceDto?, SpecialistInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<SpecialistInstanceDto?, BoxInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstanceDto?, BoxInstance?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto?, SpecialistInstance?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto?, WearableInstance?>.NewConfig()
                .MapWith(src => null);

            /*GO to GO*/
            TypeAdapterConfig<ItemInstance?, WearableInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<ItemInstance?, BoxInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<ItemInstance?, SpecialistInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstance?, BoxInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstance?, SpecialistInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<SpecialistInstance?, BoxInstance?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstance?, BoxInstance?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance?, SpecialistInstance?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstance?, WearableInstance?>.NewConfig()
                .MapWith(src => null);

            /*DTO to DTO*/
            TypeAdapterConfig<ItemInstanceDto?, WearableInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstanceDto?, SpecialistInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<ItemInstanceDto?, BoxInstanceDto?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<WearableInstanceDto?, SpecialistInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<WearableInstanceDto?, BoxInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<SpecialistInstanceDto?, BoxInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<SpecialistInstanceDto?, WearableInstanceDto?>.NewConfig()
                .MapWith(src => null);

            TypeAdapterConfig<UsableInstanceDto?, BoxInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto?, SpecialistInstanceDto?>.NewConfig()
                .MapWith(src => null);
            TypeAdapterConfig<UsableInstanceDto?, WearableInstanceDto?>.NewConfig()
                .MapWith(src => null);
        }
    }
}