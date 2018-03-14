using System;
using System.Runtime.CompilerServices;
using log4net;

namespace NosCore.Core.Logger
{
    public class Logger
    {
        #region Properties

        public static ILog Log { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Wraps up the message with the CallerMemberName
        /// </summary>
        /// <param name="Caller"></param>
        /// <param name="message"></param>
        /// <param name="memberName"></param>
        public static void Debug(string Caller, string message, [CallerMemberName] string memberName = "")
        {
            Log?.Debug($"{Caller} Method: {memberName} Packet: {message}");
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
        public static void Info(string message, Exception innerException = null, [CallerMemberName] string memberName = "")
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