[<AutoOpen;RequireQualifiedAccess>]
module Async

let inline bind f x = async.Bind(x, f)
let inline map f x = async.Bind(x, f >> async.Return)
let inline ret x = async.Return x