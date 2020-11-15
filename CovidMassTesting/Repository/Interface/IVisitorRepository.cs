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
        public Task<Visitor> Set(Visitor visitor, bool mustBeNew);
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
        public Task<Visitor> GetVisitor(int codeInt);
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
        /// <param name="rc"></param>
        /// <returns></returns>
        public Task<Visitor> GetVisitorByPersonalNumber(string rc);
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
    }
}
