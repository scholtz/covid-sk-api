﻿using System;
using System.Collections.Generic;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// User public object
    /// </summary>
    public class UserPublic
    {
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// List of roles
        /// </summary>
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only
        public List<string> Roles { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists
        /// <summary>
        /// Place at which person is assigned. All person's registrations will be placed to this location
        /// </summary>
        public string Place { get; set; }
        /// <summary>
        /// In some cases fetch the place (eg in Me method)
        /// </summary>
        public Place PlaceObj { get; set; }
        /// <summary>
        /// Last time user has updated his location
        /// </summary>
        public DateTimeOffset? PlaceLastCheck { get; set; }
    }
}
