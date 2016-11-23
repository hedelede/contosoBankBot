using contosoBankBot.DataModels;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace contosoBankBot
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<leTimeline> timelineTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("MOBILE_APP_URL");
            this.timelineTable = this.client.GetTable<leTimeline>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task AddTimeline(leTimeline timeline)
        {
            await this.timelineTable.InsertAsync(timeline);
        }

        public async Task<List<leTimeline>> GetTimelines()
        {
            return await this.timelineTable.ToListAsync();
        }
    }
}