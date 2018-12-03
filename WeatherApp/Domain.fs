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


type Wind = 
    { Degree : decimal
      Speed : decimal }

type Forecast = 
    { Date:System.DateTime
      Weather:Weather
      MinTemp:decimal
      MaxTemp:decimal
      Humidity:int
      Wind:Wind }

type CityForecast = 
    { City:string
      Days:Forecast list }

