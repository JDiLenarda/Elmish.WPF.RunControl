[<RequireQualifiedAccess>]
module Elmish.WPF.Program

open System.Windows

 // Start WPF dispatch loop. Blocking function.
let private startApp window =
  let app = if isNull Application.Current then Application() else Application.Current
  app.Run window

/// Starts both Elmish and WPF dispatch loops. Blocking function.
let runWindow window program =
  ViewModel.startLoop ElmConfig.Default window Elmish.Program.run program
  startApp window

/// Starts both Elmish and WPF dispatch loops with the specified configuration.
let runWindowWithConfig config window program =
  ViewModel.startLoop config window Elmish.Program.run program
  startApp window

/// start an Elmish dispatch loop and bind it to a framework element, such as window or a user control
let bindFrameWorkElement element program =
  ViewModel.startLoop ElmConfig.Default element Elmish.Program.run program

/// start an Elmish dispatch loop and bind it to a framework element, such as window or a user control, with the specified configuration.
let bindFrameWorkElementWithConfig config element program =
  ViewModel.startLoop config element Elmish.Program.run program


