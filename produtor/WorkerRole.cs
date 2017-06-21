using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace produtor
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        //declarar filas
        static CloudQueue cloudQueueOne;
        static CloudQueue cloudQueueTwo;

        // Conecta na QueueOne
        public static void ConnectToStorageQueue()
        {
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=joabconta;AccountKey=x29pJbgintarDcyb31icWMH6UoiQZYmTOhL/jt3NBpR0ngRDSLL3/YGXMr98mA6abv+pr/8cbrMAvffFzgWvMQ==;EndpointSuffix=core.windows.net"; // ConfigurationManager.ConnectionStrings["Azure Storage Account Demo Primary"].ConnectionString;
            CloudStorageAccount cloudStorageAccount;

            if (!CloudStorageAccount.TryParse(connectionString, out cloudStorageAccount))
            {
                Console.WriteLine("Expected connection string 'Azure Storage Account to be a valid Azure Storage Connection String.");
            }

            var cloudQueueClient = cloudStorageAccount.CreateCloudQueueClient();
            cloudQueueOne = cloudQueueClient.GetQueueReference("queueone");
            cloudQueueTwo = cloudQueueClient.GetQueueReference("queuetwo");

            cloudQueueOne.CreateIfNotExists();
            cloudQueueTwo.CreateIfNotExists();
        }


        //SEND To QueueTwo
        public void SendMessageToQueueTwo(String MessageText)
        {
            var message = new CloudQueueMessage(MessageText);

            cloudQueueTwo.AddMessage(message);

        }


        // GET TO QueueOne
        public void GetMessageFromQueueOne()
        {
            CloudQueueMessage cloudQueueMessage = cloudQueueOne.GetMessage();

            if (cloudQueueMessage == null)
            {
                return;
            }
            Trace.TraceInformation("Get message from QueueOne and send to QueueTwo");
            SendMessageToQueueTwo(cloudQueueMessage.AsString);
            cloudQueueOne.DeleteMessage(cloudQueueMessage);
        }








        public override void Run()
        {
            Trace.TraceInformation("produtor is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Definir o número máximo de conexões simultâneas
            ServicePointManager.DefaultConnectionLimit = 12;

            // Para obter informações sobre como tratar as alterações de configuração
            // veja o tópico do MSDN em https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("produtor has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("produtor is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("produtor has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {

            ConnectToStorageQueue();

            // TODO: substitua o item a seguir pela sua própria lógica.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                GetMessageFromQueueOne();
                await Task.Delay(2000);
            }
        }
    }
}
