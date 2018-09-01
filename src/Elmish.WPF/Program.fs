[<RequireQualifiedAccess>]
module Elmish.WPF.Program

open System.Windows
open Elmish
open Elmish.WPF.Internal
open System.Windows.Controls

let private run
    (config: ElmConfig)
    (uiElement: FrameworkElement)  //(window: Window)
    (programRun: Program<'t, 'model, 'msg, BindingSpec<'model, 'msg> list> -> unit)
    (program: Program<'t, 'model, 'msg, BindingSpec<'model, 'msg> list>)
    (uiDispatcher: Threading.Dispatcher) =
  let mutable lastModel = None

  let setState model dispatch =
    match lastModel with
    | None ->
        let mapping = program.view model dispatch
        let vm = ViewModel<'model,'msg>(model, dispatch, mapping, config, uiDispatcher)
        uiElement.DataContext <- vm
        lastModel <- Some vm
    | Some vm ->
        vm.UpdateModel model

  // Start Elmish dispatch loop
  programRun { program with setState = setState }

  match uiElement with
  | :? Window as window ->
    // Start WPF dispatch loop
    let app = if isNull Application.Current then Application() else Application.Current
    app.Run window
  | _ -> 0

/// Starts both Elmish and WPF dispatch loops. Blocking function.
let runWindow window program =
  run ElmConfig.Default window Elmish.Program.run program window.Dispatcher

/// Starts both Elmish and WPF dispatch loops with the specified configuration.
/// Blocking function.
let runWindowWithConfig config window program =
  run config window Elmish.Program.run program window.Dispatcher

let runUserControl (control: UserControl) program =
  run ElmConfig.Default control Elmish.Program.run program control.Dispatcher

let runUserControlWithConfig config (control: UserControl) program =
  run config control Elmish.Program.run program control.Dispatcher
