﻿using System;
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
                endOutput += "  To get started, tell me what you want to do:  \n";
                endOutput += "-Write \"**stocks**\" to get the latest stock info  \n";
                endOutput += "-Write \"**conversion**\" to get the latest conversion rates of the NZD  \n";
                endOutput += "-Write \"**login**\" to access your personal data";


                bool greeting = userData.GetProperty<bool>("SentGreeting");
                                
                //Basic greeting, saving name (working? not fully)
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

                //Timeline implemtation
                if (userMessage.ToLower().Equals("get timelines"))
                {
                    List<leTimeline> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    endOutput = "";
                    foreach (leTimeline t in timelines)
                    {
                        endOutput += "[" + t.Date + "] Sadness " + t.Sadness + ", Anger " + t.Anger + "\n\n";
                    }
                    exchangeRateRequest = false;

                }

                //Timeline creation
                if (userMessage.ToLower().Equals("new timeline"))
                {
                    leTimeline timeline = new leTimeline()
                    {
                        Anger = 0.1,
                        Contempt = 0.2,
                        Disgust = 0.3,
                        Fear = 0.3,
                        Happiness = 0.3,
                        Neutral = 0.2,
                        Sadness = 0.4,
                        Surprise = 0.4,
                        Date = DateTime.Now
                    };

                    await AzureManager.AzureManagerInstance.AddTimeline(timeline);

                    exchangeRateRequest = false;

                    endOutput = "New timeline added [" + timeline.Date + "]";
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