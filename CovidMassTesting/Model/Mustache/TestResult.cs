using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Mustache
{
    /// <summary>
    /// Variables for PDF generation
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// BirthDay - day
        /// </summary>
        public int? BirthDayDay { get; set; }
        /// <summary>
        /// BirthDay - month
        /// </summary>
        public int? BirthDayMonth { get; set; }
        /// <summary>
        /// BirthDay - year
        /// </summary>
        public int? BirthDayYear { get; set; }
        /// <summary>
        /// Address where user has pernament residence
        /// </summary>
        public string PernamentAddress { get; set; }
        /// <summary>
        /// Base64 encoded signature picture in png
        /// </summary>
        public string Signature { get; set; }
        /// <summary>
        /// Result in text
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Test result in english
        /// </summary>
        public string TextEN { get; set; }
        public string Category { get; set; }
        public string CategoryEN { get; set; }
        public string DGCQR { get; set; }
        public string TestPurpose { get; set; }
        public string Country { get; set; }
        public string DgcIssuer { get; set; }
        /// <summary>
        /// Id of the test from product
        /// </summary>
        public string TestBrandName { get; set; }
        /// <summary>
        /// TestManufacturer
        /// </summary>
        public string TestManufacturer { get; set; }
        /// <summary>
        /// SampleOrigin
        /// </summary>
        public string SampleOrigin { get; set; }
        /// <summary>
        /// Result help
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// DescriptionEN
        /// </summary>
        public string DescriptionEN { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Personal number or insurance number
        /// </summary>
        public string PersonalNumber { get; set; }
        /// <summary>
        /// Passport number for foreigners
        /// </summary>
        public string PassportNumber { get; set; }
        /// <summary>
        /// DateTime
        /// </summary>
        public string Date { get; set; }
        /// <summary>
        /// DateResult
        /// </summary>
        public string DateResult { get; set; }
        /// <summary>
        /// DateResult
        /// </summary>
        public string DateResultEN { get; set; }
        /// <summary>
        /// DateTime en
        /// </summary>
        public string DateEN { get; set; }
        /// <summary>
        /// Link to testing place
        /// </summary>
        public string FrontedURL { get; set; }
        /// <summary>
        /// Link for verification url
        /// </summary>
        public string VerifyURL { get; set; }
        /// <summary>
        /// QR code for faster check lookup
        /// </summary>
        public string QRVerificationURL { get; set; }
        /// <summary>
        /// Identification of testing provider
        /// </summary>
        public string TestingEntity { get; set; }
        /// <summary>
        /// Address where user has been tested
        /// </summary>
        public string TestingAddress { get; set; }
        /// <summary>
        /// Product - PCR or Antigen test
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// ResultGUID
        /// </summary>
        public string ResultGUID { get; set; }
        /// <summary>
        /// Oversight
        /// </summary>
        public string Oversight { get; set; }
        /// <summary>
        /// Eu Flag above DGC
        /// </summary>
        public string EuFlag { get; set; }
    }
}
