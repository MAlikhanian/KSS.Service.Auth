using KSS.Helper;
using KSS.Helper.Enum.Base;
using KSS.Helper.Model;

namespace KSS.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int delay = 1000;

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                await TaskExecute(stoppingToken);

                await Task.Delay(delay, stoppingToken);

                delay = 30000;
            }
        }

        private async Task TaskExecute(CancellationToken stoppingToken)
        {
            try
            {
                var client = new APIClient();

                //var runningTasks = await client.Post<List<_000_ERP_Task>, Filter>("_000_ERP_Task/ToListByFilter/", new Filter { DataType = DataType.Byte, Value = 2 });

                //if (!runningTasks.Any())
                //{
                //    //var tasks = await client.Post<List<_000_ERP_Task>, Filter>("_000_ERP_Task/ToListByFilter/", new Filter { DataType = DataType.Byte, Value = 1 });

                //    var task = tasks.FirstOrDefault();

                //    if (task != null)
                //    {
                //        if (task.TypeId == 3 || task.TypeId == 4 || task.TypeId == 5 || task.TypeId == 8 || task.TypeId == 9 || task.TypeId == 10 ||
                //            task.TypeId == 12 || task.TypeId == 22 || task.TypeId == 23 || task.TypeId == 24 || task.TypeId == 30 || task.TypeId == 31)
                //        {
                //            //var taskUpdateResult = await client.Put("_000_ERP_Task/UpdateDto/", new _000_ERP_TaskUpdateDto { Id = task.Id, StartGDateTime = DateTime.Now, StatusId = 2 });

                //            try
                //            {
                //                switch (task.TypeId)
                //                {
                //                    case 3:

                //                        var result = await client.Get<string>($"_005_SAWMS_Business/PayrollCalculate?id={task.Id}");

                //                        _logger.LogInformation(result);

                //                        break;

                //                    case 4:

                //                        result = await client.Get<string>($"_005_SAWMS_Business/DeductionBenefitInstallment?id={task.Id}");

                //                        break;

                //                    case 5:

                //                        result = await client.Get<string>($"_005_SAWMS_Business/CostAssignmentCalculate?id={task.Id}");

                //                        break;

                //                    case 6:

                //                        result = await client.Get<string>($"_015_TMS_Business/AutoTimeSheetCalculate?id={task.Id}");

                //                        break;

                //                    case 8:

                //                        result = await client.Get<string>($"_005_SAWMS_Business/ReportFileCreate?id={task.Id}");

                //                        break;

                //                    case 9:

                //                        if (task.Parameter02 == "1")
                //                            result = await client.Get<string>($"_005_SAWMS_Business/ReportFileCreate?id={task.Id}");
                //                        else if (task.Parameter02 == "2")
                //                            result = await client.Get<string>($"_005_SAWMS_Business/InsuranceFileCreate?id={task.Id}");

                //                        break;

                //                    case 10:

                //                        if (task.Parameter02 == "1")
                //                            result = await client.Get<string>($"_005_SAWMS_Business/ReportFileCreate?id={task.Id}");
                //                        else if (task.Parameter02 == "2")
                //                            result = await client.Get<string>($"_005_SAWMS_Business/TaxFileCreate?id={task.Id}");

                //                        break;

                //                    case 12:

                //                        result = await client.Get<string>($"_005_SAWMS_Business/FinancialFileCreate?id={task.Id}");

                //                        break;

                //                    case 22:

                //                        result = await client.Get<string>($"_005_SAWMS_Business/ReportFileCreate?id={task.Id}");

                //                        break;

                //                    case 23:

                //                        result = await client.Get<string>($"_005_SAWMS_Business/ReportFileCreate?id={task.Id}");

                //                        break;

                //                    case 24:

                //                        result = await client.Get<string>($"_005_SAWMS_Business/ReportFileCreate?id={task.Id}");

                //                        break;

                //                    case 30:

                //                        result = await client.Get<string>($"_015_TMS_Business/TimeSheetReport?id={task.Id}");

                //                        break;

                //                    case 31:

                //                        result = await client.Get<string>($"_006_CMS_Business/SearchLetterExport?id={task.Id}");

                //                        break;
                //                }

                //                //taskUpdateResult = await client.Put("_000_ERP_Task/UpdateDto/", new _000_ERP_TaskUpdateDto { Id = task.Id, EndGDateTime = DateTime.Now, StatusId = 3 });

                //            }
                //            catch (Exception)
                //            {
                //                //taskUpdateResult = await client.Put("_000_ERP_Task/UpdateDto/", new _000_ERP_TaskUpdateDto { Id = task.Id, EndGDateTime = DateTime.Now, StatusId = 4 });
                //            }
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while Task Execute");
            }
        }
    }
}