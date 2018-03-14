using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core.Networking
{
    public class SessionFactory
    {
        #region Members

        private static SessionFactory _instance;
        private int _sessionCounter;

        #endregion

        #region Instantiation

        private SessionFactory()
        {
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
    }
}