using Quartz;
using Quartz.Impl;
using RaysCloud.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;

namespace TaskManage
{
    public class QuartzManager
    {
        public static DataHelper db = new DataHelper("db_live");
        public static string table_name = "job_detail";
        static IScheduler _scheduler;

        // 上次数据的更新标识
        public static Dictionary<string, string> dicIndentify = new Dictionary<string, string>();
        /* 已删除的任务列表
            （比如：达到 end_time 的时间的）
                没有达到 end_time 的任务也可能会被删除，Quartz会检测下一次运行任务的时间是否在 end_time 之前，如果在 end_time 之前已经不够执行一次任务了，任务也会被删除掉
                例如：每隔 10 秒运行一次任务，结束时间在 2017-10-10 10:10:15 那么任务会在 2017-10-10 10:10:10 的时候运行一次后就被删除掉，因为在到达 2017-10-10 10:10:15 的时候已经不够运行下次任务了
        */
        public static List<dynamic> deletedJobList = new List<dynamic>();

        // Quartz Scheduler
        public static IScheduler Scheduler
        {
            get
            {
                if (_scheduler == null)
                {
                    _scheduler = new StdSchedulerFactory().GetScheduler();
                    //_scheduler.ListenerManager.AddJobListener(new MonitorJobListener());
                    //_scheduler.ListenerManager.AddTriggerListener(new MonitorTriggerListener());
                    _scheduler.ListenerManager.AddSchedulerListener(new MonitorSchedulerListener());
                }
                return _scheduler;
            }
            set
            {
                _scheduler = value;
            }
        }

        public static IList<TriggerKey> triggerKeys { get; set; }

        #region Quartz.net 分布式集群配置
        /// <summary>
        /// Quartz.net 分布式集群配置
        /// </summary>
        /// <returns></returns>
        public static NameValueCollection SetJobDistributedCluster()
        {
            NameValueCollection properties = new NameValueCollection();
            // 驱动类型,目前支持如下驱动：
            //Quartz.Impl.AdoJobStore.FirebirdDelegate
            //Quartz.Impl.AdoJobStore.MySQLDelegate
            //Quartz.Impl.AdoJobStore.OracleDelegate
            //Quartz.Impl.AdoJobStore.SQLiteDelegate
            //Quartz.Impl.AdoJobStore.SqlServerDelegate
            properties["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.SqlServerDelegate, Quartz";

            // 数据源名称
            properties["quartz.jobStore.dataSource"] = "myDS";

            // 数据库版本
            /* 数据库版本    MySql.Data.dll版本,二者必须保持一致
             * MySql-10    1.0.10.1
             * MySql-109   1.0.9.0
             * MySql-50    5.0.9.0
             * MySql-51    5.1.6.0
             * MySql-65    6.5.4.0
             * MySql-695   6.9.5.0
             *             System.Data
             * SqlServer-20         2.0.0.0
             * SqlServerCe-351      3.5.1.0
             * SqlServerCe-352      3.5.1.50
             * SqlServerCe-400      4.0.0.0
             * 其他还有OracleODP，Npgsql，SQLite，Firebird，OleDb
            */

            properties["quartz.dataSource.myDS.connectionString"] = @"Data Source=wechat.changdelao.net;Initial Catalog=BlueseaCloud_JobListen;User ID=sa;Password=hifun!@#qwe";
            properties["quartz.dataSource.myDS.provider"] = "SqlServer-20";

            //properties["quartz.dataSource.myDS.provider"] = "SqlServerCe-400";
            //// 连接字符串
            //properties["quartz.dataSource.myDS.connectionString"] = "Data Source=wechat.changdelao.net;Database=BlueseaCloud_JobListen;uid=sa;pwd=hifun!@#qwe";
            // 事物类型JobStoreTX自动管理 JobStoreCMT应用程序管理
            properties["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz";
            // 表名前缀
            properties["quartz.jobStore.tablePrefix"] = "QRTZ_";
            // Quartz Scheduler唯一实例ID，auto：自动生成
            properties["quartz.scheduler.instanceId"] = "AUTO";
            // 集群
            properties["quartz.jobStore.clustered"] = "true";
            return properties;
        }
        #endregion

        #region 添加任务
        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="jobDetail"></param>
        /// <returns></returns>
        public static void Add(JobDetail jobDetail)
        {
            if (Scheduler.CheckExists(GetJobKey(jobDetail)))
            {
                throw new DataExistsException("存在同名任务");
            }

            // 创建一个任务
            IJobDetail job = JobBuilder.Create()
                .WithIdentity(jobDetail.job_name, GetGroupNameOrDefault(jobDetail.group_name))
                .WithDescription(jobDetail.job_description)
                .OfType(Type.GetType(jobDetail.job_classname.Split(',')[0]))
                .UsingJobData("JobData", jobDetail.job_data)
                .Build();
            if (Util.isNotNull(jobDetail.cron))
            {
                // 触发器
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity(jobDetail.job_name, GetGroupNameOrDefault(jobDetail.group_name))
                    .WithDescription(jobDetail.job_description)
                    .WithCronSchedule(jobDetail.cron,x=> 
                    {
                        x.WithMisfireHandlingInstructionDoNothing();//不立即执行
                    })
                    //结束执行
                    .EndAt(jobDetail.end_time == DateTime.MinValue ? null : (DateTimeOffset?)new DateTimeOffset(jobDetail.end_time))
                    .Build();

                // 添加 Job
                Scheduler.ScheduleJob(job, trigger);
            }
            else if (!Util.isNotNull(jobDetail.cron) && Util.isNotNull(jobDetail.start_time))
            {
                // 触发器(简单触发器：执行一次)
                ISimpleTrigger trigger = (ISimpleTrigger)TriggerBuilder.Create().StartAt(new DateTimeOffset(jobDetail.start_time))
                    .EndAt(jobDetail.end_time == DateTime.MinValue ? null : (DateTimeOffset?)new DateTimeOffset(jobDetail.end_time))
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).WithRepeatCount(0)).Build();
                // 添加 Job
                Scheduler.ScheduleJob(job, trigger);
            }

            // 立即运行一次
            if (jobDetail.is_start_now)
            {
                Scheduler.TriggerJob(GetJobKey(jobDetail));
            }
        }
        #endregion

