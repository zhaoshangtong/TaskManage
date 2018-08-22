using Quartz;
using System;

namespace TaskManage
{
    class MonitorSchedulerListener : ISchedulerListener
    {
        public void JobAdded(IJobDetail jobDetail)
        {
            LogHelper.Info($@"【{jobDetail.Key.Name}】 JobAdded ");
        }

        /// <summary>
        /// 达到 结束时间时触发
        /// </summary>
        /// <param name="jobKey"></param>
        public void JobDeleted(JobKey jobKey)
        {
            QuartzManager.deletedJobList.Add(new { job_name = jobKey.Name, time = DateTime.Now });
            QuartzManager.db.ExcuteSQL($@"update {QuartzManager.table_name} set handler='quartz',status='DELETED' where job_name='{jobKey.Name}'");
        }

        public void JobPaused(JobKey jobKey)
        {
            LogHelper.Info($@"【{jobKey.Name}】 JobPaused ");
        }

        public void JobResumed(JobKey jobKey)
        {
            LogHelper.Info($@"【{jobKey.Name}】 JobResumed ");
        }

        public void JobScheduled(ITrigger trigger)
        {
            LogHelper.Info($@"【{trigger.Key.Name}】 JobScheduled ");
        }

        public void JobsPaused(string jobGroup)
        {
            LogHelper.Info($@"【{jobGroup}】 JobsPaused ");
        }

        public void JobsResumed(string jobGroup)
        {
            LogHelper.Info($@"【{jobGroup}】 JobsResumed ");
        }

        public void JobUnscheduled(TriggerKey triggerKey)
        {
            LogHelper.Info($@"【{triggerKey.Name}】 JobUnscheduled ");
        }

        public void SchedulerError(string msg, SchedulerException cause)
        {
            LogHelper.Info($@"【{msg}】 SchedulerError ");
        }

        public void SchedulerInStandbyMode()
        {
            LogHelper.Info($@"SchedulerInStandbyMode ");
        }

        public void SchedulerShutdown()
        {
            LogHelper.Info($@" SchedulerShutdown ");
        }

        public void SchedulerShuttingdown()
        {
            LogHelper.Info($@" SchedulerShuttingdown ");
        }

        public void SchedulerStarted()
        {
            LogHelper.Info($@" SchedulerStarted ");
        }

        public void SchedulerStarting()
        {
            LogHelper.Info($@" SchedulerStarting ");
        }

        public void SchedulingDataCleared()
        {
            LogHelper.Info($@" SchedulingDataCleared ");
        }

        public void TriggerFinalized(ITrigger trigger)
        {
            LogHelper.Info($@"【{trigger.Key.Name}】 TriggerFinalized ");
        }

        public void TriggerPaused(TriggerKey triggerKey)
        {
            LogHelper.Info($@"【{triggerKey.Name}】 TriggerPaused ");
        }

        public void TriggerResumed(TriggerKey triggerKey)
        {
            LogHelper.Info($@"【{triggerKey.Name}】 TriggerResumed ");
        }

        public void TriggersPaused(string triggerGroup)
        {
            LogHelper.Info($@"【{triggerGroup}】 TriggersPaused ");
        }

        public void TriggersResumed(string triggerGroup)
        {
            LogHelper.Info($@"【{triggerGroup}】 TriggersResumed ");
        }
    }
}
