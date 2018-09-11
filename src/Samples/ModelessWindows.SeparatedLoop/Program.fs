open System
open Elmish
open Elmish.WPF
open ModelessWindows.Views
open System.Collections.Generic

type Person =
  { Id: Guid
    NickName:   string
    FirstName:  string
    LastName:   string }

type PersonMsg =
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

  let bindings () =
    [ "Id" |> Binding.oneWay (fun m -> m.Id)
      "NickName" |> Binding.twoWay (fun m -> m.NickName) (fun v _ -> ChangeNickName v)
      "FirstName" |> Binding.twoWay (fun m -> m.FirstName) (fun v _ -> ChangeFirstName v)
      "LastName" |> Binding.twoWay (fun m -> m.LastName) (fun v _ -> ChangeLastName v)  ]
  
type Crowd = { Persons: Person list }

type CrowdMsg =
  | PersonMessage of Id: Guid * Msg: PersonMsg
  | Update of Person

let init () =
  { Persons = [ Person.init "JDiLenarda" "Julien" "Di Lenarda"
                Person.init "cmeeren" "Christer" "Van der Meeren"
                Person.init "giuliohome" "?" "?"
                Person.init "JohnStov" "John" "Stovin"
                Person.init "2sComplement" "Justin" "Sacks" ] }

let update msg model =
  let updatePerson crowd person =
    { crowd with Persons =
                    crowd.Persons
                    |> List.filter (fun p -> p.Id <> person.Id)
                    |> List.append [ person ] }
  match msg with  
  | Update person -> updatePerson model person
  | PersonMessage _ -> model

let mainWindow = new MainWindow()

let bindings _ dispatch =
  let editors = new Dictionary<Guid,PeopleEditorSubmit>()

  let closeEditor id = editors.[id].Close()
  let dispatchUpdate = Update >> dispatch

  let rec personBindings () =
    Person.bindings ()
    |> List.append  [ "CallEditor" |> Binding.cmd (fun m -> callEditor m ; Nothing)
                      "Submit" |> Binding.cmd (fun m -> dispatchUpdate m ; Nothing)
                      "SubmitAndClose" |> Binding.cmd (fun m -> dispatchUpdate m ; closeEditor m.Id ; Nothing)    ]
  and callEditor person =
    let init () = person
    let update = Person.update
    let bindings _ _ = personBindings ()

    if not (editors.ContainsKey person.Id) then
      let editor = new PeopleEditorSubmit ()
      editor.Owner <- mainWindow
      editor.Closed.Add (fun _ -> editors.Remove person.Id |> ignore)
      Program.mkSimple init update bindings
      |> Program.bindFrameWorkElement editor
      editors.Add(person.Id, editor)
      editors.[person.Id].Show()
    else
      editors.[person.Id].Activate() |> ignore

  [ "Persons" |> Binding.subModelSeq (fun m -> m.Persons |> List.sortBy (fun m -> m.NickName))
                                     (fun sm -> sm.Id)
                                     personBindings
                                     PersonMessage  ]

[<EntryPoint;STAThread>]
let main argv = 
    Program.mkSimple init update bindings
    |> Program.withConsoleTrace
    |> Program.runWindow mainWindow
