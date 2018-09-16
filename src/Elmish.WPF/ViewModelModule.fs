namespace Elmish.WPF

[<RequireQualifiedAccess>]
module ViewModel =

  open Microsoft.CSharp.RuntimeBinder
  open System.Linq.Expressions

  open System
  open System.Windows
  open System.Windows.Controls
  open System.Windows.Data

  open Elmish
  open Elmish.WPF
  open Elmish.WPF.Internal
  

  // Basic utilities

  /// retrieve data context from a FrameWorkElement
  let ofFrameWorkElement (element:FrameworkElement) = element.DataContext
  /// retrieve data context from a Window
  let ofWindow (window:Window) = ofFrameWorkElement window
  /// retrieve data context from a UserControl
  let ofUserControl (control:UserControl) = ofFrameWorkElement control
  /// bind data context to FrameworkElement
  let bindTo (element:FrameworkElement) vm = element.DataContext <- vm

  /// retrieve the current model from the data context
  let currentModel<'mdl> vm =(unbox<IBoxedViewModel> vm).CurrentModel |> unbox<'mdl>

  /// Start Elmish dispatch loop
  let startLoop
      (config: ElmConfig)
      (element: FrameworkElement)
      (programRun: Program<'t, 'model, 'msg, BindingSpec<'model, 'msg> list> -> unit)
      (program: Program<'t, 'model, 'msg, BindingSpec<'model, 'msg> list>) =
    let mutable lastModel = None

    let setState model dispatch =
      match lastModel with
      | None ->
          let mapping = program.view model dispatch
          let vm = ViewModel<'model,'msg>(model, dispatch, mapping, config, element.Dispatcher)
          element.DataContext <- box vm
          lastModel <- Some vm
      | Some vm ->
          vm.UpdateModel model

    programRun { program with setState = setState }

  // Path finding
  let resolvePath (path:string) source =
    let ctrl = ContentControl()
    ctrl.DataContext <- source
    ctrl.SetBinding(ContentControl.ContentProperty, path) |> ignore
    ctrl.Content
