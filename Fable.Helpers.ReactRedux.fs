[<Fable.Core.Erase>]
module Fable.Helpers.ReactRedux

open Fable.Core
open Fable.Import
open Fable.Core.JsInterop

let inline private convertAndWrap<'a> (fn : ('a -> (string * obj) list)) =
    let f = System.Func<_, _> (fun x -> x |> fn |> createObj) 
    f |> box |> Some

let inline private convertAndWrapWithProps<'a, 'b> (fn : ('a -> 'b -> (string * obj) list)) =
    let f = System.Func<_, _, _> (fun x y -> 
        fn x y 
        |> createObj) 
    f |> box |> Some

let inline private createReduxConnector (statemapper : obj option) (dispatchmapper : obj option) =
    match statemapper, dispatchmapper with
    | Some(s), Some(d) -> 
        ReactReduxImport.connect(s, d)
    | None, Some(d) -> 
        ReactReduxImport.connect(None, d)
    | Some(s), None -> 
        ReactReduxImport.connect(s)
    | None, None -> 
        failwith "No mapper provided!"

type Inner<'TState, 'TProps> = 
    ('TState -> (string * obj) list) option *
    ('TState -> 'TProps -> (string * obj) list) option *
    (ReactRedux.Dispatcher -> (string * obj) list) option *
    (ReactRedux.Dispatcher -> 'TProps -> (string * obj) list) option

[<Fable.Core.Erase>]
type ConnectorBuilder<'TState, 'TProps> =
    | Connector of Inner<'TState, 'TProps>
        
let inline createConnector () =
    ConnectorBuilder.Connector(None, None, None, None)

let inline withStateMapper (sm : ('S -> (string * obj) list)) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(_, _, dm, dmp) -> ConnectorBuilder.Connector(Some sm, None, dm, dmp)

let inline withStateMapperWithProps (smp : ('S -> 'P -> (string * obj) list)) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(_, _, dm, dmp) -> ConnectorBuilder.Connector(None, Some smp, dm, dmp)

let inline withDispatchMapper (dm : (ReactRedux.Dispatcher -> (string * obj) list)) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(sm, smp, _, _) -> ConnectorBuilder.Connector(sm, smp, Some dm, None)

let inline withDispatchMapperWithProps (dmp : (ReactRedux.Dispatcher -> 'P -> (string * obj) list)) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(sm, smp, _, _) -> ConnectorBuilder.Connector(sm, smp, None, Some dmp)

let inline private appendChildren
        (props : 'TProps)
        (children : Fable.Import.React.ReactElement<obj> list)
        (cmp : React.ComponentClass<'TProps>) =
    let nodechildren = 
        children
        |> List.map (unbox<React.ReactNode>) 
        |> List.toArray
    React.createElement(cmp, props, nodechildren) 
    |> unbox<React.ReactElement<obj>>

let inline getMappers<'TState, 'TProps> (c : ConnectorBuilder<'TState, 'TProps>) = 
    let inner = c |> unbox<Inner<'TState, 'TProps>>
    let stateMapper =
        match inner with
        | None, None, _, _ -> None
        | Some(sm), None, _, _ -> convertAndWrap sm
        | _, Some(smp), _, _ -> convertAndWrapWithProps smp
    let dispatchMapper =
        match inner with
        | _, _, None, None -> None
        | _, _, Some(dm), None -> convertAndWrap dm
        | _, _, _, Some(dmp) -> convertAndWrapWithProps dmp
    stateMapper, dispatchMapper

let inline buildComponent<'TComponent, 'TProps, 'TCtx, 'TState when 'TComponent :> React.Component<'TProps, 'TCtx>> 
    (props : 'TProps) 
    (children : Fable.Import.React.ReactElement<obj> list)
    (c : ConnectorBuilder<'TState, 'TProps>) = 
        let stateMapper, dispatchMapper = getMappers<'TState, 'TProps> c
        createReduxConnector stateMapper dispatchMapper
        |> fun cr -> cr$(U2.Case1(unbox typeof<'TComponent>)) 
        |> unbox<React.ComponentClass<'TProps>>
        |> appendChildren props children

let inline buildFunction<'TProps, 'TState> 
    (fn : ('TProps -> React.ReactElement<obj>))
    (props : 'TProps) 
    (children : Fable.Import.React.ReactElement<obj> list)
    (c : ConnectorBuilder<'TState, 'TProps>) = 
        let stateMapper, dispatchMapper = getMappers<'TState, 'TProps> c
        createReduxConnector stateMapper dispatchMapper
        |> fun cr -> cr$(fn)
        |> unbox<React.ComponentClass<'TProps>>
        |> appendChildren props children


let inline createProvider<'TStore when 'TStore :> Redux.IUntypedStore> (store : 'TStore)  (app : React.ReactElement<obj>) =
    Fable.Helpers.React.com<ReactRedux.Provider<_, _>, _, _> (createObj [ "store" ==> store ]) [ app ]
