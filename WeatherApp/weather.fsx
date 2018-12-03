#r """C:\Users\Clément\.nuget\packages\fsharp.data\3.0.0\lib\net45\FSharp.Data.dll"""
#load "Async.fs"
#load "Domain.fs"
#load "WeatherApi.fs"

WeatherApi.get 48.8713344 2.3462986 |> Async.RunSynchronously