// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.BotBuilderSamples;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient()
                .AddControllers()
                .AddNewtonsoftJson();

// Adapter & auth
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// State
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<UserState>();
builder.Services.AddSingleton<ConversationState>();

// Dialogs
builder.Services.AddSingleton<CreateTicketDialog>();
builder.Services.AddSingleton<FeedbackDialog>();
builder.Services.AddSingleton<MainDialog>();

// Bot
builder.Services.AddTransient<IBot, TeamsBot<MainDialog>>();
builder.Services.AddHttpClient<Microsoft.BotBuilderSamples.Services.TicketApiClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles()
   .UseStaticFiles()
   .UseRouting()
   .UseAuthorization()
   .UseEndpoints(endpoints =>
   {
       endpoints.MapControllers();
   });

app.Run();