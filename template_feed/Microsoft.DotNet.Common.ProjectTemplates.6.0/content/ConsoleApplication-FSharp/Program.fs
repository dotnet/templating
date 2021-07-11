// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System

// Define a function to construct a message to print
let helloFrom whom =
  $"Hello world from {whom}"

[<EntryPoint>]
let main argv =
    // Get the caller from the launch arguments, or a default
    let caller = if Array.length argv > 0 then argv.[0] else "F#"
    // Call the function
    printfn "%s" (helloFrom caller)
    0 // return an integer exit code

