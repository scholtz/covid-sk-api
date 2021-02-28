using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers
{
    /// <summary>
    /// Manages places
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PlaceController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<PlaceController> localizer;
        private readonly ILogger<PlaceController> logger;
        private readonly IPlaceRepository placeRepository;
        private readonly IPlaceProviderRepository placeProviderRepository;
        private readonly IUserRepository userRepository;
        private readonly ISlotRepository slotRepository;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="localizer"></param>
        /// <param name="logger"></param>
        /// <param name="placeRepository"></param>
        /// <param name="userRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="slotRepository"></param>
        public PlaceController(
            IConfiguration configuration,
            IStringLocalizer<PlaceController> localizer,
            ILogger<PlaceController> logger,
            IPlaceRepository placeRepository,
            IUserRepository userRepository,
            IPlaceProviderRepository placeProviderRepository,
            ISlotRepository slotRepository
            )
        {
            this.configuration = configuration;
            this.localizer = localizer;
            this.logger = logger;
            this.placeRepository = placeRepository;
            this.userRepository = userRepository;
            this.placeProviderRepository = placeProviderRepository;
            this.slotRepository = slotRepository;
        }


        private static ConcurrentBag<Place> Cache = new ConcurrentBag<Place>();
        private static DateTimeOffset? CacheTime;
        private async Task<IEnumerable<Place>> ListAllPlaces()
        {
            IEnumerable<Place> list;

            if (string.IsNullOrEmpty(configuration["DoNotUseObjCache"]))
            {
                var rand = new Random();
                var limit = rand.Next(1, 5);
                if (CacheTime.HasValue && CacheTime.Value.AddMinutes(limit) > DateTimeOffset.Now)
                {
                    list = Cache;
                }
                else
                {
                    list = await placeRepository.ListAll();
                    list = list.Where(p => p.IsVisible == true);
                    var toCache = new ConcurrentBag<Place>(list);
                    EnrichWithEmptySlots(ref toCache);
                    Cache = toCache;
                    CacheTime = DateTimeOffset.Now;
                    list = Cache;
                }
            }
            else
            {
                list = await placeRepository.ListAll();
                list = list.Where(p => p.IsVisible == true);
                var toCache = new ConcurrentBag<Place>(list);
                EnrichWithEmptySlots(ref toCache);
                Cache = toCache;
                CacheTime = DateTimeOffset.Now;
                list = Cache;
            }
            return list;
        }

        /// <summary>
        /// List places
        /// 
        /// Contains live statistics of users, registered, infected and healthy visitors
        /// </summary>
        /// <returns></returns>
        [HttpGet("List")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, Place>>> List()
        {
            try
            {

                var list = await ListAllPlaces();
                var ret = list.ToDictionary(p => p.Id, p => p);


                if (string.IsNullOrEmpty(configuration["LimitPer5MinSlot"])
                    && string.IsNullOrEmpty(configuration["LimitPer1HourSlot"]))
                {
                    return Ok(ret);
                }

                if (!string.IsNullOrEmpty(configuration["LimitPer5MinSlot"]))
                {
                    if (int.TryParse(configuration["LimitPer5MinSlot"], out var confLimit))
                    {
                        foreach (var key in ret.Keys)
                        {
                            if (ret[key].LimitPer1HourSlot > confLimit)
                            {
                                ret[key].LimitPer1HourSlot = confLimit;
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(configuration["LimitPer1HourSlot"]))
                {
                    if (int.TryParse(configuration["LimitPer1HourSlot"], out var confLimit))
                    {
                        foreach (var key in ret.Keys)
                        {
                            if (ret[key].LimitPer1HourSlot > confLimit)
                            {
                                ret[key].LimitPer1HourSlot = confLimit;
                            }
                        }
                    }
                }
                return Ok(ret.OrderBy(p => p.Value.Name));
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        private bool EnrichWithEmptySlots(ref ConcurrentBag<Place> list, int daysCount = 1)
        {
            var ret = new List<Place>();

            Parallel.ForEach(list, item =>
            {
                try
                {
                    var total = 0;
                    var days = slotRepository.ListDaySlotsByPlace(item.Id).Result;
                    foreach (var day in
                        days.Where(d =>
                            d.Time >= DateTimeOffset.UtcNow.AddDays(-1)
                            && d.Time < DateTimeOffset.UtcNow.AddDays(daysCount - 1)
                    ))
                    {
                        var hours = slotRepository.ListHourSlotsByPlaceAndDaySlotId(item.Id, day.SlotId).Result;
                        foreach (var hour in hours)
                        {
                            if (hour.Time < DateTimeOffset.Now.AddHours(-1))
                            {
                                continue;
                            }

                            var count = item.LimitPer1HourSlot - hour.Registrations;
                            if (hour.Registrations == 2)
                            {
                                var x = 0;
                                x++;
                            }

                            var countMAggregated = 0;
                            if (count > 0)
                            {
                                var minutes = slotRepository.ListMinuteSlotsByPlaceAndHourSlotId(item.Id, hour.SlotId).Result;
                                foreach (var minute in minutes)
                                {
                                    if (minute.Time < DateTimeOffset.Now.AddMinutes(-5))
                                    {
                                        continue;
                                    }

                                    var countm = item.LimitPer5MinSlot - minute.Registrations;
                                    if (countm > 0)
                                    {
                                        countMAggregated += countm;
                                    }
                                }
                            }
                            total += Math.Min(countMAggregated, count);
                        }
                        item.AvailableSlotsTodayUpdate = DateTimeOffset.UtcNow;
                        item.AvailableSlotsToday = total;
                    }
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, exc.Message);
                }
            });

            return true;
        }

        /// <summary>
        /// List places
        /// 
        /// Contains live statistics of users, registered, infected and healthy visitors
        /// </summary>
        /// <returns></returns>
        [HttpGet("ListFiltered")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, Place>>> ListFiltered([FromQuery] string category = "all", [FromQuery] string availability = "all")
        {
            try
            {
                var places = await ListAllPlaces();


                if (string.IsNullOrEmpty(category))
                {
                    category = "all";
                }

                if (string.IsNullOrEmpty(availability))
                {
                    availability = "all";
                }

                if (category != "all")
                {
                    var onlyInsured = false;
                    if (category.EndsWith("-doctor"))
                    {
                        onlyInsured = true;
                        category = category.Replace("-doctor", "");
                    }
                    if (category.EndsWith("-self"))
                    {
                        onlyInsured = false;
                        category = category.Replace("-self", "");
                    }
                    var pprs = await placeProviderRepository.ListPlaceProductByCategory(category);

                    if (onlyInsured)
                    {
                        pprs = pprs.Where(ppr => ppr.InsuranceOnly == true);
                    }

                    var ids = pprs.Select(ppr => ppr.PlaceId).Distinct().Where(id => !string.IsNullOrEmpty(id)).ToHashSet();
                    places = places.Where(place => ids.Contains(place.Id));
                }
                if (availability == "all")
                {
                    return Ok(places.OrderBy(p => p.Name).ToDictionary(p => p.Id, p => p));
                }


                var ret = new List<Place>();
                ///@TODO
                return Ok(ret.ToDictionary(p => p.Id, p => p));
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// List places
        /// 
        /// Contains live statistics of users, registered, infected and healthy visitors
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("PrivateList")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, Place>>> PrivateList()
        {
            try
            {
                var isGlobalAdmin = User.IsAdmin(userRepository);
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    return Ok(null);
                }

                var list = (await placeRepository.ListAll());
                //if (isGlobalAdmin) return Ok(list.ToDictionary(p => p.Id, p => p));
                return Ok(list.Where(p => p.PlaceProviderId == User.GetPlaceProvider()).OrderBy(p => p.Name).ToDictionary(p => p.Id, p => p));
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Set openning hours for branches.
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("ScheduleOpenningHours")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> ScheduleOpenningHours([FromBody] TimeUpdate[] actions = null)
        {
            try
            {
                if (actions is null)
                {
                    throw new ArgumentNullException(nameof(actions));
                }
                var isGlobalAdmin = User.IsAdmin(userRepository);
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("You must be place provider admin to be able to set openning hours");
                }

                var list = (await placeRepository.ListAll());
                if (!isGlobalAdmin)
                {
                    list = list.Where(p => p.PlaceProviderId == User.GetPlaceProvider());
                }
                var ids = list.Select(i => i.Id).Distinct().ToHashSet();
                if (actions.Any(a => string.IsNullOrEmpty(a.PlaceId) || (a.PlaceId != "__ALL__" && !ids.Contains(a.PlaceId))))
                {
                    throw new Exception("You cannot manage all places you have selected");
                }
                var places = new Dictionary<string, Place>();
                foreach (var placeId in ids)
                {
                    places[placeId] = await placeRepository.GetPlace(placeId);
                    if (places[placeId] == null)
                    {
                        throw new Exception($"Invalid place with id {placeId}");
                    }

                    foreach (var action in actions.Where(a => a.PlaceId == placeId))
                    {
                        if (action.Type == "set")
                        {
                            if (action.OpeningHoursTemplateId == 1)
                            {
                                if (!places[placeId].OpeningHoursWorkDay.ValidateOpeningHours())
                                {
                                    throw new Exception($"Place {places[placeId].Name} does not have valid opening hours: {places[placeId].OpeningHoursWorkDay}");
                                }
                            }
                            if (action.OpeningHoursTemplateId == 2)
                            {
                                if (!places[placeId].OpeningHoursOther1.ValidateOpeningHours())
                                {
                                    throw new Exception($"Place {places[placeId].Name} does not have valid opening hours: {places[placeId].OpeningHoursOther1}");
                                }
                            }
                            if (action.OpeningHoursTemplateId == 3)
                            {
                                if (!places[placeId].OpeningHoursOther2.ValidateOpeningHours())
                                {
                                    throw new Exception($"Place {places[placeId].Name} does not have valid opening hours: {places[placeId].OpeningHoursOther2}");
                                }
                            }
                        }
                    }
                }
                var ret = 0;

                foreach (var action in actions)
                {
                    foreach (var placeId in ids)
                    {
                        if (action.PlaceId != "__ALL__" && action.PlaceId != placeId)
                        {
                            continue;
                        }

                        var hours = "";
                        var dayTicks = DateTimeOffset.Parse(action.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture).Ticks;
                        if (action.Type == "set")
                        {
                            if (action.OpeningHoursTemplateId == 1)
                            {
                                hours = places[placeId].OpeningHoursWorkDay;
                            }
                            if (action.OpeningHoursTemplateId == 2)
                            {
                                hours = places[placeId].OpeningHoursOther1;
                            }
                            if (action.OpeningHoursTemplateId == 3)
                            {
                                hours = places[placeId].OpeningHoursOther2;
                            }
                        }
                        else if (action.Type == "delete")
                        {

                            hours = "";
                        }

                        ret += await slotRepository.CheckSlots(dayTicks, placeId, hours, action.OpeningHoursTemplateId);
                    }
                }

                return Ok(ret);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Set openning hours for branches.
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListScheduledDays")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<DayTimeManagement>>> ListScheduledDays([FromQuery] string placeId = "")
        {
            try
            {
                var isGlobalAdmin = User.IsAdmin(userRepository);
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("You must be place provider admin to be able to set openning hours");
                }

                IEnumerable<Place> list;
                if (string.IsNullOrEmpty(placeId) || placeId == "__ALL__")
                {
                    list = (await placeRepository.ListAll());
                }
                else
                {
                    list = new List<Place>() { await placeRepository.GetPlace(placeId) };
                }

                if (!isGlobalAdmin)
                {
                    list = list.Where(p => p.PlaceProviderId == User.GetPlaceProvider());
                }
                var ids = list.Select(i => i.Id).Distinct().ToHashSet();

                if (string.IsNullOrEmpty(placeId) || placeId == "__ALL__")
                {
                    if (!list.Any())
                    {
                        throw new Exception("Create place first");
                    }
                }
                else
                {
                    if (!ids.Contains(placeId))
                    {
                        throw new Exception("You are not allowed to manage this place");
                    }
                }

                var ret = new Dictionary<long, DayTimeManagement>();
                foreach (var id in ids)
                {
                    var daySlots = await slotRepository.ListDaySlotsByPlace(id);
                    foreach (var daySlot in daySlots)
                    {
                        if (ret.ContainsKey(daySlot.SlotId))
                        {
                            ret[daySlot.SlotId].Count++;
                            if (!ret[daySlot.SlotId].OpeningHours.Contains(daySlot.OpeningHours))
                            {
                                ret[daySlot.SlotId].OpeningHours.Add(daySlot.OpeningHours);
                            }
                            if (!ret[daySlot.SlotId].OpeningHoursTemplates.Contains(daySlot.OpeningHoursTemplate))
                            {
                                ret[daySlot.SlotId].OpeningHoursTemplates.Add(daySlot.OpeningHoursTemplate);
                            }
                        }
                        else
                        {
                            ret[daySlot.SlotId] = new DayTimeManagement()
                            {
                                SlotId = daySlot.SlotId,
                                Count = 1,
                                Day = daySlot.Time,
                                OpeningHours = new HashSet<string>() { daySlot.OpeningHours },
                                OpeningHoursTemplates = new HashSet<int>() { daySlot.OpeningHoursTemplate }
                            };
                        }
                    }
                }
                return Ok(ret.Values);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// Admin can insert new testing location
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("InsertOrUpdate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Place>> InsertOrUpdate(
            [FromBody] Place place
            )
        {

            try
            {
                var isGlobalAdmin = User.IsAdmin(userRepository);
                if (!isGlobalAdmin && !await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_PlaceController.Only_admin_is_allowed_to_manage_testing_places].Value);
                }

                if (place is null)
                {
                    throw new ArgumentNullException(nameof(place));
                }

                Place oldPlace = null;
                var update = true;
                if (string.IsNullOrEmpty(place.Id))
                {
                    update = false;
                }
                else
                {
                    try
                    {
                        oldPlace = await placeRepository.GetPlace(place.Id);
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, localizer[Controllers_PlaceController.Old_place_not_found].Value);
                    }
                    if (oldPlace == null)
                    {
                        logger.LogInformation(localizer[Controllers_PlaceController.Old_place_not_found].Value);
                        update = false;
                    }
                    else
                    {
                        logger.LogInformation($"Changing place: {Newtonsoft.Json.JsonConvert.SerializeObject(oldPlace)}");
                    }
                }

                logger.LogInformation($"InsertUpdate : {place.Id} {update}");

                if (!update)
                {
                    // new place
                    if (string.IsNullOrEmpty(place.Id))
                    {
                        //setup place id only if we do not wish to setup one
                        place.Id = Guid.NewGuid().ToString();
                    }
                    place.PlaceProviderId = User.GetPlaceProvider();
                    place = await placeRepository.SetPlace(place);
                    logger.LogInformation($"Place {place.Name} has been created");
                }
                else
                {
                    // update existing
                    if (!isGlobalAdmin && string.IsNullOrEmpty(place.PlaceProviderId))
                    {
                        throw new Exception("You are not allowed to manage this place");
                    }

                    if (!isGlobalAdmin && place.PlaceProviderId != User.GetPlaceProvider())
                    {
                        throw new Exception("You are not allowed to manage this place");
                    }

                    place.Healthy = oldPlace.Healthy;
                    place.Sick = oldPlace.Sick;
                    if (string.IsNullOrEmpty(place.PlaceProviderId))
                    {
                        place.PlaceProviderId = User.GetPlaceProvider();
                    }
                    place.Registrations = oldPlace.Registrations;
                    place.OtherLimitations = oldPlace.OtherLimitations;
                    place = await placeRepository.SetPlace(place);
                    logger.LogInformation($"Place {place.Name} has been updated");
                }

                return Ok(place);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Limit specific place
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="from"></param>
        /// <param name="until"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetPlaceLimitation")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PlaceLimitation>> SetPlaceLimitation(
            [FromForm] string placeId,
            [FromForm] DateTimeOffset from,
            [FromForm] DateTimeOffset until,
            [FromForm] int limit
            )
        {

            try
            {
                var isGlobalAdmin = User.IsAdmin(userRepository);
                if (!isGlobalAdmin && !await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_PlaceController.Only_admin_is_allowed_to_manage_testing_places].Value);
                }

                var place = await placeRepository.GetPlace(placeId);

                if (place is null)
                {
                    throw new ArgumentNullException(nameof(placeId));
                }

                var existing = place.OtherLimitations?.FirstOrDefault(l => l.From == from && l.Until == until);
                if (existing != null)
                {
                    existing.HourLimit = limit;
                }
                else
                {
                    var limitation = new PlaceLimitation()
                    {
                        Id = Guid.NewGuid().ToString(),
                        From = from,
                        Until = until,
                        HourLimit = limit,
                        PlaceId = placeId
                    };
                    place.OtherLimitations.Add(limitation);
                }
                await placeRepository.SetPlace(place);

                return Ok(place);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Delete place limitation
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="limitId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DeletePlaceLimitation")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PlaceLimitation>> DeletePlaceLimitation(
            [FromForm] string placeId,
            [FromForm] string limitId
            )
        {

            try
            {
                var isGlobalAdmin = User.IsAdmin(userRepository);
                if (!isGlobalAdmin && !await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_PlaceController.Only_admin_is_allowed_to_manage_testing_places].Value);
                }

                var place = await placeRepository.GetPlace(placeId);

                if (place is null)
                {
                    throw new ArgumentNullException(nameof(placeId));
                }

                var item = place.OtherLimitations.FirstOrDefault(l => l.Id == limitId);
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(limitId));
                }
                place.OtherLimitations.Remove(item);

                await placeRepository.SetPlace(place);

                return Ok(item);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }


        /// <summary>
        /// Admin can delete testing location
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("Delete")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Place>> Delete(
            [FromBody] Place place
            )
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_PlaceController.Only_admin_is_allowed_to_manage_testing_places].Value);
                }

                if (place is null)
                {
                    throw new ArgumentNullException(nameof(place));
                }

                if (string.IsNullOrEmpty(place.Id) || await placeRepository.GetPlace(place.Id) == null)
                {
                    // new place
                    throw new Exception(localizer[Controllers_PlaceController.Place_not_found].Value);
                }
                else
                {
                    // update existing

                    await placeRepository.Delete(place);
                    logger.LogInformation($"Place {place.Name} has been deleted");
                }

                return Ok(place);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }


        /// <summary>
        /// Creates product at specified place with special price valid from until as specified in PlaceProduct object 
        /// </summary>
        /// <param name="placeProduct"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("InsertOrUpdatePlaceProduct")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PlaceProduct>> InsertOrUpdatePlaceProduct(
            [FromBody] PlaceProduct placeProduct
            )
        {

            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_PlaceController.Only_admin_is_allowed_to_manage_testing_places].Value);
                }

                var place = await placeRepository.GetPlace(placeProduct.PlaceId);
                if (place == null)
                {
                    throw new Exception("Place not found");
                }

                if (place.PlaceProviderId != User.GetPlaceProvider())
                {
                    throw new Exception("You can define product only for your places");
                }

                var update = true;
                if (string.IsNullOrEmpty(placeProduct.Id))
                {
                    update = false;
                }

                logger.LogInformation($"InsertOrUpdatePlaceProduct : {placeProduct.PlaceId} {update}");

                var products = await placeProviderRepository.ListProducts(User.GetPlaceProvider());
                var product = products.FirstOrDefault(p => p.Id == placeProduct.ProductId);
                if (product == null)
                {
                    throw new Exception("Product not found");
                }

                if (!update)
                {
                    // new place
                    placeProduct.Id = Guid.NewGuid().ToString();
                    placeProduct.PlaceProviderId = User.GetPlaceProvider();
                    placeProduct.InsuranceOnly = product.InsuranceOnly;
                    placeProduct.CollectInsurance = product.CollectInsurance;
                    placeProduct.SchoolOnly = product.SchoolOnly;
                    placeProduct.EmployeesOnly = product.EmployeesOnly;
                    placeProduct.EHealthDefault = product.EHealthDefault;
                    placeProduct.EmployeesRegistration = product.EmployeesRegistration;
                    placeProduct = await placeRepository.SetProductPlace(placeProduct);
                    logger.LogInformation($"ProductPlace {place.Name} has been created");
                }
                else
                {
                    // update existing
                    if (placeProduct.PlaceProviderId != User.GetPlaceProvider())
                    {
                        throw new Exception("You can define place products only for your places");
                    }

                    var oldPlaceProduct = await placeRepository.GetPlaceProduct(placeProduct.Id);

                    placeProduct.InsuranceOnly = product.InsuranceOnly;
                    placeProduct = await placeRepository.SetProductPlace(placeProduct);

                    logger.LogInformation($"Place {place.Name} has been updated");
                }

                return Ok(placeProduct);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Admin can delete testing location
        /// </summary>
        /// <param name="placeProductid"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DeletePlaceProduct")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> DeletePlaceProduct(
            [FromForm] string placeProductid
            )
        {
            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_PlaceController.Only_admin_is_allowed_to_manage_testing_places].Value);
                }

                var productPlace = await placeRepository.GetPlaceProduct(placeProductid);
                if (productPlace == null)
                {
                    throw new Exception("Place not found");
                }

                if (productPlace.PlaceProviderId != User.GetPlaceProvider())
                {
                    throw new Exception("You can define product only for your places");
                }

                return Ok(await placeRepository.DeletePlaceProduct(productPlace));
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
    }
}
