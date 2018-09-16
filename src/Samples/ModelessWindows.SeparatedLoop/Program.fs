open System
open Elmish
open Elmish.WPF
open ModelessWindows.Views
open System.Collections.Generic
open System.Windows

module VM = Elmish.WPF.ViewModel

type Person = { Id: Guid ; NickName: string ; FirstName: string ; LastName: string }

module Person =
  type Message =
    | Nothing
    | ChangeNickName of string
    | ChangeFirstName of string
    | ChangeLastName of string
  let init nick first last =  { Id = Guid.NewGuid() ; NickName = nick ; FirstName = first ; LastName = last  }
  let update msg model =
    match msg with
    | Nothing -> model
    | ChangeNickName nickName -> { model with NickName = nickName }
    | ChangeFirstName firstname -> { model with FirstName = firstname }
    | ChangeLastName lastname -> { model with LastName = lastname }

  let bindings () =
    [ "Id" |> Binding.oneWay (fun (m:Person) -> m.Id)
      "NickName" |> Binding.twoWay (fun m -> m.NickName) (fun v _ -> ChangeNickName v)
      "FirstName" |> Binding.twoWay (fun m -> m.FirstName) (fun v _ -> ChangeFirstName v)
      "LastName" |> Binding.twoWay (fun m -> m.LastName) (fun v _ -> ChangeLastName v)  ]
  
type Crowd = { Persons: Person list }

module Crowd =
  type Message =
    | Refresh
    | PersonMessage of Id: Guid * Msg: Person.Message
    | InsertOrUpdate of Person
    | Delete of Person

  let init () =
    { Persons = [ Person.init "JDiLenarda" "Julien" "Di Lenarda"
                  Person.init "cmeeren" "Christer" "Van der Meeren"
                  Person.init "giuliohome" "?" "?"
                  Person.init "JohnStov" "John" "Stovin"
                  Person.init "2sComplement" "Justin" "Sacks" ] }

  let update msg model =
    let deletePerson crowd person =
      { crowd with Persons = crowd.Persons |> List.filter (fun p -> p.Id <> person.Id) }
    let insertOrUpdatePerson crowd person =
      { crowd with Persons = crowd.Persons |> List.filter (fun p -> p.Id <> person.Id) |> List.append [ person ] }
    match msg with
    | Refresh
    | PersonMessage _ -> model
    | InsertOrUpdate person -> insertOrUpdatePerson model person 
    | Delete person -> deletePerson model person

open Crowd

let bindings window _ dispatch =
  let editors = new Dictionary<Guid,PeopleEditorSubmit>()

  let callEditor person =
    if not (editors.ContainsKey person.Id) then
      let editor = new PeopleEditorSubmit ()
      editor.Owner <- window
      editor.Closed.Add (fun _ -> editors.Remove person.Id |> ignore ; dispatch Refresh)

      Program.mkSimple (fun () -> person) Person.update (fun _ _ -> Person.bindings ())
      |> Program.bindFrameWorkElement editor

      editors.Add(person.Id, editor)
      editors.[person.Id].Show()
    else
      editors.[person.Id].Activate() |> ignore

  let closeEditor id = editors.[id].Close()

  let isEditorOpen id = editors.ContainsKey id

  let confirmDelete person =
    MessageBox.Show("do you want to delete " + person.NickName + " ?", "Delete person", MessageBoxButton.YesNo)
    |> function | MessageBoxResult.Yes -> (if editors.ContainsKey person.Id then editors.[person.Id].Close()) ; Delete person
                | _ -> Refresh

  [ "Persons" |> Binding.subModelSeq (fun m -> m.Persons |> List.sortByDescending (fun m -> m.NickName))
                                     (fun sm -> sm.Id)
                                     Person.bindings
                                     PersonMessage
    "AddPerson" |> Binding.cmd (fun _ -> callEditor (Person.init "New person" "" "") ; Refresh)
    "EditPerson" |> Binding.paramCmd (fun v _ ->  callEditor (VM.currentModel<_> v) ; Refresh) 
    "DeletePerson" |> Binding.paramCmdIf (fun v _ -> v |> VM.currentModel<_> |> confirmDelete)
                                         (fun v _ -> (isNull v) || not (isEditorOpen (VM.currentModel<_> v).Id))
                                         false
    "Submit" |> Binding.paramCmd (fun v m -> v |> VM.currentModel<_> |> InsertOrUpdate )
    "SubmitAndClose" |> Binding.paramCmd (fun v _ ->  let person = VM.currentModel<_> v
                                                      closeEditor person.Id
                                                      InsertOrUpdate person )  ]
[<EntryPoint;STAThread>]
let main argv =
    let mainWindow = new MainWindow()
    Program.mkSimple Crowd.init Crowd.update (bindings mainWindow)
    |> Program.withConsoleTrace
    |> Program.runWindow mainWindow
