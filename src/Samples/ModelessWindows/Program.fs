open System
open Elmish
open Elmish.WPF
open ModelessWindows.Views
open System.Collections.Generic
open System.Windows

type Person =
  { Id: Guid
    NickName:   string
    FirstName:  string
    LastName:   string }
type Persons =
  | Nothing
  | ChangeNickName of string
  | ChangeFirstName of string
  | ChangeLastName of string

module Person =
  let init nick first last =  { Id = Guid.NewGuid() ; NickName = nick ; FirstName = first ; LastName = last  }
  let update msg model =
    match msg with
    | Nothing -> model
    | ChangeNickName nickName -> { model with NickName = nickName }
    | ChangeFirstName firstname -> { model with FirstName = firstname }
    | ChangeLastName lastname -> { model with LastName = lastname }
  
type Crowd = { Persons: Person list }

type CrowdMsg =
  | PersonMessage of Id: Guid * Msg: Persons

let init () =
  { Persons = [ Person.init "JDiLenarda" "Julien" "Di Lenarda"
                Person.init "cmeeren" "Christer" "Van der Meeren"
                Person.init "giorgiohome" "?" "?"
                Person.init "JohnStov" "John" "Stovin"
                Person.init "2sComplement" "Justin" "Sacks"   ] },
  Cmd.none

let update msg model =
  let updatePerson person crowd =
    { crowd with Persons =
                    crowd.Persons
                    |> List.filter (fun p -> p.Id <> person.Id)
                    |> List.append [ person ] }
  match msg with  
  | PersonMessage (id,pplMsg) ->
    let person = model.Persons |> List.find (fun p -> p.Id = id)
    let person' = Person.update pplMsg person
    updatePerson person' model, Cmd.none    

let mainWindow = new MainWindow()

let bindings _ _ =
  let editors = new Dictionary<_,_>()

  let callEditor person =
    if not (editors.ContainsKey person.Id) then
      let editor = new PeopleEditor ()
      editor.Closed.Add (fun _ -> editors.Remove person.Id |> ignore)
      // I wish this could be written like this : Window.bindToParentPartialContext ("Persons[" + person.Id + "]") mainWindow editor
      Window.bindToParentPartialContext [ Window.KeyedProperty ("Persons", string person.Id) ] mainWindow editor
      editors.Add(person.Id, editor)
      editors.[person.Id].Show()
    else
      editors.[person.Id].Activate() |> ignore

  let rec personBindings () =
    [ "Id" |> Binding.oneWay (fun m -> m.Id)
      "NickName" |> Binding.twoWay (fun m -> m.NickName) (fun v _ -> ChangeNickName v)
      "FirstName" |> Binding.twoWay (fun m -> m.FirstName) (fun v _ -> ChangeFirstName v)
      "LastName" |> Binding.twoWay (fun m -> m.LastName) (fun v _ -> ChangeLastName v)
      "CallEditor" |> Binding.cmd (fun m -> callEditor m ; Nothing)   ]

  [ "Persons" |> Binding.subModelSeq (fun m -> m.Persons |> List.sortBy (fun m -> m.NickName))
                                     (fun sm -> sm.Id)
                                     personBindings
                                     PersonMessage    ]

[<EntryPoint;STAThread>]
let main argv = 
    Program.mkProgram init update bindings
    |> Program.withConsoleTrace
    |> Program.runWindow mainWindow
