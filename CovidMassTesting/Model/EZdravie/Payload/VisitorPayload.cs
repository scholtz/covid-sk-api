using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie.Payload
{
    public class VisitorPayload
    {
        public string AddressNote { get; set; }
        public string Age { get; set; }
        public string BirthNumber { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string DailyCapacity { get; set; }
        public string DataSource { get; set; }
        public string DesignatedDriveinCity { get; set; }
        public string DesignatedLatitude { get; set; }
        public string DesignatedLongitude { get; set; }
        [JsonConverter(typeof(Helpers.CustomDateTimeConverter))]
        public DateTimeOffset? DesignatedDriveinScheduledAt { get; set; }
        public string DesignatedDriveinStreetName { get; set; }
        public string DesignatedDriveinStreetNumber { get; set; }
        public string DesignatedDriveinZipCode { get; set; }
        public string DriveinId { get; set; }
        [JsonConverter(typeof(Helpers.CustomDateTimeConverter))]
        public DateTimeOffset? EntryFromAbroadPlannedAt { get; set; }
        public string FirstName { get; set; }
        public string Gender { get; set; }
        public string HasComeFromCountry { get; set; }
        public string HasPlannedOperation { get; set; }
        public string HealthInsuranceCompany { get; set; }
        public string Id { get; set; }
        public string LastName { get; set; }
        public string Municipality { get; set; }
        public string OtherSymptoms { get; set; }
        public string PersonTrackingNumber { get; set; }
        public string PostalCode { get; set; }
        public string PrimaryPhone { get; set; }
        public string QuarantineAddressNote { get; set; }
        public string QuarantineCountry { get; set; }
        public string QuarantineMunicipality { get; set; }
        public string QuarantinePostalCode { get; set; }
        public string QuarantineStreet { get; set; }
        public string QuarantineStreetNumber { get; set; }
        public string State { get; set; }
        public string StateColor { get; set; }
        public string StateDescription { get; set; }
        public string StateIcon { get; set; }
        public string StateName { get; set; }
        public string Street { get; set; }
        public string StreetNumber { get; set; }
        public string TemporaryAddressNote { get; set; }
        public string TemporaryCountry { get; set; }
        public string TemporaryMunicipality { get; set; }
        public string TemporaryPostalCode { get; set; }
        public string TemporaryStreet { get; set; }
        public string TemporaryStreetNumber { get; set; }
        public string Title { get; set; }

    }
}
