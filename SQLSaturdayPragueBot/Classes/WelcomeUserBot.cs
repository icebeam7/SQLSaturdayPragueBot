using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SQLSaturdayPragueBot.Helpers;
using SQLSaturdayPragueBot.Models;
using SQLSaturdayPragueBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace SQLSaturdayPragueBot.Classes
{
    public class WelcomeUserBot : ActivityHandler, IBot
    {
        private const string WelcomeMessage = "Welcome to Adventure Works";

        private const string InfoMessage = "How can we help you? You can talk to our assistant. Try saying 'I want to access' or 'show me the products list'. If you want to know about a specific product, you can use 'please tell me about mountain bike' or similar messages. Our smart digital assistant will do its best to help you!";

        private const string PatternMessage = @"You can also say help to display some options";

        private readonly BotService _services;
        public static readonly string LuisKey = "AdventureWorksBotBot";

        private BotState _conversationState;
        private BotState _userState;

        public WelcomeUserBot(BotService services, ConversationState conversationState, UserState userState)
        {
            _services = services ?? throw new System.ArgumentNullException(nameof(services));

            if (!_services.LuisServices.ContainsKey(LuisKey))
                throw new System.ArgumentException($"Invalid configuration....");

            _conversationState = conversationState;
            _userState = userState;
        }

        static bool welcome = false;



        public WelcomeUserBot(ConversationState conversationState, UserState userState)
        {
            _conversationState = conversationState;
            _userState = userState;
        }

        public async override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = new CancellationToken())
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // luis
                var recognizer = await _services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
                var topIntent = recognizer?.GetTopScoringIntent();

                if (topIntent != null && topIntent.HasValue && topIntent.Value.intent != "None")
                {
                    switch (topIntent.Value.intent)
                    {
                        case Constants.LoginIntent:
                            var ent = LuisParser.GetEntityValue(recognizer);
                            var t_v = ent.Split("_", 2);

                            switch (t_v[0])
                            {
                                case Constants.EmailLabel:
                                    var customer = await WebApiService.GetCustomer(t_v[1]);
                                    var userName = "";

                                    if (customer != null)
                                    {
                                        userName = customer.CustomerName;

                                        var hero = new HeroCard();
                                        hero.Title = "Welcome";
                                        hero.Text = customer.CustomerName;
                                        hero.Subtitle = customer.CompanyName;

                                        var us = _userState.CreateProperty<CustomerShort>(nameof(CustomerShort));
                                        var c = await us.GetAsync(turnContext, () => new CustomerShort());
                                        c.CompanyName = customer.CompanyName;
                                        c.CustomerName = customer.CustomerName;
                                        c.CustomerID = customer.CustomerID;
                                        c.EmailAddress = customer.EmailAddress;

                                        var response = turnContext.Activity.CreateReply();
                                        response.Attachments = new List<Attachment>() { hero.ToAttachment() };
                                        await turnContext.SendActivityAsync(response, cancellationToken);

                                        //await turnContext.SendActivityAsync($"Welcome {userName}");
                                    }
                                    else
                                        await turnContext.SendActivityAsync($"User not found. Pleae try again");
                                    break;
                                default:
                                    await turnContext.SendActivityAsync("Please add your email to your login message");
                                    break;
                            }
                            break;
                        case Constants.ProductInfoIntent:
                            var entity = LuisParser.GetEntityValue(recognizer, Constants.ProductInfoIntent);

                            var type_value = entity.Split("_", 2);

                            switch (type_value[0])
                            {
                                case Constants.ProductLabel:
                                case Constants.ProductNameLabel:
                                    var product = "_";
                                    var message = "Our Top 5 Products are:";

                                    if (type_value[0] == Constants.ProductNameLabel)
                                    {
                                        product = type_value[1];
                                        message = "Your query returned the following products: ";
                                    }

                                    var products = await WebApiService.GetProducts(product);
                                    var data = "No results";

                                    var typing = Activity.CreateTypingActivity();
                                    var delay = new Activity { Type = "delay", Value = 5000 };

                                    if (products != null)
                                    {
                                        var responseProducts = turnContext.Activity.CreateReply();
                                        responseProducts.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        responseProducts.Attachments = new List<Attachment>();

                                        foreach (var item in products)
                                        {
                                            var card = new HeroCard();
                                            card.Subtitle = item.ListPrice.ToString("N2");
                                            card.Title = item.Name;
                                            card.Text = $"{item.Category} - {item.Model} - {item.Color}";
                                            card.Buttons = new List<CardAction>()
                                            {
                                                 new CardAction()
                                                 {
                                                     Value = $"Add product {item.ProductID} to the cart",
                                                     Type = ActionTypes.ImBack,
                                                     Title = " Add To Cart "
                                                 }
                                            };

                                            card.Images = new List<CardImage>()
                                            {
                                                new CardImage()
                                                {

                                                    Url = $"data:image/gif;base64,{item.Photo}"
                                                }
                                            };

                                            var plAttachment = card.ToAttachment();
                                            responseProducts.Attachments.Add(plAttachment);
                                        }

                                        var activities = new IActivity[]
                                        {
                                            typing,
                                            delay,
                                            MessageFactory.Text($"{message}: "),
                                            responseProducts,
                                            MessageFactory.Text("What else can I do for you?")
                                        };

                                        await turnContext.SendActivitiesAsync(activities);
                                    }
                                    else
                                    {
                                        var activities = new IActivity[]
                                        {   typing,
                                            delay,
                                            MessageFactory.Text($"{message}: {data}"),
                                            MessageFactory.Text("What else can I do for you?")
                                        };

                                        await turnContext.SendActivitiesAsync(activities);
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case Constants.AddToCartIntent:
                            var entProd = LuisParser.GetEntityValue(recognizer, Constants.NumberLabel);
                            var tv = entProd.Split("_", 2);
                            var number = 0;

                            if (tv[0] == Constants.NumberLabel)
                            {
                                number = int.Parse(tv[1]);

                                var product = await WebApiService.GetProduct(number);

                                var userStateAccessors = _userState.CreateProperty<CustomerShort>(nameof(CustomerShort));
                                var shoppingCartAccessors = _userState.CreateProperty<List<ShoppingCart>>(nameof(List<ShoppingCart>));

                                var customer = await userStateAccessors.GetAsync(turnContext, () => new CustomerShort());
                                var cart = await shoppingCartAccessors.GetAsync(turnContext, () => new List<ShoppingCart>());

                                var item = new ShoppingCart()
                                {
                                    CustomerID = customer.CustomerID,
                                    ProductID = product.ProductID,
                                    ProductName = product.Name,
                                    ListPrice = product.ListPrice,
                                    Photo = product.Photo
                                };
                                cart.Add(item);

                                var act = new IActivity[]
                                {
                                    Activity.CreateTypingActivity(),
                                    new Activity { Type = "delay", Value = 5000 },
                                    MessageFactory.Text($"Product {product.Name} was added to the cart."),
                                    MessageFactory.Text("What else can I do for you?")
                                };

                                await turnContext.SendActivitiesAsync(act);
                            }

                            break;
                        case Constants.PlaceOrderIntent:
                            //////////////////////77
                            var usAccessors = _userState.CreateProperty<CustomerShort>(nameof(CustomerShort));
                            var scAccessors = _userState.CreateProperty<List<ShoppingCart>>(nameof(List<ShoppingCart>));

                            var cust = await usAccessors.GetAsync(turnContext, () => new CustomerShort());
                            var shoppingCart = await scAccessors.GetAsync(turnContext, () => new List<ShoppingCart>());

                            if (shoppingCart.Count() > 0)
                            {
                                var receipt = turnContext.Activity.CreateReply();
                                receipt.Attachments = new List<Attachment>();

                                var card = new ReceiptCard();
                                card.Title = "Adventure Works";
                                card.Facts = new List<Fact>
                                {
                                    new Fact("Name:", cust.CustomerName),
                                    new Fact("E-mail:", cust.EmailAddress),
                                    new Fact("Company:", cust.CompanyName),
                                };

                                decimal subtotal = 0;
                                decimal p = 16M / 100;

                                card.Items = new List<ReceiptItem>();

                                foreach (var product in shoppingCart)
                                {
                                    var item = new ReceiptItem();
                                    item.Price = product.ListPrice.ToString("N2");
                                    item.Quantity = "1";
                                    item.Text = product.ProductName;
                                    item.Subtitle = product.ProductName;
                                    item.Image = new CardImage()
                                    {
                                        Url = $"data:image/gif;base64, {product.Photo}"
                                    };

                                    subtotal += product.ListPrice;

                                    card.Items.Add(item);
                                    //var plAttachment = card.ToAttachment();
                                    //receipt.Attachments.Add(plAttachment);
                                }
                                receipt.Attachments.Add(card.ToAttachment());

                                var tax = subtotal * p;
                                card.Tax = tax.ToString("N2");

                                var total = subtotal + tax;
                                card.Total = total.ToString("N2");

                                var activities = new IActivity[]
                                {
                                    Activity.CreateTypingActivity(),
                                    new Activity { Type = "delay", Value = 5000 },
                                    MessageFactory.Text("Here is your receipt: "),
                                    receipt,
                                    MessageFactory.Text("What else can I do for you?")
                                };

                                await turnContext.SendActivitiesAsync(activities);
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    var text = turnContext.Activity.Text.ToLowerInvariant();
                    switch (text)
                    {
                        case "help":
                            await SendIntroCardAsync(turnContext, cancellationToken);
                            break;
                        default:
                            await turnContext.SendActivityAsync("I did not understand you, sorry. Try again with a different sentence, please", cancellationToken: cancellationToken);
                            break;
                    }
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (!welcome)
                {
                    welcome = true;

                    await turnContext.SendActivityAsync($"Hi there. {WelcomeMessage}", cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync(InfoMessage, cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync(PatternMessage, cancellationToken: cancellationToken);
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} activity detected");
            }

            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var response = turnContext.Activity.CreateReply();

            var card = new HeroCard();
            card.Title = WelcomeMessage;
            card.Text = InfoMessage;
            card.Images = new List<CardImage>() { new CardImage("https://drive.google.com/uc?id=1eE_WlkW8G9cSI_w9heIWeo53ZkMtQu4x") };
            card.Buttons = new List<CardAction>()
            {
                new CardAction(ActionTypes.OpenUrl, "Enter my credentials", null, "Enter my credentials", "Enter my credentials", "Login"),
                new CardAction(ActionTypes.OpenUrl, "Show me the product list", null, "Show me the product list", "Show me the product list", "ProductInfo"),
            };

            response.Attachments = new List<Attachment>() { card.ToAttachment() };
            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Get the state properties from the turn context.
            var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

            var userStateAccessors = _userState.CreateProperty<CustomerShort>(nameof(CustomerShort));
            var shoppingCartAccessors = _userState.CreateProperty<List<ShoppingCart>>(nameof(List<ShoppingCart>));

            var customer = await userStateAccessors.GetAsync(turnContext, () => new CustomerShort());
            var cart = await shoppingCartAccessors.GetAsync(turnContext, () => new List<ShoppingCart>());

            if (string.IsNullOrEmpty(customer.CustomerName))
            {
                if (conversationData.PromptedUserForName)
                {
                    customer.CustomerName = turnContext.Activity.Text?.Trim();
                    await turnContext.SendActivityAsync($"Thanks {customer.CustomerName}.");
                    conversationData.PromptedUserForName = false;
                }
                else
                {
                    await turnContext.SendActivityAsync($"What is your name?");
                    conversationData.PromptedUserForName = true;
                }
            }
            else
            {
                var messageTimeOffset = (DateTimeOffset)turnContext.Activity.Timestamp;
                var localMessageTime = messageTimeOffset.ToLocalTime();
                conversationData.Timestamp = localMessageTime.ToString();
                conversationData.ChannelId = turnContext.Activity.ChannelId.ToString();

                // Display state data.
                //await turnContext.SendActivityAsync($"{customer.CustomerName} sent: {turnContext.Activity.Text}");
                //await turnContext.SendActivityAsync($"Message received at: {conversationData.Timestamp}");
                //await turnContext.SendActivityAsync($"Message received from: {conversationData.ChannelId}");
            }
        }
    }
}