        #region 删除任务
        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="groupName">任务分组名称</param>
        /// <returns></returns>
        public static void Delete(string jobName, string groupName = "")
        {
            //var jobKey = JobKey.Create(jobName, groupName.IsNullOrWhiteSpace() ? TriggerKey.DefaultGroup : groupName);
            //var triggerKey = new TriggerKey(jobName, groupName.IsNullOrWhiteSpace() ? TriggerKey.DefaultGroup : groupName);
            //Scheduler.PauseTrigger(triggerKey);
            //Scheduler.UnscheduleJob(triggerKey);
            Scheduler.DeleteJob(GetJobKey(jobName, groupName));
        }
        #endregion

        #region 删除触发器
        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="groupName">任务分组名称</param>
        /// <returns></returns>
        public static void DeleteTigger(string triggerName, string groupName = "")
        {
            var triggerKey = GetTriggerKey(triggerName, groupName);
            Scheduler.PauseTrigger(triggerKey);
            Scheduler.UnscheduleJob(triggerKey);
        }
        #endregion

        #region 暂停任务
        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="groupName">任务组名称</param>
        public static void Pause(string jobName, string groupName = "")
        {
            Scheduler.PauseJob(GetJobKey(jobName, groupName));
        }
        #endregion

        #region 恢复任务
        /// <summary>
        /// 恢复任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="groupName">任务组名称</param>
        public static void Resume(string jobName, string groupName = "")
        {
            Scheduler.ResumeJob(GetJobKey(jobName, groupName));
        }
        #endregion

        #region 修改任务
        /// <summary>
        /// 修改任务
        /// </summary>
        /// <returns></returns>
        public static void Modify(JobDetail jobDetail)
        {
            var trigger = Scheduler.GetTrigger(GetTriggerKey(jobDetail));
            if (trigger == null)
            {
                throw new NotFoundException("没有找到该任务");
            }

            // 修改 Job
            // 得到任务
            var oldJobDetail = Scheduler.GetJobDetail(trigger.JobKey);
            // 修改数据
            oldJobDetail = oldJobDetail.GetJobBuilder()
                .WithDescription(jobDetail.job_description)
                .UsingJobData("JobData", jobDetail.job_data)
                .Build();

            // 修改 Trigger
            trigger = trigger.GetTriggerBuilder()
                        .WithSchedule(CronScheduleBuilder.CronSchedule(jobDetail.cron).WithMisfireHandlingInstructionDoNothing())
                        .EndAt(jobDetail.end_time == DateTime.MinValue ? null : (DateTimeOffset?)new DateTimeOffset(jobDetail.end_time))
                        .Build();

            // 删除旧 Job
            Scheduler.DeleteJob(trigger.JobKey);
            // 新 Job 加入调度池
            Scheduler.ScheduleJob(oldJobDetail, trigger);
        }
        #endregion

