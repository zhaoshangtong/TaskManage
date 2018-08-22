using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManage
{
    class MonitorJobListener : IJobListener
    {
        public string Name
        {
            get
            {
                return "MonitorJobListener";
            }
        }

        public void JobExecutionVetoed(IJobExecutionContext context)
        {
            LogHelper.Info($@"【{context.JobDetail.Key.Name}】 JobExecutionVetoed ");
        }

        public void JobToBeExecuted(IJobExecutionContext context)
        {
            LogHelper.Info($@"【{context.JobDetail.Key.Name}】 JobToBeExecuted ");
        }

        public void JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException)
        {
            LogHelper.Info($@"【{context.JobDetail.Key.Name}】 JobWasExecuted ");
        }
    }
}
