module Fable.Helpers.ReactRedux

open Fable.Core
open Fable.Import
open Fable.Core.JsInterop

[<PassGenerics>]
let private convertAndWrapWithProps<'a, 'b> (fn : ('a -> 'b -> 'b)) =
    let f = System.Func<_, _, _> (fun x y -> fn x y ) 
    f |> box |> Some


type private Inner<'TState, 'TProps> = {
    propsCreator : ('TProps -> 'TProps) option
    stateMapper : ('TState -> 'TProps -> 'TProps) option
    dispatchMapper : (ReactRedux.Dispatcher -> 'TProps -> 'TProps) option
}

let [<Import("default", "shallowequal")>] private shallowequal : (obj * obj) -> bool = jsNative

[<PassGenerics>]
let private selectorFactory<'S, 'P> (dispatch : ReactRedux.Dispatcher) (cfg : Inner<'S, 'P>) : ReactRedux.Selector<'S, 'P> =
    let mutable result : 'P = createEmpty
    let stateMapper =
        match cfg.stateMapper with
        | Some(sm) -> sm
        | None -> fun _ p -> p
    let dispatchMapper =
        match cfg.dispatchMapper with
        | Some(dm) -> dm
        | None -> fun _ p -> p
    ReactRedux.Selector<'S, 'P>(fun (nextState : 'S) (nextOwnprops : 'P) ->
        let nextResult =
            nextOwnprops
            |> dispatchMapper dispatch
            |> stateMapper nextState
        
        if shallowequal(result, nextResult) then
            result
        else 
            result <- nextResult
            nextResult)
            
[<Fable.Core.Erase>]
type ConnectorBuilder<'TState, 'TProps> =
    private
    | Connector of Inner<'TState, 'TProps>

[<PassGenerics>]
let private createReduxConnector (cb : ConnectorBuilder<'TState, 'TProps>) =
    match cb with
    | Connector(inner) ->
        ReactReduxImport.connectAdvanced(System.Func<_, _, _>(selectorFactory), inner)

let createConnector () =
    ConnectorBuilder.Connector({ propsCreator = None; stateMapper = None; dispatchMapper = None })

let withProps (defaultPropsCreator : 'P -> 'P) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(inner) -> ConnectorBuilder.Connector({ inner with propsCreator = Some defaultPropsCreator })

let withStateMapper (smp : ('S -> 'P -> 'P)) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(inner) -> ConnectorBuilder.Connector({ inner with stateMapper = Some smp })

let withDispatchMapper (dmp : (ReactRedux.Dispatcher -> 'P -> 'P)) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(inner) -> ConnectorBuilder.Connector({ inner with dispatchMapper = Some dmp })

[<PassGenerics>]
let private getMappers<'TState, 'TProps> (c : ConnectorBuilder<'TState, 'TProps>) = 
    let inner = c |> unbox<Inner<'TState, 'TProps>>
    let stateMapper = inner.stateMapper |> Option.bind convertAndWrapWithProps
    let dispatchMapper = inner.dispatchMapper |> Option.bind convertAndWrapWithProps
    stateMapper, dispatchMapper

[<PassGenerics>]
let private getProps<'TState, 'TProps> (c : ConnectorBuilder<'TState, 'TProps>) = 
    match c with
    | Connector(inner) -> inner.propsCreator
    
let [<Import("createElement", "react")>] private createElement
    (``type``: #React.ComponentClass<'P>) : React.ReactElement = jsNative

let [<Import("createElement", "react")>] private createElementWithProps
    (``type``: #React.ComponentClass<'P>) (props : 'P) : React.ReactElement = jsNative

type ElementFactory = unit -> React.ReactElement

let private toElementFactory p cc =
    match p with
    | Some(props) ->
        fun () -> createElementWithProps cc props
    | None ->
        fun () -> createElementWithProps cc createEmpty

[<PassGenerics>]
let buildComponent<'TComponent, 'TProps, 'TCtx, 'TState when 'TComponent :> React.Component<'TProps, 'TCtx>> 
    (c : ConnectorBuilder<'TState, 'TProps>) = 
        let stateMapper, dispatchMapper = getMappers<'TState, 'TProps> c
        let props = 
            getProps c
            |> Option.map (fun propsFn -> createEmpty |> unbox<'TProps> |> propsFn)
        createReduxConnector c
        |> fun cr -> cr$(U2.Case1(unbox typeof<'TComponent>)) 
        |> unbox<React.ComponentClass<'TProps>>
        |> toElementFactory props

[<PassGenerics>]
let buildFunction<'TProps, 'TState> 
    (fn : ('TProps -> React.ReactElement))
    (c : ConnectorBuilder<'TState, 'TProps>) = 
        let stateMapper, dispatchMapper = getMappers<'TState, 'TProps> c
        let props = 
            getProps c
            |> Option.map (fun propsFn -> createEmpty |> unbox<'TProps> |> propsFn)
        createReduxConnector c
        |> fun cr -> cr$(fn)
        |> unbox<React.ComponentClass<'TProps>>
        |> toElementFactory props

[<PassGenerics>]
let createProvider<'TStore when 'TStore :> Redux.IUntypedStore> (store : 'TStore)  (app : React.ReactElement) =
    Fable.Helpers.React.com<ReactRedux.Provider<_, _>, _, _> (createObj [ "store" ==> store ]) [ app ]

