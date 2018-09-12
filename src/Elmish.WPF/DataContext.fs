[<RequireQualifiedAccess>]
module Elmish.WPF.DataContext

open Microsoft.CSharp.RuntimeBinder
open System.Linq.Expressions
open Elmish.WPF.Internal
open System.Windows
open System
open System.Windows.Controls

// Basic utilities

/// retrieve data context from a FrameWorkElement
let ofFrameWorkElement (element:FrameworkElement) = element.DataContext
/// retrieve data context from a Window
let ofWindow (window:Window) = ofFrameWorkElement window
/// retrieve data context from a UserControl
let ofUserControl (control:UserControl) = ofFrameWorkElement control
/// bind data context to FrameworkElement
let bindTo (element:FrameworkElement) datacontext = element.DataContext <- datacontext
/// check if a datacontext is Elmish compliant and not null
let isValid (datacontext:obj) =
  if datacontext = null then false else
  try
    (datacontext :?> IBoxedViewModel) |> ignore
    true
  with | _ -> false

/// retrieve the current model from the data context
let currentModel<'mdl> (dataContext:obj) = (dataContext :?> IBoxedViewModel).CurrentModel |> unbox<'mdl>

// Path finding

// cargo-culted from stackoverflow, adapted from C#
// todo: review with care
let private buildPropertyGetter targetType propertyName =
  let rootParam =  System.Linq.Expressions.Expression.Parameter(typeof<obj>)
  let propBinder = Binder.GetMember(CSharpBinderFlags.None,
                                    propertyName,
                                    targetType,
                                    [ CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) ])
  let propGetExpression = Expression.Dynamic(propBinder, typeof<obj>, Expression.Convert(rootParam, targetType));
  let getPropExpression = Expression.Lambda (propGetExpression, rootParam)
  getPropExpression.Compile()

/// construct to retrieve properties and sub-data context in a data context created through Binding module function
type PathAbstract =
  /// to retrieve a property by name. Untested
  | Property of string                  // todo: test
  /// to retrieve an element by a name and an index for data contexts created with Binding.oneWaySeq or Binding.subModelSeq
  /// for a Binding.subModelSeq, the search is first down usgin its getId function, backing up on a list index search if it fails. Untested
  | IndexedProperty of string * int     // todo: test
  /// to retrieve an element by a name and a key for data contexts created with Binding.subModelSeq (using its getId function)
  | KeyedProperty of property: string * key: string

let private subViewModelFromName context name = 
  let getter = buildPropertyGetter (context.GetType()) name
  getter.DynamicInvoke [| context |]

// todo: trace  error.
let private subViewModel (context:obj) property =
  try
    let subSeq name = subViewModelFromName context name :?> seq<ViewModel<_,_>>
    let currentModel (ctx:obj) = (ctx :?> IBoxedViewModel).CurrentModel
    let indexGetter = (context :?> IBoxedViewModel).GetIndexGetter

    let fromIndex name index = subSeq name |> Seq.item index |> box
    let fromKey name getIndex key = subSeq name |> Seq.find (fun subContext -> subContext |> currentModel |> getIndex |> string |> (=) key) |> box
      
    match property with
    | Property name -> subViewModelFromName context name
    | IndexedProperty (name, index) ->
      match indexGetter name with
      | None -> fromIndex name index 
      | Some getIndex -> let r = fromKey name getIndex (string index)
                         if r <> null then r else fromIndex name index 
    | KeyedProperty (name, key) ->
      match indexGetter name with
      | Some getIndex -> fromKey name getIndex key
      | None -> null
  with | _ -> null

/// retrieve a property or sub context from a dataContext, following the given path
let findAbstractPath path dataContext =
  let rec loop path' context =
    match path' with
    | x :: xs -> loop xs (subViewModel context x)
    | [] -> context    
  loop path dataContext

// todo: to write
let private stringToPathDescription (path:string) : PathAbstract list =
  raise <| NotImplementedException()

/// retrieve a property or sub context from a dataContext, following the given path
/// the path is a string working like a Xaml binding path.
let findPath path dataContext =
  findAbstractPath (stringToPathDescription path) dataContext
