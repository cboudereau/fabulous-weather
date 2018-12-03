﻿namespace WeatherApp.Android

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Android.App

// the name of the type here needs to match the name inside the ResourceDesigner attribute
type Resources = WeatherApp.Android.Resource
[<assembly: Android.Runtime.ResourceDesigner("WeatherApp.Android.Resources", IsApplication=true)>]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[<assembly: AssemblyTitle("WeatherApp.Android")>]
[<assembly: AssemblyDescription("")>]
[<assembly: AssemblyConfiguration("")>]
[<assembly: AssemblyCompany("")>]
[<assembly: AssemblyProduct("WeatherApp.Android")>]
[<assembly: AssemblyCopyright("Copyright ©  2014")>]
[<assembly: AssemblyTrademark("")>]
[<assembly: AssemblyCulture("")>]
[<assembly: ComVisible(false)>]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [<assembly: AssemblyVersion("1.0.*")>]
[<assembly: AssemblyVersion("1.0.0.0")>]
[<assembly: AssemblyFileVersion("1.0.0.0")>]

// Add some common permissions, these can be removed if not needed
[<assembly: UsesPermission(Android.Manifest.Permission.Internet)>]
[<assembly: UsesPermission(Android.Manifest.Permission.AccessCoarseLocation)>]
[<assembly: UsesPermission(Android.Manifest.Permission.AccessFineLocation)>]
[<assembly: UsesPermission(Android.Manifest.Permission.WriteExternalStorage)>]
[<assembly: UsesFeature("android.hardware.location", Required = false)>]
[<assembly: UsesFeature("android.hardware.location.gps", Required = false)>]
[<assembly: UsesFeature("android.hardware.location.network", Required = false)>]
do()
