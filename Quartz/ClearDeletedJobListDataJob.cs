using Quartz;
using System;

namespace TaskManage
{
    class ClearDeletedJobListDataJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                LogHelper.Info("ClearDeletedJobListDataJob Start");
                // 删除 2 天前的数据
                QuartzManager.deletedJobList.RemoveAll(x => DateTime.Parse(x.time.ToString()) < DateTime.Now.AddDays(-2));
            }
            catch (Exception ex)
            {
                LogHelper.Error("ClearDeletedJobListDataJob 主任务错误,清理 DeletedJobList 时发生错误", ex);
            }
        }
    }
}
