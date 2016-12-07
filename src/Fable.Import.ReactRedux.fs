namespace Fable.Import

open Fable.Core

module ReactRedux =
    type [<Import("Provider", "react-redux")>] Provider<'P, 'C>(props : 'P, context) =
        inherit Fable.Import.React.Component<'P, 'C>(props)

    type IDispatchable = interface end

    type Dispatcher = IDispatchable -> unit

    type Globals =
        abstract connect : state : 'a * ?dispatch : 'b -> (React.Component<'TProps, 'TContext> -> React.Component<'TProps, 'TComponent>)

[<AutoOpen>]
module ReactRedux_Extensions =
    let [<Import("*", "react-redux")>] ReactReduxImport : ReactRedux.Globals = jsNative