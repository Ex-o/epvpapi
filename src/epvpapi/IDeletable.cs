﻿using epvpapi.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epvpapi
{
    /// <summary>
    /// Interface for objects that are deletable
    /// </summary>
    interface IDeletable
    {
        /// <summary>
        /// Deletes the object using the given <c>Session</c>
        /// </summary>
        /// <param name="session"> Session that is used for sending the request </param>
        void Delete<T>(ProfileSession<T> session) where T : User; 
    }
}
