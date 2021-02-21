using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.ScheduledTasks
{
    /// <summary>
    /// Scheduler tasks
    /// </summary>
    public interface IScheduledTask
    {
        /// <summary>
        /// Process
        /// </summary>
        /// <returns></returns>
        public Task<bool> Process();
    }
}
