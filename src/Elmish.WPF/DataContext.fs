[<RequireQualifiedAccess>]
module Elmish.WPF.DataContext

open Microsoft.CSharp.RuntimeBinder
open System.Linq.Expressions
open Elmish.WPF.Internal
open System.Windows
open System

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
type PathDescription =
  /// to retrieve a property by name
  | Property of string                  // todo: test
  /// to retrieve an element by index in a data context created with Binding.oneWaySeq or Binding.subModelSeq
  | IndexedProperty of string * int     // todo: test
  /// to retrieve an element by key in a data context created with Binding.subModelSeq, using its getId function
  | KeyedProperty of property: string * key: string

let private subViewModelFromName context name = 
  let getter = buildPropertyGetter (context.GetType()) name
  getter.DynamicInvoke [| context |]

// todo: trace when error.
let private subViewModel (context:obj) property =
  let toSeq (x:obj) = x :?> seq<_>
  try
    match property with
    | Property name -> subViewModelFromName context name
    | IndexedProperty (name,index ) -> subViewModelFromName context name |> toSeq |> Seq.item index
    | KeyedProperty (name, key) ->
      let vm = context :?> IGetIndex
      match vm.GetIndexGetter name with
      | Some getIndex ->
        subViewModelFromName context name
        |> toSeq
        |> Seq.find (fun subContext -> (subContext :?> ViewModel<_,_>).CurrentModel |> getIndex |> string |> (=) key)
      | None -> null
  with | _ -> null

let subContextFromDescription (path: PathDescription list) dataContext =
  let rec loop path' context =
    match path' with
    | x :: xs -> loop xs (subViewModel context x)
    | [] -> context    
  loop path dataContext

// todo: to write
let private stringToPathDescription (path:string) : PathDescription list =
  raise <| NotImplementedException()

let subContext path dataContext =
  subContextFromDescription (stringToPathDescription path) dataContext
