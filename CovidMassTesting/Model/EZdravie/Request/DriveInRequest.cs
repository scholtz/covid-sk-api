using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie.Request
{
    public class DriveInRequest
    {
        [JsonProperty("designated_drivein_city")]
        public string DesignatedDriveinCity { get; set; }
        [JsonProperty("designated_drivein_id")]
        public string DesignatedDriveinId { get; set; }
        [JsonProperty("designated_drivein_latitude")]
        public string DesignatedDriveinLatitude { get; set; }
        [JsonProperty("designated_drivein_longitude")]
        public string DesignatedDriveinLongitude { get; set; }
        [JsonProperty("designated_drivein_operated_by")]
        public string DesignatedDriveinOperated_by { get; set; } = null;
        [JsonProperty("designated_drivein_scheduled_at")]
        public string DesignatedDriveinScheduledAt { get; set; }
        [JsonProperty("designated_drivein_street_name")]
        public string DesignatedDriveinStreetName { get; set; }
        [JsonProperty("designated_drivein_street_number")]
        public string DesignatedDriveinStreetNumber { get; set; }
        [JsonProperty("designated_drivein_title")]
        public string DesignatedDriveinTitle { get; set; }
        [JsonProperty("designated_drivein_zip_code")]
        public string DesignatedDriveinZipCode { get; set; }
        [JsonProperty("drivein_entered_at")]
        public string DriveinEnteredAt { get; set; } = null;
        [JsonProperty("drivein_left_at")]
        public string DriveinLeftAt { get; set; } = null;
        [JsonProperty("medical_assessed_at")]
        public string MedicalAssessedAt { get; set; }
        [JsonProperty("state")]
        public string State { get; set; } = "SD";
        [JsonProperty("triage")]
        public string Triage { get; set; } = "2";

    }
}
