module TestPathSolver

open Xunit
open Hedgehog
open Swensen.Unquote

open Elmish
open Elmish.WPF

open System
open System.Windows
open System.Windows.Controls
open System.Threading
open Elmish.WPF
open Elmish.WPF.Internal

type Simple = { Property1: string }
type Message = | Void of (unit -> unit) | Void2 of string * Message
let update _ model = model


let _startLoop
      (programRun: Program<'t, 'model, 'msg, BindingSpec<'model, 'msg> list> -> unit)
      (program: Program<'t, 'model, 'msg, BindingSpec<'model, 'msg> list>) =
    let mutable lastModel = None
    let config = ElmConfig.Default

    let setState model dispatch =
      match lastModel with
      | None ->
          let mapping = program.view model dispatch
          let vm = ViewModel<'model,'msg>(model, dispatch, mapping, config, Windows.Threading.Dispatcher.CurrentDispatcher)
          lastModel <- Some vm
      | Some vm ->
          vm.UpdateModel model

    programRun { program with setState = setState }
    lastModel.Value

let startLoop p = _startLoop Elmish.Program.run p

let startTestThread f =
  let t = Thread(ThreadStart f)
  t.SetApartmentState ApartmentState.STA
  t.Start()
  t.Join()

[<Fact>]
let ``path solver can retrieve simple property`` () =
  let _test () =
    Property.check <| property {
      let loop =
        (fun _ _ -> [ "Property1" |> Binding.oneWay (fun m -> "prop1") ])
        |> Program.mkSimple (fun _ -> ()) update
                          
      let value = ViewModel.resolvePath "Property1" (startLoop loop)
      test <@ value |> unbox = "prop1" @>
    }
  startTestThread _test
    


[<Fact>]
let ``path solver can retrieve sub property`` () =
  let _test () =
    Property.check <| property {
        let loop =
          (fun _ _ -> [ "Property1" |> Binding.subModel
                                            (fun _ -> ())
                                            (fun () -> [  "Property2" |> Binding.oneWay (fun m -> 42.0) ])
                                            Void    ])
          |> Program.mkSimple (fun _ -> ()) update
            
        let value = ViewModel.resolvePath "Property1.Property2" (startLoop loop)
        test <@ value |> unbox = 42.0 @>
        }
  startTestThread _test
   
    

[<Fact>]
let ``path solver can retrieve indexed property on oneWaySeq`` () =
  let _test () =
    Property.check <| property {
      let loop = 
        Program.mkSimple
          (fun _ -> ())
          update
          (fun _ _ -> [ "Property1" |> Binding.oneWaySeq
                                          (fun _ -> [ "zéro" ; "un" ; "deux" ])
                                          id
                                          (=)     ])
      let value = ViewModel.resolvePath "Property1[1]" (startLoop loop)
      test <@ value |> unbox = "un" @>
      }
  startTestThread _test

[<Fact>]
let ``path solver can retrieve keyed property on subModelSeq`` () =
  let _test () = 
    Property.check <| property {
    
      let loop =
        Program.mkSimple
          (fun _ -> ())
          update
          (fun _ _ -> [ "Property1" |> Binding.subModelSeq
                                          (fun _ -> [ ("one","first") ; ("two","second") ; ("three","third") ])
                                          (fun x -> fst x)
                                          (fun () -> [  "__" |> Binding.oneWay (fun x -> ()) ])
                                          Void2                      ])
      let value = ViewModel.resolvePath "Property1[three]" (startLoop loop)
      test <@ value |> unbox |> ViewModel.currentModel<_> = ("three","third") @>
    }
  startTestThread _test

[<Fact>]
let ``path solver can retrieve sub-property of keyed property on subModelSeq`` () =
  let _test () = 
    Property.check <| property {
      let loop =
        Program.mkSimple
          (fun _ -> ())
          update
          (fun _ _ -> [ "Property1" |> Binding.subModelSeq
                                          (fun _ -> [ ("one","first") ; ("two","second") ; ("three","third") ])
                                          (fun x -> fst x)
                                          (fun () -> [  "Property2" |> Binding.oneWay (fun x -> snd x) ])
                                          Void2
                      ])
      
      let value = ViewModel.resolvePath "Property1[two].Property2" (startLoop loop)
      test <@ value |> unbox = "second" @>
    }
  startTestThread _test

