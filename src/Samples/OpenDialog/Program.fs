open System
open Elmish
open Elmish.WPF
open OpenDialog.Views
open System.Globalization
open System.Windows

let cultures =
  [ "fr-FR" ; "en-GB" ; "en-US" ; "es-ES" ; "ru-RU" ; "ja-JA" ; "zh-CN" ; "it-IT" ; "de-DE" ; CultureInfo.CurrentCulture.Name ]
  |> List.distinct
  |> List.sort

type Model = {  Time: DateTime
                Culture: CultureInfo }

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

let mainWindow = new MainWindow()

(*  When performed in the command part of the loop, such a call blocks the update loop and let the messages queuing,
    unless you run it in an async mode. Also, it has to be done in the application thread dispatcher.
    When performed from a Binding.cmd, it automatically run asynchronously in the WPF dispatch loop.
    That's all fine for a dialog using its parent's update loop, though there is still the need to get parent DataContext *)
let callSameLoopDialog model dispatch = 
    let selector = CultureSelector()
    selector.DataContext <- mainWindow.DataContext
    let currentCulture = model.Culture
    if selector.ShowDialog().Value = false then
        ChangeCulture currentCulture |> dispatch

let reset dispatch =
    let res = MessageBox.Show(mainWindow,
                                "Do you want to reset culture ?", 
                                "Reset culture", 
                                MessageBoxButton.YesNo)
    if res = MessageBoxResult.Yes then 
        ChangeCulture CultureInfo.CurrentCulture |> dispatch

type SubModel = CultureInfo
type SubMessage = | SubChangeCulture of CultureInfo
let callSelfLoopDialog culture dispatch =
    let init () = culture
    let update (SubChangeCulture newCulture) _ = newCulture
    let bindings _ _ =
        [ "Cultures" |> Binding.oneWay (fun _ -> cultures)
          "SelectedCulture" |> Binding.twoWay (fun (m:SubModel) -> m.Name)
                                              (fun v _ -> CultureInfo.CreateSpecificCulture v |> SubChangeCulture) ]
    Program.mkSimple init update bindings
    |> Program.withConsoleTrace
    |> Window.showDialog (new CultureSelector())
    |> function
    | (true,model) -> ChangeCulture model |> dispatch
    | (false,_) -> ()

let bindings _ dispatch =
    [   "TimeStamp" |> Binding.oneWay (fun m -> m.Time.ToString("F", m.Culture))
        "Cultures" |> Binding.oneWay (fun _ -> cultures)
        "SelectedCulture" |> Binding.twoWay (fun m -> m.Culture.Name)
                                            (fun v _ -> CultureInfo.CreateSpecificCulture v |> ChangeCulture)
        "CallSameLoopDialog" |> Binding.cmd (fun m -> callSameLoopDialog m dispatch ; Nothing)
        "Reset" |> Binding.cmd (fun _ -> reset dispatch ; Nothing)
        "CallSelfLoopDialog" |> Binding.cmd (fun m -> callSelfLoopDialog m.Culture dispatch ; Nothing)
    ]

let timerTick dispatch =
  let timer = new System.Timers.Timer(1000.)
  timer.Elapsed.Add (fun _ -> dispatch <| Tick DateTime.Now)
  timer.Start()

[<EntryPoint;STAThread>]
let main argv = 
    Program.mkSimple init update bindings
    |> Program.withSubscription (fun _ -> Cmd.ofSub timerTick)
    |> Program.withConsoleTrace
    |> Program.runWindow (mainWindow)
