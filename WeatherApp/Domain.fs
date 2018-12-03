module Domain

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

type Position = 
    { Longitude: float
      Latitude: float }

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
      Days:Forecast list }

