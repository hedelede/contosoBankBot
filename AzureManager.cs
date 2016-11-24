using contosoBankBot.DataModels;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Text;

namespace contosoBankBot
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<leContosoBankTable> databaseTable;

        private AzureManager()
        {
            client = new MobileServiceClient("http://lecontosobankdatabase.azurewebsites.net");
            databaseTable = client.GetTable<leContosoBankTable>();
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

        public async Task AddDatabase(leContosoBankTable database)
        {
            await databaseTable.InsertAsync(database);
        }

        public async Task<List<leContosoBankTable>> GetDatabase()
        {
            return await databaseTable.ToListAsync();
        }
    }
}