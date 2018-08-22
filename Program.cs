using System;
using System.Dynamic;
using Topshelf;

namespace TaskManage
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();    // log4net 配置

            HostFactory.Run(x =>
            {
                x.Service<QuartzService>(s =>
                {
                    s.ConstructUsing(name => new QuartzService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Zhiyin 通用任务管理");
                x.SetDisplayName("Zhiyin任务");
                x.SetServiceName("ZhiyinTask");
            });
        }
    }

    public class QuartzService
    {
        public QuartzService() { }
        public void Start() { QuartzManager.Start(); }
        public void Stop() { QuartzManager.Stop(); }
    }
}
