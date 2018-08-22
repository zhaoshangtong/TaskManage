using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;

namespace TaskManage
{
    public class ScheduledScanDatabaseJob : IJob
    {
        public ScheduledScanDatabaseJob()
        {
        }

        public void Execute(IJobExecutionContext context)
        {
            JobDetail jobDetail = new JobDetail();
            try
            {
                var dt = QuartzManager.db.GetDataTable($@"select * from {QuartzManager.table_name}");
                // 得到所有的任务Key
                var jobKeys = QuartzManager.Scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals("DEFAULT"));

                // 移除已经删除的任务(匹配数据库和 jobKeys 中的数据，如果数据库中没有而在 jobKeys 中有，则删除)
                foreach (var jobKey in jobKeys)
                {
                    var exists = false;
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["job_name"].ToString().Equals(jobKey.Name))
                        {
                            exists = true;
                        }
                    }
                    if (!exists)
                    {
                        QuartzManager.Delete(jobKey.Name);
                        QuartzManager.dicIndentify.Remove(jobKey.Name);
                        LogHelper.Info($@"【{jobKey.Name} {QuartzManager.Scheduler.GetJobDetail(jobKey).Description}】 任务删除");
                    }
                }

                // 循环对比数据库任务和正在运行的任务
                foreach (DataRow row in dt.Rows)
                {
                    var update_indentify = row["update_indentify"].ToString();
                    jobDetail = new JobDetail()
                    {
                        job_name = row["job_name"].ToString(),
                        job_classname = row["job_classname"].ToString(),
                        job_description = row["job_description"].ToString(),
                        group_name = row["group_name"].ToString(),
                        cron = row["cron"].ToString(),
                        is_start_now = Convert.ToBoolean(row["is_start_now"]),
                        start_time = row["start_time"].To(DateTime.MinValue),
                        end_time = row["end_time"].To(DateTime.MinValue),
                        job_data = new { url = row["url"].ToString(), request_type = row["request_type"].ToString(), data = row["job_data"] }.ToJson()
                    };

                    // 是否有正在运行的同名任务
                    var exists = false;
                    foreach (var jobKey in jobKeys)
                    {
                        if (!row["job_name"].ToString().Equals(jobKey.Name))
                        {
                            continue;
                        }

                        var job_data = QuartzManager.Scheduler.GetJobDetail(jobKey).JobDataMap.Get("JobData");
                        if (job_data != null && !string.IsNullOrWhiteSpace(job_data.ToString()))
                        {
                            var j_data = job_data.ToString().ToJObject();
                            // 判断更新标识不一致则更新 JOB
                            if (!update_indentify.Equals(QuartzManager.dicIndentify[jobDetail.job_name]))
                            {
                                switch (row["status"].ToString().ToUpper())
                                {
                                    case "MODIFY":
                                        QuartzManager.Modify(jobDetail);
                                        QuartzManager.dicIndentify[jobDetail.job_name] = update_indentify;
                                        QuartzManager.db.ExcuteSQL($@"update {QuartzManager.table_name} set handler='quartz',status='NORMAL' where id={row["id"].ToString()} and update_indentify='{update_indentify}'");
                                        LogHelper.Info($@"【{row["job_name"].ToString()} {row["job_description"].ToString()}】 更新任务完成 任务状态：{QuartzManager.Scheduler.GetTriggerState(new TriggerKey(jobKey.Name)).ToString()}");
                                        break;
                                    case "PAUSED":
                                        QuartzManager.Pause(jobDetail.job_name, jobDetail.group_name);
                                        QuartzManager.dicIndentify[jobDetail.job_name] = update_indentify;
                                        QuartzManager.db.ExcuteSQL($@"update {QuartzManager.table_name} set handler='quartz' where id={row["id"].ToString()} and update_indentify='{update_indentify}'");
                                        LogHelper.Info($@"【{row["job_name"].ToString()} {row["job_description"].ToString()}】 暂停任务完成 任务状态：{QuartzManager.Scheduler.GetTriggerState(new TriggerKey(jobKey.Name)).ToString()}");
                                        break;
                                    case "RESUME":
                                        QuartzManager.Resume(jobDetail.job_name, jobDetail.group_name);
                                        QuartzManager.dicIndentify[jobDetail.job_name] = update_indentify;
                                        QuartzManager.db.ExcuteSQL($@"update {QuartzManager.table_name} set handler='quartz',status='NORMAL' where id={row["id"].ToString()} and update_indentify='{update_indentify}'");
                                        LogHelper.Info($@"【{row["job_name"].ToString()} {row["job_description"].ToString()}】 恢复任务完成 任务状态：{QuartzManager.Scheduler.GetTriggerState(new TriggerKey(jobKey.Name)).ToString()}");
                                        break;
                                }
                            }
                            exists = true;
                            break;
                        }
                    }

                    // 如果 
                    //      JOB 不在 Quartz 任务中
                    //      任务是 ADD、NORMAL、MODIFY、RESUME 中的某种状态
                    //      任务不在 deletedList 列表中
                    // 则添加到 Quartz
                    if (!exists
                        && (new List<string>() { "ADD", "NORMAL", "MODIFY", "RESUME" }.Exists(x => row["status"].ToString().ToUpper().Equals(x)))
                        && !QuartzManager.deletedJobList.Exists(x => row["job_name"].ToString().Equals(x.job_name.ToString()))
                        )
                    {
                        try
                        {
                            QuartzManager.Add(jobDetail);
                            QuartzManager.dicIndentify.Add(jobDetail.job_name, update_indentify);
                            QuartzManager.db.ExcuteSQL($@"update {QuartzManager.table_name} set handler='quartz',status='NORMAL' where id={row["id"].ToString()} and update_indentify='{update_indentify}'");
                            LogHelper.Info($@"【{row["job_name"].ToString()} {row["job_description"].ToString()}】 添加任务完成");
                        }
                        catch (Exception ex)
                        {
                            if ("End time cannot be before start time".Equals(ex.Message))
                            {
                                QuartzManager.deletedJobList.Add(new { job_name = jobDetail.job_name, time = DateTime.Now });
                                QuartzManager.db.ExcuteSQL($@"update {QuartzManager.table_name} set handler='quartz',status='DELETED' where job_name='{jobDetail.job_name}'");
                            }
                            else
                            {
                                LogHelper.Error("主任务错误_添加任务出错", ex);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogHelper.Error("主任务错误", ex);
            }
        }
    }
}
