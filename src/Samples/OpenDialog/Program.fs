open System
open Elmish
open Elmish.WPF
open OpenDialog.Views
open System.Globalization
open System.Windows

module VM = Elmish.WPF.ViewModel

let cultures =
  [ "fr-FR" ; "en-GB" ; "en-US" ; "es-ES" ; "ru-RU" ; "ja-JA" ; "zh-CN" ; "it-IT" ; "de-DE" ; CultureInfo.CurrentCulture.Name ]
  |> List.distinct
  |> List.sort

type Model = {  Time: DateTime ; Culture: CultureInfo }

type Message =
  | Tick of DateTime
  | ChangeCulture of CultureInfo
  | Nothing

let init () = { Time = DateTime.Now ; Culture = CultureInfo.CurrentCulture }

let update msg model =
  match msg with
  | Tick dateTime ->  {model with Time = DateTime.Now }
  | ChangeCulture cultureInfo -> { model with Culture = cultureInfo }
  | Nothing -> model

module SameLoop =
  (*  When performed in the command part of the loop, such a call blocks the update loop and let the messages queuing,
      unless you run it in an async mode. Also, it has to be done in the application thread dispatcher.
      When performed from a Binding.cmd, it automatically run asynchronously in the WPF dispatch loop.
      That's all fine for a dialog using its parent's update loop, though there is still the need to get parent DataContext *)
  let showDialog parent dispatch model = 
    let selector = CultureSelector()
    selector.Owner <- parent
    parent |> VM.ofWindow |> VM.bindTo selector
    let currentCulture = model.Culture
    if selector.ShowDialog().Value = false then ChangeCulture currentCulture |> dispatch

module SelfLoop =
  type SubModel = CultureInfo
  type SubMessage = | SubChangeCulture of CultureInfo

  let private _showDialog parent program =      
    let window = CultureSelector()      // take note that CultureSelector has a few line of C# code-behind
    window.Owner <- parent
    Program.bindFrameWorkElement window program
    window.ShowDialog().Value
    |> function | true -> Choice1Of2 (window |> VM.ofWindow |> VM.currentModel<SubModel>)
                | false -> Choice2Of2 ()

  let showDialog parent dispatch culture =
    let init () = culture
    let update (SubChangeCulture newCulture) _ = newCulture
    let bindings _ _ =
        [ "Cultures" |> Binding.oneWay (fun _ -> cultures)
          "SelectedCulture" |> Binding.twoWay (fun (m:SubModel) -> m.Name)
                                              (fun v _ -> CultureInfo.CreateSpecificCulture v |> SubChangeCulture) ]
    Program.mkSimple init update bindings
    |> Program.withConsoleTrace
    |> _showDialog parent
    |> function | Choice1Of2 model -> ChangeCulture model |> dispatch
                | Choice2Of2 _ -> ()

let reset parent dispatch =
  let res = MessageBox.Show(parent, "Do you want to reset culture ?", "Reset culture", MessageBoxButton.YesNo)
  if res = MessageBoxResult.Yes then ChangeCulture CultureInfo.CurrentCulture |> dispatch

let bindings parent _ dispatch =
  let callSameLoopDialog' = SameLoop.showDialog parent dispatch
  let callSelfLoopDialog' = SelfLoop.showDialog parent dispatch
  let reset' = reset parent

  [   "TimeStamp" |> Binding.oneWay (fun m -> m.Time.ToString("F", m.Culture))
      "Cultures" |> Binding.oneWay (fun _ -> cultures)
      "SelectedCulture" |> Binding.twoWay (fun m -> m.Culture.Name) (fun v _ -> CultureInfo.CreateSpecificCulture v |> ChangeCulture)
      "CallSameLoopDialog" |> Binding.cmd (fun m -> callSameLoopDialog' m ; Nothing)
      "CallSelfLoopDialog" |> Binding.cmd (fun m -> callSelfLoopDialog' m.Culture ; Nothing)
      "Reset" |> Binding.cmd (fun _ -> reset' dispatch ; Nothing)   ]

let timerTick dispatch =
  let timer = new System.Timers.Timer(1000.)
  timer.Elapsed.Add (fun _ -> dispatch <| Tick DateTime.Now)
  timer.Start()

[<EntryPoint;STAThread>]
let main argv =
  let mainWindow = new MainWindow()
  Program.mkSimple init update (bindings mainWindow)
  |> Program.withSubscription (fun _ -> Cmd.ofSub timerTick)
  //|> Program.withConsoleTrace
  |> Program.runWindow (mainWindow)
