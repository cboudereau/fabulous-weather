// Copyright 2018 Fabulous contributors. See LICENSE.md for license.
namespace WeatherApp

open System.Diagnostics
open Fabulous.Core
open Fabulous.DynamicViews
open Xamarin.Forms
open Plugin.Geolocator

module App = 
    open System
    open Domain

    type Model = 
      { City:string
        Coords: Position }

    type Msg = 
        | NewPosition of Position

    let initModel = { City= "Samoreau"; Coords= { Longitude=0.; Latitude=0. } }

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
        |> Async.map (ofPositionArg >> NewPosition)

    let init () = 
        let cmd = 
            5. |> TimeSpan.FromSeconds 
            |> startTracking 
            |> Async.bind (fun isListening ->
                if isListening then
                    lastPosition ()
                    |> Async.bind(Option.map Async.ret >> Option.defaultWith currentPosition)
                    |> Async.map (NewPosition >> Some)
                else Async.ret None)
            |> Cmd.ofAsyncMsgOption
        initModel, cmd 

    let update msg model =
        match msg with
        | NewPosition p ->
           { model with Coords = p }, trackPosition () |> Cmd.ofAsyncMsg
    
    let empty height = 
        View.BoxView(
            color=Color(0.,0.,0.,0.),
            heightRequest=height)

    let day x y = 
        let text size color text = 
            View.Label(text=text, textColor=color, fontSize=size, horizontalTextAlignment=TextAlignment.Center) 
        
        let temp size color t = sprintf "%i°" t |> text size color
        
        let separator color = 
            View.BoxView(
                color=color, 
                heightRequest=1.)

        let humidity h = sprintf "H %i%%" h |> text 18. Color.Beige

        View.StackLayout(
            backgroundColor=Color(0., 0., 0., 0.4),
            children=
                [ text 20. Color.Beige "Mon"
                  View.Label(text="2nd", textColor=Color.Beige, fontSize=20., fontAttributes=FontAttributes.Bold, horizontalTextAlignment=TextAlignment.Center)
                  empty 15.
                  View.Image(source="stormy.png")
                  empty 15.
                  temp 22. Color.Beige 20
                  separator Color.SkyBlue
                  temp 22. Color.SkyBlue 14
                  empty 30.
                  humidity 98
                  empty 25.
                  text 22. (Color.OrangeRed) "S-W"
                  empty 5.
                  text 18. Color.Beige "55 km/h"
                ]).GridRow(x).GridColumn(y)
        
    
    let view (model: Model) dispatch =
        View.ContentPage(
          title = "Weather",
          inputTransparent = true,
          backgroundImage = "bluesky.png",
          content = View.StackLayout(padding = 20.0, verticalOptions = LayoutOptions.FillAndExpand,
            children = [ 
                View.Label(text="SAMOREAU", textColor=Color.Beige, backgroundColor=Color.FromHex("#0F4D8FAC"), fontSize=50, horizontalTextAlignment=TextAlignment.Center)
                empty 20.
                View.Grid(
                    rowdefs=["*"],
                    coldefs=[ for _ in 1..5 -> "*" ],
                    children=
                        [ day 0 0
                          day 0 1
                          day 0 2
                          day 0 3
                          day 0 4 ]
                    )
            ]))

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
