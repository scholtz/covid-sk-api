using CovidMassTesting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.Interface
{
    public interface IVisitorRepository
    {
        public Task<Visitor> Add(Visitor visitor);
        public Task<Visitor> Set(Visitor visitor, bool mustBeNew);
        public Task<Result> GetTest(int code, string pass);
        public Task<bool> UpdateTestingState(int code, string state);
        public Task<string> ConnectVisitorToTest(int codeInt, string testCodeClear);
        public Task<Visitor> GetVisitor(int codeInt);
        public Task<Result> SetTestResult(string testCode, string result);
        public Task<Visitor> GetVisitorByPersonalNumber(string rc);
    }
}
