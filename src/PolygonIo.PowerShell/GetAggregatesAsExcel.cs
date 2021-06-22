using Nito.AsyncEx.Synchronous;
using NPOI.XSSF.UserModel;
using PolygonIo.WebApi;
using System;
using System.IO;
using System.Management.Automation;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PolygonIo.WebApi.Contracts;

namespace PolygonIo.PowerShell
{
    [Cmdlet(VerbsCommon.Get, "AggregatesAsExcel")]
    [OutputType(typeof(string))]
    public class GetAggregatesAsExcel : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty()]
        public string StocksTicker { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int Multiplier { get; set; }

        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public Timespan Timespan { get; set; }

        [Parameter(Position = 3, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public DateTime From { get; set; }

        [Parameter(Position = 4, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public DateTime To { get; set; }

        [Parameter(Position = 5, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string ApiKey { get; set; }

        [Parameter(Position = 6, Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public string ExcelFileName { get; set; } = "";

        [Parameter(Position = 7, ValueFromPipelineByPropertyName = true)]
        public bool Unadjusted { get; set; } = false;

        [Parameter(Position = 8, ValueFromPipelineByPropertyName = true)]
        public Sort Sort { get; set; } = Sort.asc;

        [Parameter(Position = 9, ValueFromPipelineByPropertyName = true)]
        public int Limit { get; set; } = 50000;

        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        protected async Task<AggregateResponse> ProcessRecordAsync()
        {
            using (var client = new HttpClient())
            {
                return await client.GetPolygonAggregatesBarsV2Async(
                    cts.Token, this.ApiKey, StocksTicker, Multiplier, Timespan, From, To, Unadjusted, Sort, Limit);
            }
        }

        protected override void StopProcessing()
        {
            cts.Cancel();
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var sb = new StringBuilder();

            try
            {
                if(string.IsNullOrEmpty(ExcelFileName))
                {
                    ExcelFileName = $"{StocksTicker}-{From:yyyy-MM-dd}-to-{To:yyyy-MM-dd}-{Multiplier}-{Timespan}.xlsx";
                }

                var task = ProcessRecordAsync();
                var result = task.WaitAndUnwrapException();
   
                // output details
                sb.AppendLine($"status: {result.Status}");
                sb.AppendLine($"ticker: {result.Ticker}");
                sb.AppendLine($"from: {From}");
                sb.AppendLine($"to: {To}");
                sb.AppendLine($"timespan: {Timespan}");
                sb.AppendLine($"multiplier: {Multiplier}");
                sb.AppendLine($"queryCount: {result.QueryCount}");
                sb.AppendLine($"resultsCount: {result.ResultsCount}");
                sb.AppendLine($"adjusted: {result.Adjusted}");
                sb.AppendLine($"url: {result.Url}");
                sb.AppendLine($"excel file: {ExcelFileName}");

                // write spreadsheet
                WriteAggregateResponseToExcel(result, ExcelFileName);

                WriteObject(sb.ToString());
            }
            catch(Exception e)
            {
                var errorRecord = new ErrorRecord(e, e.Message, ErrorCategory.CloseError, null);
                ThrowTerminatingError(errorRecord);
            }
        }

        void WriteAggregateResponseToExcel(AggregateResponse response, string path)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet(StocksTicker);

            var header = sheet.CreateRow(0);
            header.CreateCell(0).SetCellValue("UTC");
            header.CreateCell(1).SetCellValue("Open");
            header.CreateCell(2).SetCellValue("High");
            header.CreateCell(3).SetCellValue("Low");
            header.CreateCell(4).SetCellValue("Close");
            header.CreateCell(5).SetCellValue("Volume");
            header.CreateCell(6).SetCellValue("Samples");

            var rowCount = 1;
            foreach(var rowResult in response.Results)
            {
                var row = sheet.CreateRow(rowCount);
                row.CreateCell(0).SetCellValue(rowResult.Timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                row.CreateCell(1).SetCellValue((double)rowResult.Open);
                row.CreateCell(2).SetCellValue((double)rowResult.High);
                row.CreateCell(3).SetCellValue((double)rowResult.Low);
                row.CreateCell(4).SetCellValue((double)rowResult.Close);
                row.CreateCell(5).SetCellValue((double)rowResult.Volume);
                row.CreateCell(6).SetCellValue((double)rowResult.Samples);
                rowCount++;
            }

            var sw = File.Create(path);
            workbook.Write(sw);
            sw.Close();
        }
    }
}
