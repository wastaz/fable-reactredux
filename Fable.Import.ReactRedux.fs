namespace Fable.Import

open Fable.Core

module ReactRedux =
    type [<Import("Provider", "react-redux")>] Provider<'P, 'C>(props : 'P, context) =
        inherit Fable.Import.React.Component<'P, 'C>(props , context)

    type IDispatchable = interface end

    type Dispatcher = IDispatchable -> unit

    type Globals =
        member __.connect(state) : (React.Component<'TProps, 'TContext> -> React.Component<'TProps, 'TComponent>) = jsNative
        member __.connect(state, dispatch) : (React.Component<'TProps, 'TContext> -> React.Component<'TProps, 'TComponent>) = jsNative

[<AutoOpen>]
module ReactRedux_Extensions =
    let [<Import("*", "react-redux")>] ReactReduxImport : ReactRedux.Globals = failwith "JS only"