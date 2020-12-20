using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Admin can invite person to the place provider (company which manages the testing places)
    /// 
    /// After user accepts invitation, admin can allocate him to the testing place at specific time
    /// </summary>
    public class Invitation
    {
        /// <summary>
        /// Id
        /// </summary>
        public string InvitationId { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Phone
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Inviter
        /// </summary>
        public string InviterName { get; set; }
        /// <summary>
        /// Place provider
        /// </summary>
        public string PlaceProviderId { get; set; }
        /// <summary>
        /// Invitation status - invited | accepted | declined
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Time of the invitation
        /// </summary>
        public DateTimeOffset InvitationTime { get; set; }
        /// <summary>
        /// Time of the invitation
        /// </summary>
        public DateTimeOffset? StatusTime { get; set; }
        /// <summary>
        /// Returns last update of the invitation
        /// </summary>
        public DateTimeOffset LastUpdate
        {
            get
            {
                if (StatusTime.HasValue) return StatusTime.Value;
                return InvitationTime;
            }
        }
    }
    /// <summary>
    /// Invitation.Status valid values
    /// </summary>
    public static class InvitationStatus
    {
        /// <summary>
        /// Invited
        /// </summary>
        public const string Invited = "invited";
        /// <summary>
        /// Accepted
        /// </summary>
        public const string Accepted = "accepted";
        /// <summary>
        /// Declined
        /// </summary>
        public const string Declined = "declined";
    }
}
