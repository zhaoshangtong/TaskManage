using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManage
{
    public class ExecRequestJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                var job_name_desc = context.JobDetail.Key.Name + " " + context.JobDetail.Description;
                var job_id = context.JobDetail.Key.Name + DateTime.Now.ToString("yyyyMMddHHmmss");
                LogHelper.Info($@"{job_id} 开始执行 { job_name_desc } 任务");

                var job_data = context.JobDetail.JobDataMap.Get("JobData");

                if (job_data != null && !string.IsNullOrWhiteSpace(job_data.ToString()))
                {
                    var j_data = job_data.ToString().ToJObject();

                    if ("GET".Equals(j_data.Value<string>("request_type").ToUpper()))
                    {
                        Util.MethodGETAsync(j_data.Value<string>("url"), "UTF-8").ContinueWith(x =>
                        {
                            try
                            {
                                LogHelper.Info($@"{job_id} 结束执行 { job_name_desc } 任务,执行结果：{x.Result}");
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Error($@"{job_id} 执行 { job_name_desc } 任务错误", ex);
                            }
                        });
                    }
                    else if ("POST".Equals(j_data.Value<string>("request_type").ToUpper()))
                    {
                        Util.MethodPOSTAsync(j_data.Value<string>("url"), j_data.Value<string>("data") ?? "", "UTF-8", "application/json").ContinueWith(x =>
                        {
                            try
                            {
                                LogHelper.Info($@"{job_id} 结束执行 { job_name_desc } 任务,执行结果：{x.Result}");
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Error($@"{job_id} 执行 { job_name_desc } 任务错误", ex);
                            }
                        });
                    }
                    else
                    {
                        LogHelper.Error($@"{job_id} 结束执行 { job_name_desc } 任务,执行结果：未知的请求类型{j_data.Value<string>("request_type").ToUpper()} 目前只支持 GET/POST");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(context.JobDetail.Key.Name, ex);
            }
        }
    }
}
