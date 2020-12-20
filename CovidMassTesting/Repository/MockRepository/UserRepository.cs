using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.MockRepository
{
    /// <summary>
    /// In memory user repository
    /// </summary>
    public class UserRepository : Repository.RedisRepository.UserRepository
    {
        private readonly IStringLocalizer<UserRepository> localizer;
        private readonly IPlaceProviderRepository placeProviderRepository;
        private readonly ConcurrentDictionary<string, User> data = new ConcurrentDictionary<string, User>();
        private readonly ConcurrentDictionary<string, Invitation> invitaions = new ConcurrentDictionary<string, Invitation>();
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="localizer2"></param>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="redisCacheClient"></param>
        /// <param name="emailSender"></param>
        /// <param name="smsSender"></param>
        /// <param name="placeRepository"></param>
        /// <param name="placeProviderRepository"></param>
        public UserRepository(
            IStringLocalizer<UserRepository> localizer,
            IStringLocalizer<Repository.RedisRepository.UserRepository> localizer2,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender,
            ISMSSender smsSender,
            IPlaceRepository placeRepository,
            IPlaceProviderRepository placeProviderRepository
        ) : base(
                localizer2,
                configuration,
                loggerFactory.CreateLogger<Repository.RedisRepository.UserRepository>(),
                redisCacheClient,
                emailSender,
                smsSender,
                placeRepository,
                placeProviderRepository
                )
        {
            this.localizer = localizer;
            this.placeProviderRepository = placeProviderRepository;
        }
        /// <summary>
        /// set user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public override async Task<bool> SetUser(User user, bool mustBeNew)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (mustBeNew)
            {
                if (data.ContainsKey(user.Email))
                {
                    throw new Exception(localizer["User already exists"].Value);
                }
            }

            data[user.Email] = user;
            return true;
        }
        /// <summary>
        /// Gets user
        /// </summary>
        /// <param name="email"></param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public override async Task<User> GetUser(string email, string placeProviderId)
        {
            if (!data.ContainsKey(email))
            {
                return null;
            }
            var ret = data[email];
            if (!string.IsNullOrEmpty(placeProviderId))
            {
                var groups = await placeProviderRepository.GetUserGroups(email, placeProviderId);
                if (ret.Roles == null) ret.Roles = new List<string>();
                foreach (var group in groups)
                {
                    if (!ret.Roles.Contains(group)) ret.Roles.Add(group);
                }
            }
            return ret;
        }
        /// <summary>
        /// Returns all users
        /// </summary>
        /// <returns></returns>
        public override async Task<IEnumerable<User>> ListAll()
        {
            return data.Values;
        }
        /// <summary>
        /// Get invitation
        /// </summary>
        /// <param name="invitationId"></param>
        /// <returns></returns>
        public async override Task<Invitation> GetInvitation(string invitationId)
        {
            if (invitaions.TryGetValue(invitationId, out var inv))
            {
                return inv;
            }
            return null;
        }
        /// <summary>
        /// List invitations for place provider
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public async override Task<IEnumerable<Invitation>> ListInvitationsByPP(string placeProviderId)
        {
            return invitaions.Values.Where(i => i.PlaceProviderId == placeProviderId).OrderByDescending(i => i.LastUpdate);
        }
        /// <summary>
        /// List user invitations
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async override Task<IEnumerable<Invitation>> ListInvitationsByEmail(string email)
        {
            return invitaions.Values.Where(i => i.Email == email).OrderByDescending(i => i.LastUpdate);
        }
        /// <summary>
        /// New invitation
        /// </summary>
        /// <param name="invitation"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public async override Task<Invitation> SetInvitation(Invitation invitation, bool mustBeNew)
        {
            invitaions[invitation.InvitationId] = invitation;
            return invitation;
        }

        /// <summary>
        /// Removes user
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public override async Task<bool> Remove(string email)
        {
            data.TryRemove(email, out var _);
            return true;
        }
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public override async Task<int> DropAllData()
        {
            var ret = data.Count;
            data.Clear();
            invitaions.Clear();
            return ret;
        }
    }
}
