using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.WindowsAzure.MobileServices;
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
                endOutput += "To get started, tell me what you want to do. \n\n";
                endOutput += "If you need help, write **help 1**!";


                bool greeting = userData.GetProperty<bool>("SentGreeting");

                //Basic greeting, saving name
                if (greeting == true)
                {
                    string name = userData.GetProperty<string>("Name");
                    endOutput = "What can I do for you, " + name + "?";
                    if (name == null)
                    {
                        endOutput = "Why hello there, random stranger! What is your name?";
                        userData.SetProperty<bool>("SentGreeting", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }
                    else
                    {
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
                        if (name.Length != 0)
                        {
                            endOutput = "So your name is " + name + "? Nice to meet you!";
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            userData.SetProperty<bool>("SentGreeting", true);
                        }

                        userData.SetProperty<string>("Name", name);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }
                }

                //Clear user data if requested
                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "Your user data has been cleared.";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
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

                //Read account information
                if (userMessage.ToLower().Equals("get account"))
                {
                    List<leContosoBankTable> databases = await AzureManager.AzureManagerInstance.GetDatabase();
                    endOutput = "";
                    foreach (leContosoBankTable t in databases)
                    {
                        endOutput += "As of " + t.UpdatedAt + ", your "+ t.AccountName + " acount has " + t.Balance + " (NZD) in it." ;
                    }

                }

                //Create account
                if (userMessage.Length > 12 && userMessage.ToLower().Substring(0, 11).Equals("new account"))
                {

                    string accountName = userMessage.Substring(12);
                    leContosoBankTable database = new leContosoBankTable()
                    {
                        AccountName = accountName,
                        Balance = "0"
                    };

                    await AzureManager.AzureManagerInstance.AddDatabase(database);

                    endOutput = "A new account called " + database.AccountName + " was created!";
                }

                //Delete account
                /*
                if (userMessage.Length > 15 && userMessage.ToLower().Substring(0, 14).Equals("delete account"))
                {
                    string accountName = userMessage.Substring(12);
                    leContosoBankTable database = new leContosoBankTable()
                    {
                        AccountName = accountName,
                    };
                    await AzureManager.AzureManagerInstance.RemoveDatabase(database);
                    endOutput = "Your account was deleted.";
                }
                */
                //Update account
                /*
                if (userMessage.Length > 15 && userMessage.ToLower().Substring(0, 14).Equals("update account"))
                {
                    string accountName = userMessage.Substring(12);
                    Random amountOfMoney = new Random();
                    double newBalance = amountOfMoney.Next(0, 1000);

                    List<leContosoBankTable> database = await AzureManager.AzureManagerInstance.GetDatabase();
                    foreach (leContosoBankTable account in database)
                    {
                        if (account.AccountName == accountName)
                        {
                            account.Balance = newBalance.ToString();
                            await AzureManager.AzureManagerInstance.UpdateDatabase(account);
                        }
                    }
                    endOutput = "You now have " + newBalance.ToString() + " (NZD) in your acccount.";
                }
                */
                //Set base currency
                if (userMessage.Length > 21)
                {
                    if (userMessage.ToLower().Substring(0, 20).Equals("set base currency to"))
                    {
                        string baseCurrency = ((userMessage.Substring(21, 3)).ToLower());
                        string[] availableCurrency = new string[] { "aud", "bgn", "brl", "cad", "chf", "cny", "czk", "dkk", "eur", "gbp", "hkd", "hrk", "huf", "idr", "ils", "inr", "jpy", "krw", "mxn", "myr", "nok", "nzd", "php", "pln", "ron", "rub", "sek", "sgd", "thb", "try", "usd", "zar" };

                        if (!((availableCurrency).Contains(baseCurrency)))
                        {
                            endOutput = "This does not appear to be one of my available currencies";
                        }
                        else
                        {
                            userData.SetProperty<string>("baseCurrency", baseCurrency);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            endOutput = "Your base currency is now set to " + baseCurrency.ToUpper();
                        }
                    }
                }

                //Set destination currency
                if (userMessage.Length > 27)
                {
                    if (userMessage.ToLower().Substring(0, 27).Equals("set destination currency to"))
                    {
                        string destinationCurrency = ((userMessage.Substring(28, 3)).ToLower());
                        string[] availableCurrency = new string[] { "aud", "bgn", "brl", "cad", "chf", "cny", "czk", "dkk", "eur", "gbp", "hkd", "hrk", "huf", "idr", "ils", "inr", "jpy", "krw", "mxn", "myr", "nok", "nzd", "php", "pln", "ron", "rub", "sek", "sgd", "thb", "try", "usd", "zar" };

                        if (!((availableCurrency).Contains(destinationCurrency)))
                        {
                            endOutput = "This does not appear to be one of my available currencies";
                        }
                        else
                        {
                            userData.SetProperty<string>("destinationCurrency", destinationCurrency);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            endOutput = "Your destination currency is now set to " + destinationCurrency.ToUpper();
                        }
                    }
                }

                //Help
                if (userMessage.ToLower().Contains("help 1"))
                {
                    endOutput = "Here is a list of commands that may help you. ";
                    endOutput += "Please note that any bold statements are commands which may be useful to you.  \n";
                    endOutput += "Set your name: **my name is **[your name]  \n";
                    endOutput += "Access the latest stock information: **stocks**  \n";
                    endOutput += "Clear you user data: **clear**  \n";
                    endOutput += "Write **help 2** to get currency rate help";
                }
                if (userMessage.ToLower().Contains("help 2"))
                {
                    endOutput = "To get the latest currency rates:  \n";
                    endOutput += "1) **set destination currency to ** [3-lettered currency symbol]  \n";
                    endOutput += "2) **set base currency to ** [3-lettered currency symbol]  \n";
                    endOutput += "3) **conversion**  \n";
                    endOutput += "Note that 1. and 2. can be set in either order  \n";
                    endOutput += "Write **help 3** to get help with your account";
                }
                if (userMessage.ToLower().Contains("help 3"))
                {
                    endOutput = "To create an account: **new account** [account name]  \n";
                    //endOutput += "To delete an account: **delete account** [account name]  \n";
                    //endOutput += "To update an account: **update account** [account name]  \n";
                    endOutput += "To see all your accounts: **get account**  \n";
                }

                //Allow conversion
                if (userMessage.ToLower().Equals("conversion"))
                {
                    exchangeRateRequest = true;
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
                    //Access API
                    string baseCurrency = userData.GetProperty<string>("baseCurrency");
                    string destinationCurrency = userData.GetProperty<string>("destinationCurrency");

                    if (baseCurrency != null || destinationCurrency != null)
                    {
                        HttpClient Client = new HttpClient();
                        string conversionRate = await Client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + baseCurrency.ToUpper() + "&symbols=" + destinationCurrency.ToUpper()));

                        //Retrieve data
                        conversionRate = conversionRate.Substring(49, 3);
                        double rate = Convert.ToDouble(conversionRate);


                        //Calculate conversion amount
                        //double result = (1 * rate);
                        //string resultReply = Convert.ToString(result);

                        endOutput = "1" + baseCurrency.ToUpper() + " is worth " + rate + destinationCurrency.ToUpper();
                    }
                    else
                    {
                        endOutput = "Please **set base currency to** something and **set destination currency to** something before a **conversion**";
                    }


                    //// return our reply to the user
                    //Activity infoReply = activity.CreateReply(endOutput);
                    //await connector.Conversations.ReplyToActivityAsync(infoReply);

                    ////Reset the conversion information
                    //userData.SetProperty<string>("baseCurrency", "");
                    //userData.SetProperty<string>("destinationCurrency", "");
                    //userData.SetProperty<string>("conversionAmount", "");

                    // return our reply to the user
                    Activity infoReply = activity.CreateReply(endOutput);
                    await connector.Conversations.ReplyToActivityAsync(infoReply);
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