        #region 启动任务
        /// <summary>
        /// 启动任务
        /// </summary>
        public static void Start()
        {
            if (!Scheduler.IsShutdown)
            {
                Scheduler.Start();
            }
        }
        #endregion

        #region 停止任务
        /// <summary>
        /// 停止任务
        /// </summary>
        public static void Stop()
        {
            Scheduler.Shutdown();
        }
        #endregion

        #region 获取目前已经持久化的任务
        /// <summary>
        /// 获取目前已经持久化的任务
        /// </summary>
        /// <returns></returns>
        public static DataTable GetQrtzJobDetails()
        {
            return db.GetDataTable($@"select a.JOB_NAME,a.JOB_GROUP,a.JOB_CLASS_NAME,a.JOB_CLASS_NAME, a.DESCRIPTION,a.JOB_DATA,
                                      b.TRIGGER_NAME,b.NEXT_FIRE_TIME,b.PREV_FIRE_TIME,'' NEXT_FIRE_DATETIME,'' PREV_FIRE_DATETIME,b.TRIGGER_STATE,
                                      c.CRON_EXPRESSION 
                                from [dbo].[QRTZ_JOB_DETAILS] a,[QRTZ_TRIGGERS] b,[QRTZ_CRON_TRIGGERS] c
                                where a.JOB_NAME = b.JOB_NAME and b.TRIGGER_NAME = c.TRIGGER_NAME");
        }
        #endregion

        #region 提供任务下拉列表以便新建任务
        /// <summary>
        /// 提供任务下拉列表以便新建任务
        /// </summary>
        /// <returns></returns>
        public static dynamic GetBaseJobDetails()
        {
            return db.GetDataTable("select * from job_detail");
        }
        #endregion


        #region 获取 GroupName
        /// <summary>
        /// 获取 分组名称 如果为空 则返回默认的分组名称 （DEFAULT）
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        static string GetGroupNameOrDefault(string groupName)
        {
            return groupName.IsNullOrWhiteSpace() ? TriggerKey.DefaultGroup : groupName;
        }
        #endregion

        #region 获取、创建 JobKey
        /// <summary>
        /// 获取、创建 TriggerKey
        /// </summary>
        /// <param name="jobDetail"></param>
        /// <returns></returns>
        static JobKey GetJobKey(JobDetail jobDetail)
        {
            return new JobKey(jobDetail.job_name, GetGroupNameOrDefault(jobDetail.group_name));
        }

        /// <summary>
        /// 获取、创建 TriggerKey
        /// </summary>
        /// <param name="jobDetail"></param>
        /// <returns></returns>
        static JobKey GetJobKey(string jobName, string groupName = "")
        {
            return new JobKey(jobName, GetGroupNameOrDefault(groupName));
        }
        #endregion

        #region 获取、创建 TriggerKey
        /// <summary>
        /// 获取、创建 TriggerKey
        /// </summary>
        /// <param name="jobDetail"></param>
        /// <returns></returns>
        static TriggerKey GetTriggerKey(JobDetail jobDetail)
        {
            return new TriggerKey(jobDetail.job_name, GetGroupNameOrDefault(jobDetail.group_name));
        }

        /// <summary>
        /// 获取、创建 TriggerKey
        /// </summary>
        /// <param name="jobDetail"></param>
        /// <returns></returns>
        static TriggerKey GetTriggerKey(string jobName, string groupName = "")
        {
            return new TriggerKey(jobName, GetGroupNameOrDefault(groupName));
        }
        #endregion

    }
    public class JobDetail
    {
        public int id { get; set; }
        public string job_classname { get; set; }
        public string job_name { get; set; }
        public string group_name { get; set; }
        public string cron { get; set; }
        public string job_description { get; set; }
        public bool is_start_now { get; set; }
        public string job_data { get; set; }
        public DateTime start_time { get; set; }
        public DateTime end_time { get; set; }
        public DateTime createtime { get; set; }
        public string random { get; set; }

    }
}