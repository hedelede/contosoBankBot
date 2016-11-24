using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using MSA_WeatherBot.Models;
using System.Collections.Generic;
using Microsoft.WindowsAzure.MobileServices;
using contosoBankBot.Models;
using contosoBankBot.DataModels;

namespace contosoBankBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                //Setting up bot and looking for user data
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                var userMessage = activity.Text;
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                bool exchangeRateRequest = false;
                string endOutput = "Hello, I am *Contoso 343*, the Contoso Bank chatbot. ";
                endOutput += "To get started, tell me what you want to do:  \n";
                endOutput += "-Write \"**stocks**\" to get the latest stock info  \n";
                endOutput += "-Write \"**conversion**\" to get the latest conversion rates of the NZD  \n";
                endOutput += "-Write \"**login**\" to access your personal data";


                bool greeting = userData.GetProperty<bool>("SentGreeting");
                                
                //Basic greeting, saving name
                if (greeting == true)
                {
                    string name = userData.GetProperty<string>("Name");
                    endOutput = "What can I do for you, " + name + "?";
                    if (name == null)
                    {
                        endOutput = "Why hello there, random stranger! What is your name?";
                        exchangeRateRequest = false;
                        userData.SetProperty<bool>("SentGreeting", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }
                    else
                    {
                        //activity.Text = name;
                        userData.SetProperty<bool>("SentGreeting", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }
                }
                else
                {
                    userData.SetProperty<bool>("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                //Save name
                if (userMessage.Length > 11)
                {
                    if (userMessage.ToLower().Substring(0, 10).Equals("my name is"))
                    {
                        string name = userMessage.Substring(11);
                        userData.SetProperty<string>("Name", name);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        //endOutput = "So your name is " + name + "? Nice to meet you!";
                        exchangeRateRequest = false;
                        if (name.Length != 0)
                        {
                            endOutput = "So your name is " + name + "? Nice to meet you!";
                            exchangeRateRequest = false;
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            userData.SetProperty<bool>("SentGreeting", true);
                        }

                        userData.SetProperty<string>("Name", name);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        //endOutput = "So your name is " + name + "? Nice to meet you!";
                        exchangeRateRequest = false;
                    }
                }

                //Clear user data if requested
                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "User data cleared";
                    //string name = userData.GetProperty<string>("Name");
                    //name = null;
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    exchangeRateRequest = false;
                }

                //Card for displaying link to latest stocks
                if (userMessage.ToLower().Equals("stocks"))
                {
                    Activity replyToConversation = activity.CreateReply("Here are the most active stock information that I found.");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://lh5.ggpht.com/dTFTNAPcPpdd52VjidAJX7N8WYS6KuZ01l2NoXHF01iSTzLZLJ5ng-HEeuy8vCYL5DU=w300"));
                    List <CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "http://finance.yahoo.com/most-active",
                        Type = "openUrl",
                        Title = "More details"
                    };
                    cardButtons.Add(plButton);
                    ThumbnailCard plCard = new ThumbnailCard()
                    {
                        Title = "Yahoo Finance",
                        Subtitle = "The latest in world stocks",
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                //Database implementation
                if (userMessage.ToLower().Equals("get database"))
                {
                    List<leContosoBankTable> databases = await AzureManager.AzureManagerInstance.GetDatabase();
                    endOutput = "";
                    foreach (leContosoBankTable t in databases)
                    {
                        endOutput += "[" + t.CreatedAt + "] ID " + t.ID + ", Version " + t.Version + "\n\n";
                    }
                    exchangeRateRequest = false;

                }

                //Database creation
                if (userMessage.ToLower().Equals("new database"))
                {
                    leContosoBankTable database = new leContosoBankTable()
                    {
                        ID = "123454321",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        Deleted = false,
                        Version = 1.0
                    };

                    await AzureManager.AzureManagerInstance.AddDatabase(database);

                    exchangeRateRequest = false;

                    endOutput = "New timeline added [" + database.CreatedAt + "]";
                }

                //API call for conversion rates
                if (!exchangeRateRequest)
                {
                    // return our reply to the user
                    Activity infoReply = activity.CreateReply(endOutput);
                    await connector.Conversations.ReplyToActivityAsync(infoReply);

                }
                else
                {

                    WeatherObject.RootObject rootObject;
                    /*ExchangeRateObject.RootObject leRootObject;
                    endOutput = "What currency would you like to convert from?";

                    //Reset the conversion information
                    userData.SetProperty<string>("baseCurrency", "");
                    userData.SetProperty<string>("destinationCurrency", "");
                    userData.SetProperty<string>("conversionAmount", "");


                    if (userData.GetProperty<string>("baseCurrency") == "")
                    {
                        //Updates the base currency
                        string baseCurrency = activity.Text;
                        userData.SetProperty<string>("baseCurrency", baseCurrency);
                        endOutput = "What currency would you like to convert to?";
                    }
                    else if(userData.GetProperty<string>("destinationCurrency") == "")
                    {
                        //Updates the destination currency
                        string destinationCurrency = activity.Text;
                        userData.SetProperty<string>("destinationCurrency", destinationCurrency);
                        endOutput = "What amount would you like to convert?";
                    }
                    else if(userData.GetProperty<string>("conversionAmount") == "")
                    {
                        //Updates the conversion amount
                        string conversionAmount = activity.Text;
                        userData.SetProperty<string>("conversionAmount", conversionAmount);
                        endOutput = "Calculating...";
                    }
                    else if(userData.GetProperty<string>("conversionAmount") != "")
                    {
                        HttpClient leClient = new HttpClient();
                        string y = await leClient.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + userData.GetProperty<string>("baseCurrency") + "&symbols=" + userData.GetProperty<string>("destinationCurrency")));

                        leRootObject = JsonConvert.DeserializeObject<ExchangeRateObject.RootObject>(y);

                        string currency = userData.GetProperty<string>("destinationCurrency");

                        //double conversionRate = leRootObject.rates.AUD;
                        //double conversionRate = leRootObject.rates.BGN;
                        //double conversionRate = leRootObject.rates.BRL;
                        //double conversionRate = leRootObject.rates.CAD;
                        //double conversionRate = leRootObject.rates.CHF;
                        //double conversionRate = leRootObject.rates.CNY;
                        //double conversionRate = leRootObject.rates.CZK;
                        //double conversionRate = leRootObject.rates.DKK;
                        //double conversionRate = leRootObject.rates.GBP;
                        //double conversionRate = leRootObject.rates.HKD;
                        //double conversionRate = leRootObject.rates.HRK;
                        //double conversionRate = leRootObject.rates.HUF;
                        //double conversionRate = leRootObject.rates.IDR;
                        //double conversionRate = leRootObject.rates.ILS;
                        //double conversionRate = leRootObject.rates.INR;
                        //double conversionRate = leRootObject.rates.JPY;
                        //double conversionRate = leRootObject.rates.KRW;
                        //double conversionRate = leRootObject.rates.MXN;
                        //double conversionRate = leRootObject.rates.MYR;
                        //double conversionRate = leRootObject.rates.NOK;
                        //double conversionRate = leRootObject.rates.PHP;
                        //double conversionRate = leRootObject.rates.PLN;
                        //double conversionRate = leRootObject.rates.RON;
                        //double conversionRate = leRootObject.rates.RUB;
                        //double conversionRate = leRootObject.rates.SEK;
                        //double conversionRate = leRootObject.rates.SGD;
                        //double conversionRate = leRootObject.rates.THB;
                        //double conversionRate = leRootObject.rates.TRY;
                        //double conversionRate = leRootObject.rates.USD;
                        //double conversionRate = leRootObject.rates.ZAR;
                        //double conversionRate = leRootObject.rates.EUR;
                        //double conversionRate = leRootObject.rates.NZD;

                        //double result = Convert.ToDouble(currency) * ;
                    }

                    // return our reply to the user
                    Activity replyToConversation = activity.CreateReply($"This is around {result}");
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);
                    */





                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //Console.WriteLine(activity.Attachments[0].ContentUrl);

                    HttpClient client = new HttpClient();
                    string x = await client.GetStringAsync(new Uri("http://api.openweathermap.org/data/2.5/weather?q=" + activity.Text + "&units=metric&APPID=440e3d0ee33a977c5e2fff6bc12448ee"));

                    rootObject = JsonConvert.DeserializeObject<WeatherObject.RootObject>(x);

                    string cityName = rootObject.name;
                    string temp = rootObject.main.temp + "°C";
                    string pressure = rootObject.main.pressure + "hPa";
                    string humidity = rootObject.main.humidity + "%";
                    string wind = rootObject.wind.deg + "°";
                    // added fields
                    string icon = rootObject.weather[0].icon;
                    double cityId = rootObject.id;

                    // return our reply to the user
                    Activity replyToConversation = activity.CreateReply($"Current weather for {cityName}");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "http://openweathermap.org/img/w/" + icon + ".png"));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "https://openweathermap.org/city/" + cityId,
                        Type = "openUrl",
                        Title = "More Info"
                    };
                    cardButtons.Add(plButton);
                    ThumbnailCard plCard = new ThumbnailCard()
                    {
                        Title = cityName + " Weather",
                        Subtitle = "Temperature " + temp + ", pressure " + pressure + ", humidity  " + humidity + ", wind speeds of " + wind,
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}