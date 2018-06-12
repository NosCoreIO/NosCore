using System.Collections.Concurrent;

namespace NosCore.Core.Networking
{
	public sealed class SessionFactory
	{
		#region Instantiation

		private SessionFactory()
		{
			Sessions = new ConcurrentDictionary<string, int>();
		}

		#endregion

		#region Properties

		public static SessionFactory Instance => _instance ?? (_instance = new SessionFactory());

		#endregion

		#region Methods

		public int GenerateSessionId()
		{
			_sessionCounter += 2;
			return _sessionCounter;
		}

		#endregion

		#region Members

		private static SessionFactory _instance;
		private int _sessionCounter;
		public ConcurrentDictionary<string, int> Sessions;

		#endregion
	}
}