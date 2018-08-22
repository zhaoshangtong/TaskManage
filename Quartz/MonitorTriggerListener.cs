using Quartz;
using RaysCloud.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TaskManage
{
    /// <summary>
    /// 触发器监听程序
    /// </summary>
    public class MonitorTriggerListener : ITriggerListener
    {
        public MonitorTriggerListener()
        {
        }

        public string Name
        {
            get
            {
                return "MonitorTriggerListener";
            }
        }

        #region 触发成功
        /// <summary>
        /// 触发成功
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="context"></param>
        public void TriggerFired(ITrigger trigger, IJobExecutionContext context)
        {
            try
            {
                LogHelper.Info($"【TriggerFired】：触发器 {trigger.Key.Name} 触发 【TriggerFired】 完成 MonitorTriggerListener.TriggerFired");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"MonitorTriggerListener.TriggerFired 异常，当前 Trigger:{ trigger.Key.Name }", ex);
            }
        }
        #endregion

        #region 触发完成
        /// <summary>
        /// 触发完成
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="context"></param>
        /// <param name="triggerInstructionCode"></param>
        public void TriggerComplete(ITrigger trigger, IJobExecutionContext context, SchedulerInstruction triggerInstructionCode)
        {
            var currentTriggerState = QuartzManager.Scheduler.GetTriggerState(trigger.Key).ToString().ToUpper();
            if ("COMPLETE".Equals(currentTriggerState))
            {
                LogHelper.Info($"【TriggerComplete】：触发器 {trigger.Key.Name} 触发【TriggerComplete】完成 MonitorTriggerListener.TriggerComplete");
            }

            try
            {
                LogHelper.Info($"【TriggerComplete】：触发器 {trigger.Key.Name} 触发【TriggerComplete】完成 MonitorTriggerListener.TriggerComplete");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"MonitorTriggerListener.TriggerComplete 异常，当前 Trigger:{ trigger.Key.Name }", ex);
            }
        }
        #endregion

        #region 触发失败（没有可用线程，阻塞等等）
        /// <summary>
        /// 触发失败（没有可用线程，阻塞等等）
        /// </summary>
        /// <param name="trigger"></param>
        public void TriggerMisfired(ITrigger trigger)
        {
            try
            {
                LogHelper.Info($"【TriggerMisfired】：触发器 {trigger.Key.Name} 触发 【TriggerMisfired】 完成 MonitorTriggerListener.TriggerMisfired");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"MonitorTriggerListener.TriggerMisfired 异常，当前 Trigger:{ trigger.Key.Name }", ex);
            }
        }
        #endregion

        #region  阻止任务执行（可以用来过滤多任务执行  重复任务执行）
        /// <summary>
        /// 阻止任务执行（可以用来过滤多任务执行  重复任务执行）
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool VetoJobExecution(ITrigger trigger, IJobExecutionContext context)
        {
            //try
            //{
            //    LogHelper.Info($"【VetoJobExecution】：触发器 {trigger.Key.Name} 触发 【VetoJobExecution】 完成 MonitorTriggerListener.VetoJobExecution");
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Error($"MonitorTriggerListener.VetoJobExecution 异常，当前 Trigger:{ trigger.Key.Name }", ex);
            //}
            return false;
        }
        #endregion
    }
}
