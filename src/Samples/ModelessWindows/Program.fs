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

  let bindings () =
    [ "Id" |> Binding.oneWay (fun m -> m.Id)
      "NickName" |> Binding.twoWay (fun m -> m.NickName) (fun v _ -> ChangeNickName v)
      "FirstName" |> Binding.twoWay (fun m -> m.FirstName) (fun v _ -> ChangeFirstName v)
      "LastName" |> Binding.twoWay (fun m -> m.LastName) (fun v _ -> ChangeLastName v) ]
  
type Crowd = { Persons: Person list }

type CrowdMsg =
  | PersonMessage of Id: Guid * Msg: Persons

let init () =
  { Persons = [ Person.init "JDiLenarda" "Julien" "Di Lenarda"
                Person.init "cmeeren" "Christer" "Van der Meeren"
                Person.init "giuliohome" "?" "?"
                Person.init "JohnStov" "John" "Stovin"
                Person.init "2sComplement" "Justin" "Sacks"   ] }

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
    updatePerson person' model  

let mainWindow = new MainWindow()

let bindings _ _ =
  let editors = new Dictionary<_,_>()

  let callEditor person =
    if not (editors.ContainsKey person.Id) then
      let editor = new PeopleEditor ()
      editor.Owner <- mainWindow
      editor.Closed.Add (fun _ -> editors.Remove person.Id |> ignore)
      editor.DataContext <- DataContext.subContextFromDescription [ DataContext.KeyedProperty ("Persons", string person.Id) ] mainWindow.DataContext
      // I wish this could be written like this : editor.DataContext <- DataContext.subContext ("Persons[" + person.Id + "]") mainWindow.DataContext
      editors.Add(person.Id, editor)
      editors.[person.Id].Show()
    else
      editors.[person.Id].Activate() |> ignore

  let bindings () =
    Person.bindings ()
    |> List.append [  "CallEditor" |> Binding.cmd (fun m -> callEditor m ; Nothing) ]
                      //"PersonContext" |> Binding.oneWay (fun m -> DataContext.subContextFromDescription [ DataContext.KeyedProperty ("Persons", string m.Id) ] mainWindow.DataContext) ]

  [ "Persons" |> Binding.subModelSeq (fun m -> m.Persons |> List.sortBy (fun m -> m.NickName))
                                     (fun sm -> sm.Id)
                                     bindings
                                     PersonMessage    ]

[<EntryPoint;STAThread>]
let main argv = 
    Program.mkSimple init update bindings
    |> Program.withConsoleTrace
    |> Program.runWindow mainWindow
