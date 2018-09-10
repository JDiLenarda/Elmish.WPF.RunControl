[<RequireQualifiedAccess>]
module Elmish.WPF.Window

open Elmish
open System.Windows

/// bind a Window to a program and call ShowDialog. 
/// return the result of ShowDialog and the updated model
let showDialog (window:Window) program =
    let mutable lastModel = None
    let program' = { program with
                      init = (fun args ->
                        let (model,cmds) = program.init args
                        lastModel <- Some model
                        model,cmds)
                      update = (fun msg model ->
                        let (model',cmds) = program.update msg model
                        lastModel <- Some model'
                        model',cmds)
                    }
    Program.bindFrameWorkElement window program'
    window.ShowDialog().Value, lastModel.Value

// here be ugly code
// TODO: refactor

let bindToParentContext (parent:Window) (child:Window) =
  child.DataContext <- parent.DataContext

open Microsoft.CSharp.RuntimeBinder
open System.Linq.Expressions
open Elmish.WPF.Internal

let private buildPropertyGetter targetType propertyName =
    let rootParam =  System.Linq.Expressions.Expression.Parameter(typeof<obj>)
    let propBinder = Binder.GetMember(CSharpBinderFlags.None,
                                      propertyName,
                                      targetType,
                                      [ CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) ])
    let propGetExpression = Expression.Dynamic(propBinder, typeof<obj>, Expression.Convert(rootParam, targetType));
    let getPropExpression = Expression.Lambda (propGetExpression, rootParam)
    getPropExpression.Compile()
    
type PathElement =
  | Property of string
  | IndexedProperty of string * int
  | KeyedProperty of property: string * key: string

let private subContextFromPropertyName context name = 
    let getter = buildPropertyGetter (context.GetType()) name
    getter.DynamicInvoke [| context |]
    
let private getSubContext (context:obj) property =
  try
    match property with
    | Property name -> subContextFromPropertyName context name
    | IndexedProperty (name,index ) ->
      subContextFromPropertyName context name :?> seq<_>
      |> Seq.item index
    | KeyedProperty (name, key) ->
      let vm = context :?> IGetInder
      match vm.GetIndexer name with
      | Some getIndex ->
        subContextFromPropertyName context name :?> seq<_>
        |> Seq.find (fun x ->
          let y = getIndex ( (x :?> ViewModel<_,_>).CurrentModel)
          string y = key)
      | None -> null
  with | _ -> null
    

let bindToParentPartialContext (path: PathElement list) (parent:Window) (child:Window) =
  let rec loop path' context =
    match path' with
    | x :: xs -> loop xs (getSubContext context x)
    | [] -> context    

  child.DataContext <- loop path (parent.DataContext)
