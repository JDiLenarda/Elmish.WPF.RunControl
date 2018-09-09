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
