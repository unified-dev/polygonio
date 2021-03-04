using Newtonsoft.Json;
using Nito.AsyncEx.Synchronous;
using NPOI.XSSF.UserModel;
using PolygonIo.WebApi;
using PolygonIo.WebApi.Model;
using System;
using System.Management.Automation;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.PowerShell
{
    [Cmdlet(VerbsCommon.Get, "AggregatesAsJson")]
    [OutputType(typeof(string))]
    public class GetAggregatesAsJson : Cmdlet
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

        [Parameter(Position = 6, ValueFromPipelineByPropertyName = true)]
        public bool Unadjusted { get; set; } = false;

        [Parameter(Position = 7, ValueFromPipelineByPropertyName = true)]
        public Sort Sort { get; set; } = Sort.asc;

        [Parameter(Position = 8, ValueFromPipelineByPropertyName = true)]
        public int Limit { get; set; } = 50000;

        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        protected async Task<AggregateResponse> ProcessRecordAsync()
        {
            using (var hc = new HttpClient())
            {
                return await PolygonWebApi
                                .GetAggregatesBarsAsync(hc, cts.Token, ApiKey, StocksTicker, Multiplier, Timespan, From, To, Unadjusted, Sort, Limit);
            }
        }

        protected override void StopProcessing()
        {
            cts.Cancel();
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            try
            {
                var task = ProcessRecordAsync();
                var result = task.WaitAndUnwrapException();
                WriteObject(JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            catch(Exception e)
            {
                var errorRecord = new ErrorRecord(e, e.Message, ErrorCategory.CloseError, null);
                ThrowTerminatingError(errorRecord);
            }  
        }
    }
}
