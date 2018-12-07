# 2018 Fsharp Advent

## Christmas Day 2018 weather forecast (5 next days)
For the 2018 fsharp advent, I would like to build an Android and iOS weather mobile app by myself with end to end properties.

I use for sure FSharp and a little framework called [Fabulous](https://fsprojects.github.io/Fabulous/).

<img src="https://pbs.twimg.com/media/DtgylCUW0AANbV1.jpg" width="200" />

I attempt to describe the full process even if you are not a mobile dev nor a fsharp dev. 
It is really hard, so pull requests are accepted!

# What is Fabulous ?
The aim of Fabulous is : "Never write a ViewModel class again!".

Fabulous is not a MVVM architecture it uses in fact a Model View Update architecture inspired by the elm architecture.

There is tons of articles that explain it in details.

- The powerfull of elm architecture with this [famous elm video](https://elm-lang.org/blog/time-travel-made-easy)
- The [elmish architecture](https://elmish.github.io/elmish/)
- Implementations : 
  - Web development with [Fable](https://fable.io/) : fsharp + bable js, by transpiling fsharp
  - App development with [Fabulous](https://fsprojects.github.io/Fabulous/) and [Xamarin](https://visualstudio.microsoft.com/xamarin/) : fsharp + xamarin dynamic form
  
 > What are key concepts ?

## Discrete time
  Events are discrete time, not continuous time : For example when you use timer trigger, by separating the await process and the action by sending commands, you can see each Timeout event and observe the application outside.
It is important, because, now you can keep the state of your architecture without any technical debugging components because you can record the state + the timeline of your domain events. 
  
  In mobile Xamarin development, LiveUpdate could be plugged in an easiest way.

## Domain driven design
  Events are no more technical messages (INotifyPropertyChanged, ...) but domain events. 
This architecture loves domain driven design. In our case we bind the position changed event by sending a command to notify things.

  Those commands are not technical commands but a part of our domain.

## Responsive design
  It is possible to send a command in the future by using fsharp async pattern
  
  Response design + race condition don't exist anymore !

  For the next part, we will replay the development life time of a mobile app.

## Setup
  We use Xamarin, you have to modify Visual Studio by adding Xamarin and fsharp for desktop.
If it is not yet done, please configure your visual studio by following this instructions:

https://docs.microsoft.com/en-us/xamarin/cross-platform/get-started/installation/windows

 > As a mobile app developper, I start with the domain, then view by playing with some mockups and then implement behavior + associated services

## Configure Android emulator
  You can use the android device manager by using a kitkat (lightweight one) emulator to debug.
For iOS, you need a Mac build host (on cloud, or local if you already have a Mac)

## The Domain
  Here is the domain that we will use for our application :

  I need to describe the weather and select the worst weather (8 measures for one day) : 

```fsharp
type Weather = 
    | Sunny
    | PartialCloudy 
    | Cloudy 
    | Rainy 
    | Stormy 
    | Snowy 

module Weather = 
    let worst x y =
        match x, y with
        | Snowy, _ | _, Snowy -> Snowy
        | Stormy, _ | _, Stormy -> Stormy
        | Rainy, _ | _, Rainy -> Rainy
        | Cloudy, _ | _, Cloudy -> Cloudy
        | PartialCloudy, _ | _, PartialCloudy-> PartialCloudy
        | Sunny, _ | _, Sunny -> Sunny
```

I use the GPS sensor to get the current 5 days forecast 

```fsharp
type Position = 
    { Longitude: float
      Latitude: float }
```

The API uses SI unit systems that fsharp has in heart!

I defined my model by using SI unit system, and defined functions that converts SI -> other unit system.
In our app, we use celcius degree instead of Kelvin and kilometer per hour instead meter per second.
Thanks to fsharp, unit of measure and conversion function are strong typed!

```fsharp
open FSharp.Data.UnitSystems.SI.UnitSymbols

let km = 1000m<m>
let hour = 3600m<s>

module Temperature = 
    let kelvin x = x * 1m<K>
    let celcius x = 
        x - 273.15m<K>
        |> decimal

module Speed = 
    let mps x = x * 1m<m/s>
    let kmph (x:decimal<m/s>) = x * hour / km 
```

The main domain that we will use on our app.

```fsharp
type Wind = 
    { Degree : decimal
      Speed : decimal<m/s> }

type Forecast = 
    { Date:System.DateTime
      Weather:Weather
      MinTemp:decimal<K>
      MaxTemp:decimal<K>
      Humidity:int
      Wind:Wind }

type CityForecast = 
    { City:string
      Country:string
      Days:Forecast list }
```

Now that we have the domain in mind, let's start with the next step: designing and building view of our phone app.

## Designing app with type driven development
Let's change our habits by drawing our app with functions.

We will stack in a Page
- A header that contains city and country names
- 5 columns (5 days forecast) that contains the day, weather, temps, humidity, wind

We will use type driven development to build our view :  

```fsharp
///In type driven development we build things step by step without any implementation first
let fiveDaysForecast model = failwith "not yet implemented" 
let home content = failwith "not yet implemented" 

///We built this function by separating things : 5 columns inside a home that contains a background image
let view (model: CityForecast) dispatch = model |> fiveDaysForecast |> home
```

Now we can build the home function by using Fabulous helper over Xamarin forms.

```fsharp
    ///This function contains the Background and the title of the home page
    let home content = 
        View.ContentPage(
          title = "Weather",
          inputTransparent = true,
          backgroundImage = "bluesky.png",
          content = content)
```

We have to build the fiveDaysForecast : 
We can see that the pattern consists of a stack that contains a list of elements.

```fsharp
let fiveDaysForecast (model: CityForecast) = 
	let fiveDays = model.Days |> List.truncate 5
	let city = if model.Country |> String.IsNullOrWhiteSpace then model.City else sprintf "%s, %s" model.City model.Country 
	View.StackLayout(padding = 20.0, verticalOptions = LayoutOptions.FillAndExpand,
		children = [ 
			View.Label(text=city.ToUpper(), textColor=Color.Beige, backgroundColor=Color.FromHex("#0F4D8FAC"), fontSize=40, fontAttributes=FontAttributes.Bold, horizontalTextAlignment=TextAlignment.Center)
			empty 20.
			View.Grid(
				rowdefs=[box "*"],
				coldefs=[ for _ in fiveDays -> box "*" ],
				children = (fiveDays |> List.mapi (day 0) ) )
			])
```
We go deeper and deeper inside our components now we have to build the function day.

You can see that we used the partial application over the column index and the forecast data.

We have now to build the day function.

The parameter x and y are the row and column index for grid forms

```fsharp
let day x y (forecast:Forecast) = 
    View.StackLayout(
        backgroundColor=Color(0., 0., 0., 0.4),
	    children=
			[ forecast.Date |> dayOfWeek |> text 20. Color.Beige 
			  forecast.Date.Day |> ordinalDay |> bold 20. Color.Beige 
			  empty 15.
			  forecast.Weather |> weatherIcon |> image
			  forecast.MaxTemp |> Temperature.celcius |> temp 22. Color.Beige
			  separator Color.SkyBlue
			  forecast.MinTemp |> Temperature.celcius |> temp 22. Color.SkyBlue
			  empty 30.
			  forecast.Humidity |> humidity
			  empty 25.
			  forecast.Wind.Degree |> wind |> text 22. Color.LightBlue
			  empty 5.
			  forecast.Wind.Speed |> Speed.kmph |> int |> sprintf "%i km/h" |> text 14. Color.Beige 
			  empty 10.
			]).GridRow(x).GridColumn(y)
```

We have now the big picture. For example the bold and text functions are customized label that we reuse constantly inside our view!

the rest of the code can be found inside the WeatherApp/WeatherApp.fs

Now that we built a view, we want to deploy / check if it works really. 

Let's start by using a mockup first!

## Mockup

By changing the program function we can supply the init one or mockup.

```fsharp
    let program = Program.mkProgram mockup update view
```
You can locate this function by replacing init by mockup.

The mock part : Note that thanks to unit of measure, our domain is really clear (ubiquitous language!)

```fsharp
open FSharp.Data.UnitSystems.SI.UnitSymbols
let mockup () = 
	{ City="FONTAINEBLEAU"
	  Country="FR"
	  Days=
		[ { Date = DateTime.Now
			Weather = Cloudy
			MinTemp = 270m<K>
			MaxTemp = 277m<K>
			Humidity=98
			Wind = 
				{ Speed = 15m<m/s>
				  Degree = 270m } }

		  { Date = DateTime.Now.AddDays 1.
			Weather = Sunny
			MinTemp = 300m<K>
			MaxTemp = 310m<K>
			Humidity=10
			Wind = 
				{ Speed = 2m<m/s>
				  Degree = 20m } } 
				  
		  { Date = DateTime.Now.AddDays 2.
			Weather = Rainy
			MinTemp = 280m<K>
			MaxTemp = 285m<K>
			Humidity=10
			Wind = 
				{ Speed = 50m<m/s>
				  Degree = 180m } } 
				  
		  { Date = DateTime.Now.AddDays 3.
			Weather = PartialCloudy
			MinTemp = 250m<K>
			MaxTemp = 260m<K>
			Humidity=100
			Wind = 
				{ Speed = 28m<m/s>
				  Degree = 280m } } 
				  
		  { Date = DateTime.Now.AddDays 4.
			Weather = Snowy
			MinTemp = 250m<K>
			MaxTemp = 257m<K>
			Humidity=80
			Wind = 
				{ Speed = 15m<m/s>
				  Degree = 200m } } ] }, Cmd.none
```

You can now debug the application and use the Fabulous Live update feature describe here : https://fsprojects.github.io/Fabulous/tools.html

Now that we have the UI part, lets track position updates!

## Using GPS sensor through GeoLocator Xamarin plugins
The weather app tracks position and display forecast according to your location.

Thanks to the awesome plugins [GeoLocator](https://github.com/jamesmontemagno/GeolocatorPlugin) (very well documented), I transpose the csharp code to a fsharp one.

Here is the fsharp implementation (https://jamesmontemagno.github.io/GeolocatorPlugin/CurrentLocation.html) of the gps locator:

Porcelain and plumbing function to map features to our Domain (Position type)

```fsharp
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
```

When the application starts, I would like to use the last or current position as quick as possible and notify the app to update the weather : 

- Start sensor
- Get the last known position or the current if it was not cached
- Notify a new Position

Here is the fsharp code to do that.

```fsharp
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
```

Because we are using sensors, we have to prompt authorization to the user for ACCESS_FINE_LOCATION and ACCESS_COARSE_LOCATION.
You can configure each target project. This part is well described into the plugin documentation : https://jamesmontemagno.github.io/GeolocatorPlugin/GettingStarted.html

Now that we can track the position of the user, we can now use a weather service

## Weather service
For this app, I use https://openweathermap.org/ api with my own free account, maybe the api key will not be available after the fsharpadvent.
You can subscribe a free account and copy paste the APPID here:

```fsharp
let ApiKey = "760628d3c2b9ede15fb9f57de25cd3ee"
```

Thanks to the fsharp interactive you can hack any api you want.
In our case I use FSharp.Data to get the data through a REST service.

A script is available : weather.fsx

```fsharp
#r "../packages/FSharp.Data.3.0.0/lib/net45/FSharp.Data.dll"
#load "Async.fs"
#load "Domain.fs"
#load "WeatherApi.fs"

open Domain

{ Latitude=48.8713344; Longitude= 2.3462986 } 
|> WeatherApi.get 
|> Async.RunSynchronously
```

I start firstly by putting in place the result of a crafted request inside the forecast.json file.
I use FSharp.Data and defined a JsonProvider
The WeatherApi.fs file contains the implementation of the OpenWeatherMap REST service

This function build the url with our Position : 
```fsharp
let url apiKey latitude longitude = 
    let uri = 
        UriBuilder(
            Scheme="https", 
            Host="api.openweathermap.org",
            Path="data/2.5/forecast",
            Query=sprintf "lat=%f&lon=%f&APPID=%s" latitude longitude apiKey)
    uri.Uri
```

You can quickly check if it works inside the fsharp interactive
```fsharp
url "760628d3c2b9ede15fb9f57de25cd3ee" 48.8713344 2.3462986
|> string
|> Http.RequestString
```

It will return the json.

You can copy the json to a file and continuing you hack with the JsonProvider of FSharp.Data

```fsharp
type ForecastApi = JsonProvider< "forecast.json" >
```

Now we have to convert type coming from Type Provider to our Domain :
OpenWeatherMap supply 8 measures per day. 
I choose to fold measures by taking a pessimistic approach.
The API gives data into SI unit system, we will keep this approach by enforcing typing when we have to convert it.

```fsharp
let convert (forecast:ForecastApi.Root) = 
    let forecasts = 
        forecast.List
        |> Seq.groupBy(fun x -> x.DtTxt.Date)
        |> Seq.map(fun (date, sources) -> 
            { Date = date
              Weather = 
                sources |> Seq.collect(fun w -> w.Weather |> Seq.map (fun x -> x.Icon |> Weather.ofIcon)) 
                |> Seq.fold Weather.worst Weather.Sunny 
              MinTemp = sources |> Seq.map (fun w -> w.Main.TempMin) |> Seq.min |> Temperature.kelvin
              MaxTemp = sources |> Seq.map (fun w -> w.Main.TempMax) |> Seq.max |> Temperature.kelvin
              Humidity = sources |> Seq.map (fun w -> w.Main.Humidity) |> Seq.max
              Wind = 
                { Degree = sources |> Seq.map (fun w -> w.Wind.Deg) |> Seq.max 
                  Speed = sources |> Seq.map (fun w -> w.Wind.Speed) |> Seq.max |> Speed.mps } } )
        |> Seq.toList
    
    { City = forecast.City.Name
      Country = forecast.City.Country
      Days = forecasts }
```

Finally, the clue that take a position in parameters and returns the forecast!

```fsharp
let get position = 
    let u = url ApiKey position.Latitude position.Longitude
    u.ToString () 
    |> Http.AsyncRequestString 
    |> Async.map (ForecastApi.Parse >> convert)
```

You can test this function inside the weather.fsx script

Now we have view and services, we need a clue to bind command to service / model.

## Update function

The role of the update function is to process messages by generating commands and/or an updated model.
In our case, the update replace completely the previous model.

When a position changed, we have to call the service asynchronously and send a command when the Weather is received

In our case, I would like to have a function that transform continuous to discrete time case (PositionChanged message) and the async process but you can build and compose all your communication here.

Here is the interesting part for our app : 
```fsharp
let update msg model =
	match msg with
	| PositionChanged p ->
		model, 
		WeatherApi.get p |> Async.map WeatherReceived 
		|> Async.Catch
		|> Async.map (function 
			| Choice1Of2 x -> Some x 
			| Choice2Of2 _ -> 
				//Add logging here to handle api limitations, network issue, ...
				None)
		|> Cmd.ofAsyncMsgOption
	| WeatherReceived update ->
		update, trackPosition () |> Cmd.ofAsyncMsg
```

Now we have all we need to test our app into an Android Emulator (KiKat)!

In order to use the app in AdHoc mode (by copying the apk and installing to our phone) we have to publish it

## Archive / Test for real!
At this step, the app runs well inside the emulator and you are ready to test for real on your phone!

In our case, you have to right click on WeatherApp.Android project and click Archive.
The process is separated into 2 steps: 
- Build the apk
- Sign the apk with our key (Visual studio will guide you to create a key storage)

When it is done copy the signed apk to our phone and launch it.

It works !

  As a conclusion, we have created an app with views in full fsharp with the powerfull for fsharp for the templating part thanks to Fabulous helper, then created all the behavior and communication with the elmish architecture / Fabulous plumbing very quickly.
  If you are pretty new to mobile dev, keep in mind that this activity requires a lot of patience but Fabulous experience speed up the full process by embedding mature concept.
