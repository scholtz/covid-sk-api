using CovidMassTesting.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.Interface
{
    /// <summary>
    /// Visitor repository interface for dependency injection
    /// </summary>
    public interface IVisitorRepository
    {
        /// <summary>
        /// Creates new visitor
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="notify">if notify is set to false, it will not send email or sms</param>
        /// <returns></returns>
        public Task<Visitor> Add(Visitor visitor, bool notify);
        /// <summary>
        /// Sets the visitor to the database
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public Task<Visitor> SetVisitor(Visitor visitor, bool mustBeNew);
        /// <summary>
        /// Get test result for public
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public Task<Result> GetTest(int code, string pass);
        /// <summary>
        /// Removes test on wish of user when visitor was tested negative
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public Task<bool> RemoveTest(int code, string pass);
        /// <summary>
        /// Update test state of visitor
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public Task<bool> UpdateTestingState(int code, string state);
        /// <summary>
        /// Bind visitor to specific test
        /// </summary>
        /// <param name="codeInt"></param>
        /// <param name="testCodeClear"></param>
        /// <param name="adminWorker"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public Task<string> ConnectVisitorToTest(int codeInt, string testCodeClear, string adminWorker, string ipAddress);
        /// <summary>
        /// Load visitor
        /// </summary>
        /// <param name="codeInt"></param>
        /// <param name="fixOnLoad"></param>
        /// <returns></returns>
        public Task<Visitor> GetVisitor(int codeInt, bool fixOnLoad = true);
        /// <summary>
        /// Public registration
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="managerEmail"></param>
        /// <param name="notify">If notify is set to false, it does not send email or sms</param>
        /// <returns></returns>
        public Task<Visitor> Register(Visitor visitor, string managerEmail, bool notify);

        /// <summary>
        /// Set test result
        /// </summary>
        /// <param name="testCode"></param>
        /// <param name="result"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        public Task<Result> SetTestResult(string testCode, string result, bool isAdmin);
        /// <summary>
        /// Get visitor by personal number
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <param name="nullOnMissing">If true returns null not found, if false throws exception</param>
        /// <returns></returns>
        public Task<Visitor> GetVisitorByPersonalNumber(string personalNumber, bool nullOnMissing = false);
        /// <summary>
        /// Document manager can fetch one visitor from executed tests
        /// </summary>
        /// <returns></returns>
        public Task<Visitor> GetNextTest();
        /// <summary>
        /// Removes test from test queue and mark test as taken
        /// </summary>
        /// <param name="testId"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        public Task<bool> RemoveFromDocQueueAndSetTestStateAsTaken(string testId, bool isAdmin);
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public Task<int> DropAllData();
        /// <summary>
        /// Lists all sick visitors
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Visitor>> ListSickVisitors(DateTimeOffset? day = null, int from = 0, int count = 9999999);
        /// <summary>
        /// Lists all tested visitors
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Visitor>> ListTestedVisitors(DateTimeOffset? day = null, int from = 0, int count = 9999999);
        /// <summary>
        /// ListAnonymizedVisitors
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<VisitorAnonymized>> ListAnonymizedVisitors(DateTimeOffset? day = null, int from = 0, int count = 9999999);
        /// <summary>
        /// This method exports all visitors who are in state in processing
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Visitor>> ListVisitorsInProcess(DateTimeOffset? day = null, int from = 0, int count = 9999999);
        /// <summary>
        /// This method exports all visitors who are in state in processing
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Visitor>> ListAllVisitorsWhoDidNotCome(DateTimeOffset? day = null, int from = 0, int count = 9999999);
        /// <summary>
        /// List all
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Task<IEnumerable<Visitor>> ListAllVisitors(DateTimeOffset? day = null, int from = 0, int count = 9999999);
        /// <summary>
        /// List all at place
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="fromRegTime"></param>
        /// <param name="untilRegTime"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Task<IEnumerable<Visitor>> ListAllVisitorsAtPlace(
            string placeId,
            DateTimeOffset fromRegTime,
            DateTimeOffset untilRegTime,
            int from = 0,
            int count = 9999999
            );

        /// <summary>
        /// ProofOfWorkExport
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <param name="filterPlaces"></param>
        /// <returns></returns>
        public Task<IEnumerable<VisitorSimplified>> ProofOfWorkExport(DateTimeOffset? day = null, int from = 0, int count = 9999999, HashSet<string> filterPlaces = null);

        /// <summary>
        /// ListExportableDays
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<DateTimeOffset>> ListExportableDays();
        /// <summary>
        /// Test storage
        /// </summary>
        /// <returns></returns>
        public Task<int> TestStorage();
        /// <summary>
        /// Creates html source code for pdf generation
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="testingEntity"></param>
        /// <param name="placeAddress"></param>
        /// <param name="product"></param>
        /// <param name="resultguid"></param>
        /// <param name="oversight"></param>
        /// <returns></returns>
        public string GenerateResultHTML(Visitor visitor, string testingEntity, string placeAddress, string product, string resultguid, string oversight = "");
        /// <summary>
        /// Enqueue visitor
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public Task<bool> Enqueued(int code, string pass);

        /// <summary>
        /// Creates pdf from test result
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="testingEntity"></param>
        /// <param name="placeAddress"></param>
        /// <param name="product"></param>
        /// <param name="resultguid"></param>
        /// <param name="sign">Sign and password protect</param>
        /// <param name="oversight"></param>
        /// <returns></returns>
        public byte[] GenerateResultPDF(Visitor visitor, string testingEntity, string placeAddress, string product, string resultguid, bool sign = true, string oversight = "");

        /// <summary>
        /// Decode visitor data from database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<VerificationData> GetResultVerification(string id);
        /// <summary>
        /// Generate and sign PDF with test result
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public Task<byte[]> GetPublicPDF(int code, string pass);
        /// <summary>
        /// Generate unsigned PDF for printing usage
        /// </summary>
        /// <param name="code"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public Task<byte[]> GetResultPDFByEmployee(int code, string user);

        /// <summary>
        /// Allow person to request one resend for free
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public Task<bool> ResendResults(int code, string pass);
        /// <summary>
        /// Temp fix
        /// </summary>
        /// <returns></returns>
        public Task<bool> Fix01();
        /// <summary>
        /// Temp fix
        /// </summary>
        /// <returns></returns>
        public Task<bool> Fix02();
        /// <summary>
        /// Temp fix
        /// </summary>
        /// <returns></returns>
        public Task<bool> Fix03();
        /// <summary>
        /// FixStats
        /// </summary>
        /// <returns></returns>
        public Task<int> FixStats();
        /// <summary>
        /// Fix rc map
        /// </summary>
        /// <returns></returns>
        public Task<int> FixVisitorRC();
        /// <summary>
        /// Fix rc map
        /// </summary>
        /// <returns></returns>
        public Task<int> FixTestingTime();
        /// <summary>
        /// Returns number of corrected verification data
        /// </summary>
        /// <returns></returns>
        public Task<int> FixVerificationData();
        /// <summary>
        /// FixSendRegistrationSMS
        /// </summary>
        /// <returns></returns>
        public Task<int> FixSendRegistrationSMS();
        /// <summary>
        /// FixMapVisitorToDay
        /// </summary>
        /// <returns></returns>
        public Task<int> FixMapVisitorToDay();
        /// <summary>
        /// Fix year
        /// </summary>
        /// <returns></returns>
        public Task<int> FixBirthYear();
        /// <summary>
        /// Fix place by person id ad day
        /// </summary>
        /// <param name="day"></param>
        /// <param name="newPlaceId"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public Task<int> FixPersonPlace(string day, string newPlaceId, string user);
        /// <summary>
        /// Process single result
        /// </summary>
        /// <returns></returns>
        public Task<bool> ProcessSingle();
        /// <summary>
        /// Set result
        /// </summary>
        /// <param name="result"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public Task<Result> SetResultObject(Result result, bool mustBeNew);
        /// <summary>
        /// Format personal number or passport
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string FormatDocument(string input);
        /// <summary>
        /// Long term Registration
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public Task<Registration> SetRegistration(Registration registration, bool mustBeNew);
        /// <summary>
        /// Make hash from company personal number
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        public string MakeCompanyPeronalNumberHash(string companyId, string employeeId);
        /// <summary>
        /// RegistrationId From Hashed Id
        /// </summary>
        /// <param name="hashedId"></param>
        /// <returns></returns>
        public Task<string> GetRegistrationIdFromHashedId(string hashedId);
        /// <summary>
        /// Loads registration
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<Registration> GetRegistration(string id);
        /// <summary>
        /// GETVisitorCodeFromTesting code
        /// </summary>
        /// <param name="testCodeClear"></param>
        /// <returns></returns>
        public Task<int?> GETVisitorCodeFromTesting(string testCodeClear);
        /// <summary>
        /// Company registrations export
        /// </summary>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Task<IEnumerable<Registration>> ExportRegistrations(int from = 0, int count = 9999999);
    }
}
