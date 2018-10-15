//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System;
using System.Runtime.CompilerServices;
using log4net;

namespace NosCore.Shared.I18N
{
    public static class Logger
    {
        #region Properties

        public static ILog Log { get; set; }

        #endregion

        #region Methods

        public static void PrintHeader(string text)
        {
            var offset = ((Console.WindowWidth - 20) / 2) + (text.Length / 2);
            var separator = new string('=', Console.WindowWidth - 20);
            Logger.Log.Info(separator);
            Logger.Log.Info(string.Format("{0," + offset + "}", text));
            Logger.Log.Info(separator);
        }

        /// <summary>
        ///     Wraps up the message with the CallerMemberName
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="message"></param>
        /// <param name="memberName"></param>
        public static void Debug(string caller, string message, [CallerMemberName] string memberName = "")
        {
            Log?.Debug($"{caller} Method: {memberName} Packet: {message}");
        }

        /// <summary>
        ///     Wraps up the error message with the CallerMemberName
        /// </summary>
        /// <param name="memberName"></param>
        /// <param name="innerException"></param>
        public static void Error(Exception innerException = null, [CallerMemberName] string memberName = "")
        {
            if (innerException != null)
            {
                Log?.Error($"{memberName}: {innerException.Message}", innerException);
            }
        }

        /// <summary>
        ///     Wraps up the info message with the CallerMemberName
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <param name="memberName"></param>
        public static void Info(string message, Exception innerException = null,
            [CallerMemberName] string memberName = "")
        {
            if (innerException != null)
            {
                Log?.Info($"Method: {memberName} Message: {message}", innerException);
            }
        }

        public static void InitializeLogger(ILog log)
        {
            Log = log;
        }

        #endregion
    }
}