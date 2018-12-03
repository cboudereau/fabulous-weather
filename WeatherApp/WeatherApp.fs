﻿// Copyright 2018 Fabulous contributors. See LICENSE.md for license.
namespace WeatherApp

open System.Diagnostics
open Fabulous.Core
open Fabulous.DynamicViews
open Xamarin.Forms
open Plugin.Geolocator

module App = 
    open System
    open Domain

    type Msg = 
        | PositionChanged of Position
        | WeatherReceived of CityForecast

    let initModel = { City= "Waiting signal.."; Days=[] }

    let locator = CrossGeolocator.Current
    locator.DesiredAccuracy <- 100.
    
    let ofPosition (p:Abstractions.Position) = { Longitude=p.Longitude; Latitude=p.Latitude } 
    let ofPositionArg (p:Abstractions.PositionEventArgs) = p.Position |> ofPosition

    let startTracking timeout = 
        if locator.IsListening then Async.ret true
        else
            locator.StartListeningAsync(timeout, 0.) 
            |> Async.AwaitTask 
    
    let lastPosition () = 
        locator.GetLastKnownLocationAsync()
        |> Async.AwaitTask
        |> Async.map (Option.ofObj >> Option.map ofPosition)

    let currentPosition () = 
        locator.GetPositionAsync()
        |> Async.AwaitTask
        |> Async.map ofPosition

    let trackPosition () = 
        locator.PositionChanged 
        |> Async.AwaitEvent
        |> Async.map (ofPositionArg >> PositionChanged)

    let init () = 
        let cmd = 
            5. |> TimeSpan.FromSeconds 
            |> startTracking 
            |> Async.bind (fun isListening ->
                if isListening then
                    lastPosition ()
                    |> Async.bind(Option.map Async.ret >> Option.defaultWith currentPosition)
                    |> Async.map (PositionChanged >> Some)
                else Async.ret None)
            |> Cmd.ofAsyncMsgOption
        initModel, cmd 

    let update msg model =
        match msg with
        | PositionChanged p ->
            model, WeatherApi.get p |> Async.map WeatherReceived |> Cmd.ofAsyncMsg
        | WeatherReceived update ->
            update, trackPosition () |> Cmd.ofAsyncMsg
    
    let empty height = 
        View.BoxView(
            color=Color(0.,0.,0.,0.),
            heightRequest=height)

    let day x y (forecast:Forecast) = 
        let basetext fontAttributes size color text = 
            View.Label(text=text, textColor=color, fontSize=size, fontAttributes=fontAttributes, horizontalTextAlignment=TextAlignment.Center) 
        
        let text = basetext FontAttributes.None
        let bold = basetext FontAttributes.Bold

        let temp size color t = sprintf "%i°" (int t) |> text size color
        
        let separator color = 
            View.BoxView(
                color=color, 
                heightRequest=1.)

        let humidity h = sprintf "H %i%%" h |> text 18. Color.Beige
        
        let weatherIcon = function
            | Cloudy -> "cloudy.png"
            | PartialCloudy -> "partial_cloudy.png"
            | Rainy -> "rainy.png"
            | Snowy -> "snowy.png"
            | Stormy -> "stormy.png"
            | Sunny -> "sunny.png"
        
        let image source = View.Image(source=source)

        let wind = function
            | x when x < 45m -> "N"
            | x when x < 90m -> "N-E"
            | x when x < 135m -> "E"
            | x when x < 180m -> "S-E"
            | x when x < 225m -> "S"
            | x when x < 270m -> "S-W"
            | x when x < 325m -> "W"
            | x when x < 360m -> "N-W"
            | other -> failwithf "unexpected %f degree" other
        
        let dayOfWeek (d:DateTime) = d.ToString("ddd")

        let day d = 
            match d, d % 10 with
            | x, _ when x >= 10 && x <= 13  -> sprintf "%ith" x
            | x, 1 -> sprintf "%ist" x
            | x, 2 -> sprintf "%ind" x
            | x, 3 -> sprintf "%ird" x
            | other,_ -> sprintf "%ith" other

        View.StackLayout(
            backgroundColor=Color(0., 0., 0., 0.4),
            children=
                [ forecast.Date |> dayOfWeek |> text 20. Color.Beige 
                  forecast.Date.Day |> day |> bold 20. Color.Beige 
                  empty 15.
                  forecast.Weather |> weatherIcon |> image
                  empty 15.
                  forecast.MaxTemp |> Temperature.celcius |> temp 22. Color.Beige
                  separator Color.SkyBlue
                  forecast.MinTemp |> Temperature.celcius |> temp 22. Color.SkyBlue
                  empty 30.
                  forecast.Humidity |> humidity
                  empty 25.
                  forecast.Wind.Degree |> wind |> text 22. (Color.OrangeRed) 
                  empty 5.
                  forecast.Wind.Speed |> Speed.kmph |> int |> sprintf "%i km/h" |> text 18. Color.Beige 
                ]).GridRow(x).GridColumn(y)
        
    let home content = 
        View.ContentPage(
          title = "Weather",
          inputTransparent = true,
          backgroundImage = "bluesky.png",
          content = content)
    
    let fiveDaysForecast (model: CityForecast) = 
        let fiveDays = model.Days |> List.truncate 5
        View.StackLayout(padding = 20.0, verticalOptions = LayoutOptions.FillAndExpand,
            children = [ 
                View.Label(text=model.City.ToUpper(), textColor=Color.Beige, backgroundColor=Color.FromHex("#0F4D8FAC"), fontSize=50, horizontalTextAlignment=TextAlignment.Center)
                empty 20.
                View.Grid(
                    rowdefs=["*"],
                    coldefs=[ for _ in fiveDays -> "*" ],
                    children = (fiveDays |> List.mapi (day 0) ) )
                ])

    let view (model: CityForecast) dispatch = model |> fiveDaysForecast |> home

    // Note, this declaration is needed if you enable LiveUpdate
    let program = Program.mkProgram init update view

type App () as app = 
    inherit Application ()

    let runner = 
        App.program
#if DEBUG
        |> Program.withConsoleTrace
#endif
        |> Program.runWithDynamicView app

#if DEBUG
    // Uncomment this line to enable live update in debug mode. 
    // See https://fsprojects.github.io/Fabulous/tools.html for further  instructions.
    //
    //do runner.EnableLiveUpdate()
#endif    

    // Uncomment this code to save the application state to app.Properties using Newtonsoft.Json
    // See https://fsprojects.github.io/Fabulous/models.html for further  instructions.
#if APPSAVE
    let modelId = "model"
    override __.OnSleep() = 

        let json = Newtonsoft.Json.JsonConvert.SerializeObject(runner.CurrentModel)
        Console.WriteLine("OnSleep: saving model into app.Properties, json = {0}", json)

        app.Properties.[modelId] <- json

    override __.OnResume() = 
        Console.WriteLine "OnResume: checking for model in app.Properties"
        try 
            match app.Properties.TryGetValue modelId with
            | true, (:? string as json) -> 

                Console.WriteLine("OnResume: restoring model from app.Properties, json = {0}", json)
                let model = Newtonsoft.Json.JsonConvert.DeserializeObject<App.Model>(json)

                Console.WriteLine("OnResume: restoring model from app.Properties, model = {0}", (sprintf "%0A" model))
                runner.SetCurrentModel (model, Cmd.none)

            | _ -> ()
        with ex -> 
            App.program.onError("Error while restoring model found in app.Properties", ex)

    override this.OnStart() = 
        Console.WriteLine "OnStart: using same logic as OnResume()"
        this.OnResume()
#endif
