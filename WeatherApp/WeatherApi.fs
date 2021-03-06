﻿module WeatherApi

open System
open Domain
open FSharp.Data

module Weather = 
    let ofIcon = function 
        | "01d" | "01n" -> Sunny
        | "02d" | "02n" -> PartialCloudy
        | "03d" | "03n" | "04d" | "04n" | "50d" | "50n" -> Cloudy
        | "09d" | "09n" | "10d" | "10n" -> Rainy
        | "11d" | "11n" -> Stormy
        | "13d" | "13n" -> Snowy
        | other -> failwithf "unexpected icon code %s, check the icons according to https://openweathermap.org/weather-conditions" other
    
type ForecastApi = JsonProvider< "forecast.json" >

let url apiKey latitude longitude = 
    let uri = 
        UriBuilder(
            Scheme="https", 
            Host="api.openweathermap.org",
            Path="data/2.5/forecast",
            Query=sprintf "lat=%f&lon=%f&APPID=%s" latitude longitude apiKey)
    uri.Uri

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

/// This apikey will be available during the fsharp advent. If you want to continue, please subscribe by using a free account to https://openweathermap.org
let ApiKey = "760628d3c2b9ede15fb9f57de25cd3ee"

let get position = 
    let u = url ApiKey position.Latitude position.Longitude
    u.ToString () 
    |> Http.AsyncRequestString 
    |> Async.map (ForecastApi.Parse >> convert)