[<RequireQualifiedAccess>]
module Elmish.WPF.Program

open System.Windows
open Elmish
open Elmish.WPF.Internal

// Start Elmish dispatch loop
let private startLoop
    (config: ElmConfig)
    (element: FrameworkElement)
    (programRun: Program<'t, 'model, 'msg, BindingSpec<'model, 'msg> list> -> unit)
    (program: Program<'t, 'model, 'msg, BindingSpec<'model, 'msg> list>)
    (uiDispatcher: Threading.Dispatcher) =
  let mutable lastModel = None

  let setState model dispatch =
    match lastModel with
    | None ->
        let mapping = program.view model dispatch
        let vm = ViewModel<'model,'msg>(model, dispatch, mapping, config, uiDispatcher)
        element.DataContext <- box vm
        lastModel <- Some vm
    | Some vm ->
        vm.UpdateModel model

  programRun { program with setState = setState }

 // Start WPF dispatch loop. Blocking function.
let private startApp window =
  let app = if isNull Application.Current then Application() else Application.Current
  app.Run window

/// Starts both Elmish and WPF dispatch loops. Blocking function.
let runWindow window program =
  startLoop ElmConfig.Default window Elmish.Program.run program window.Dispatcher
  startApp window

/// Starts both Elmish and WPF dispatch loops with the specified configuration.
let runWindowWithConfig config window program =
  startLoop config window Elmish.Program.run program window.Dispatcher
  startApp window

/// start an Elmish dispatch loop and bind it to a framework element, such as window or a user control
let bindFrameWorkElement element program =
  startLoop ElmConfig.Default element Elmish.Program.run program element.Dispatcher

/// start an Elmish dispatch loop and bind it to a framework element, such as window or a user control, with the specified configuration.
let bindFrameWorkElementWithConfig config element program =
  startLoop config element Elmish.Program.run program element.Dispatcher


