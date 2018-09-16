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
    [ "Id" |> Binding.oneWay (fun m -> m.Id)
      "NickName" |> Binding.twoWay (fun m -> m.NickName) (fun v _ -> ChangeNickName v)
      "FirstName" |> Binding.twoWay (fun m -> m.FirstName) (fun v _ -> ChangeFirstName v)
      "LastName" |> Binding.twoWay (fun m -> m.LastName) (fun v _ -> ChangeLastName v) ]
  
type Crowd = { Persons: Person list }

module Crowd =
  type Message =
    | Refresh
    | PersonMessage of Id: Guid * Msg: Person.Message
    | Add of Person
    | Delete of Person
  let init () =
    { Persons = [ Person.init "JDiLenarda" "Julien" "Di Lenarda"
                  Person.init "cmeeren" "Christer" "Van der Meeren"
                  Person.init "giuliohome" "?" "?"
                  Person.init "JohnStov" "John" "Stovin"
                  Person.init "2sComplement" "Justin" "Sacks"   ] }
  let update msg model =
    let deletePerson crowd person =
      { crowd with Persons = crowd.Persons |> List.filter (fun p -> p.Id <> person.Id) }
    let insertOrUpdatePerson crowd person  =
      { crowd with Persons = crowd.Persons |> List.filter (fun p -> p.Id <> person.Id) |> List.append [ person ] }
    match msg with
    | Refresh -> model
    | PersonMessage (id,msg') ->
      match msg' with
      | Person.Nothing -> model
      | _ ->  model.Persons
              |> List.find (fun p -> p.Id = id)
              |> Person.update msg' 
              |> insertOrUpdatePerson model  
    | Delete person -> deletePerson model person
    | Add person -> insertOrUpdatePerson model person

 open Crowd

let bindings window _ _ =
  let editors = new Dictionary<Guid,PeopleEditor>()

  let callEditor person =
    if not (editors.ContainsKey person.Id) then
      let editor = new PeopleEditor ()
      editor.Owner <- window
      editor.Closed.Add (fun _ -> editors.Remove person.Id |> ignore)
      //window |> VM.ofWindow |> VM.findAbstractPath [ VM.KeyedProperty ("Persons", string person.Id) ] |> VM.bindTo editor
      // I wish this could be written like this :
      window |> VM.ofWindow |> VM.resolvePath ("Persons[" + (string person.Id) + "]") |> VM.bindTo editor
      //window |> VM.ofWindow |> VM.resolvePath ("Persons[0]") |> VM.bindTo editor
      editors.Add(person.Id, editor)
      editors.[person.Id].Show()
    else
      editors.[person.Id].Activate() |> ignore

  let confirmDelete person =
    MessageBox.Show("do you want to delete " + person.NickName + " ?", "Delete person", MessageBoxButton.YesNo)
    |> function | MessageBoxResult.Yes -> (if editors.ContainsKey person.Id then editors.[person.Id].Close()) ; Delete person
                | _ -> Refresh

  [ "Persons" |> Binding.subModelSeq (fun m -> m.Persons |> List.sortByDescending (fun m -> m.NickName))
                                     (fun sm -> sm.Id)
                                     Person.bindings 
                                     PersonMessage
    "AddPerson" |> Binding.cmd (fun _ -> Crowd.Add (Person.init "New person" "" ""))
    "DeletePerson" |> Binding.paramCmd (fun v _ -> v |> VM.currentModel<_> |> confirmDelete)
    "EditPerson" |> Binding.paramCmd (fun v m -> callEditor (VM.currentModel<_>  v) ; Refresh)    ]

[<EntryPoint;STAThread>]
let main argv =
    let mainWindow = new MainWindow()
    Program.mkSimple Crowd.init Crowd.update (bindings mainWindow)
    |> Program.withConsoleTrace
    |> Program.runWindow mainWindow
