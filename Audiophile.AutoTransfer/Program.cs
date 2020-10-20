using System;
using Quartz;
using Quartz.Impl;

namespace Audiophile.AutoTransfer
{
    class Program
    {
        private const string version = "v0.1";
        
        static void Main(string[] args)
        {
            Console.WriteLine("Audiophile Auto Transfer Started "+version);
            
            InitializeJobs();
            
            Console.ReadLine();
        }
        
        private static async void InitializeJobs()
        {
            var _scheduler = await new StdSchedulerFactory().GetScheduler();
            await _scheduler.Start();

            var autoTransferJob = JobBuilder.Create<AutoTransferJob>()
                .WithIdentity("AutoTransferJob")
                .Build();
            var autoTransferTrigger = TriggerBuilder.Create()
                .WithIdentity("AutoTransferJobCron")
                .StartNow()
                .WithCronSchedule("0 15 12 ? * MON,TUE,WED,THU,FRI *")  //PZT-CUM saat 12:15 (1 kere) -  0 15 12 ? * MON,TUE,WED,THU,FRI * 
                .Build();
            
            //her saat: 0 0 0/1 1/1 * ? *
            
            //PZT-CUM saat 15:00 (1 kere) -  0 0 15 ? * MON,TUE,WED,THU,FRI * 

            var result = await _scheduler.ScheduleJob(autoTransferJob, autoTransferTrigger);
        }
    }
}