using Quartz;
using RaysCloud.Common;

namespace TaskManage
{
    public class SimpleJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            LogHelper.Info("SimpleJob");
        }
    }
}
