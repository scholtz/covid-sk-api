using CovidMassTesting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <returns></returns>
        public Task<Visitor> Add(Visitor visitor);
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
        /// <returns></returns>
        public Task<string> ConnectVisitorToTest(int codeInt, string testCodeClear);
        /// <summary>
        /// Load visitor
        /// </summary>
        /// <param name="codeInt"></param>
        /// <returns></returns>
        public Task<Visitor> GetVisitor(int codeInt, bool fixOnLoad = true);
        /// <summary>
        /// Public registration
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="managerEmail"></param>
        /// <returns></returns>
        public Task<Visitor> Register(Visitor visitor, string managerEmail);

        /// <summary>
        /// Set test result
        /// </summary>
        /// <param name="testCode"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public Task<Result> SetTestResult(string testCode, string result);
        /// <summary>
        /// Get visitor by personal number
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <returns></returns>
        public Task<Visitor> GetVisitorByPersonalNumber(string personalNumber);
        /// <summary>
        /// Document manager can fetch one visitor from executed tests
        /// </summary>
        /// <returns></returns>
        public Task<Visitor> GetNextTest();
        /// <summary>
        /// Removes test from test queue and mark test as taken
        /// </summary>
        /// <param name="testId"></param>
        /// <returns></returns>
        public Task<bool> RemoveFromDocQueueAndSetTestStateAsTaken(string testId);
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public Task<int> DropAllData();
        /// <summary>
        /// Lists all sick visitors
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Visitor>> ListSickVisitors(int from = 0, int count = 9999999);
        /// <summary>
        /// This method exports all visitors who are in state in processing
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Visitor>> ListVisitorsInProcess(int from = 0, int count = 9999999);
        /// <summary>
        /// This method exports all visitors who are in state in processing
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Visitor>> ListAllVisitorsWhoDidNotCome(int from = 0, int count = 9999999);

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
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Task<IEnumerable<VisitorSimplified>> ProofOfWorkExport(int from = 0, int count = 9999999);
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
        /// <returns></returns>
        public string GenerateResultHTML(Visitor visitor, string testingEntity, string placeAddress, string product, string resultguid);
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
        /// <returns></returns>
        public byte[] GenerateResultPDF(Visitor visitor, string testingEntity, string placeAddress, string product, string resultguid);

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
        /// Fix year
        /// </summary>
        /// <returns></returns>
        public Task<int> FixBirthYear();

        public Task<bool> ProcessSingle();

        public Task<Result> SetResultObject(Result result, bool mustBeNew);
    }
}
