module Fable.Helpers.ReactRedux

open Fable.Core
open Fable.Import
open Fable.Core.JsInterop

[<PassGenerics>]
let private convertAndWrap<'a> (fn : ('a -> (string * obj) list)) =
    let f = System.Func<_, _> (fun x -> x |> fn |> createObj) 
    f |> box |> Some

[<PassGenerics>]
let private convertAndWrapWithProps<'a, 'b> (fn : ('a -> 'b -> (string * obj) list)) =
    let f = System.Func<_, _, _> (fun x y -> 
        fn x y 
        |> createObj) 
    f |> box |> Some

let private createReduxConnector (statemapper : obj option) (dispatchmapper : obj option) =
    match statemapper, dispatchmapper with
    | Some(s), Some(d) -> 
        ReactReduxImport.connect(s, d)
    | None, Some(d) -> 
        ReactReduxImport.connect(None, d)
    | Some(s), None -> 
        ReactReduxImport.connect(s)
    | None, None -> 
        failwith "No mapper provided!"

type private Inner<'TState, 'TProps> = 
    ('TState -> (string * obj) list) option *
    ('TState -> 'TProps -> (string * obj) list) option *
    (ReactRedux.Dispatcher -> (string * obj) list) option *
    (ReactRedux.Dispatcher -> 'TProps -> (string * obj) list) option

[<Fable.Core.Erase>]
type ConnectorBuilder<'TState, 'TProps> =
    | Connector of Inner<'TState, 'TProps>
        
let createConnector () =
    ConnectorBuilder.Connector(None, None, None, None)

let withStateMapper (sm : ('S -> (string * obj) list)) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(_, _, dm, dmp) -> ConnectorBuilder.Connector(Some sm, None, dm, dmp)

let withStateMapperWithProps (smp : ('S -> 'P -> (string * obj) list)) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(_, _, dm, dmp) -> ConnectorBuilder.Connector(None, Some smp, dm, dmp)

let withDispatchMapper (dm : (ReactRedux.Dispatcher -> (string * obj) list)) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(sm, smp, _, _) -> ConnectorBuilder.Connector(sm, smp, Some dm, None)

let withDispatchMapperWithProps (dmp : (ReactRedux.Dispatcher -> 'P -> (string * obj) list)) (c : ConnectorBuilder<'S, 'P>) =
    match c with
    | Connector(sm, smp, _, _) -> ConnectorBuilder.Connector(sm, smp, None, Some dmp)

let private appendChildren
        (props : 'TProps)
        (children : Fable.Import.React.ReactElement list)
        (cmp : React.ComponentClass<'TProps>) =
    React.from cmp props children

[<PassGenerics>]
let private getMappers<'TState, 'TProps> (c : ConnectorBuilder<'TState, 'TProps>) = 
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

[<PassGenerics>]
let buildComponent<'TComponent, 'TProps, 'TCtx, 'TState when 'TComponent :> React.Component<'TProps, 'TCtx>> 
    (props : 'TProps) 
    (children : Fable.Import.React.ReactElement list)
    (c : ConnectorBuilder<'TState, 'TProps>) = 
        let stateMapper, dispatchMapper = getMappers<'TState, 'TProps> c
        createReduxConnector stateMapper dispatchMapper
        |> fun cr -> cr$(U2.Case1(unbox typeof<'TComponent>)) 
        |> unbox<React.ComponentClass<'TProps>>
        |> appendChildren props children

[<PassGenerics>]
let buildFunction<'TProps, 'TState> 
    (fn : ('TProps -> React.ReactElement))
    (props : 'TProps) 
    (children : Fable.Import.React.ReactElement list)
    (c : ConnectorBuilder<'TState, 'TProps>) = 
        let stateMapper, dispatchMapper = getMappers<'TState, 'TProps> c
        createReduxConnector stateMapper dispatchMapper
        |> fun cr -> cr$(fn)
        |> unbox<React.ComponentClass<'TProps>>
        |> appendChildren props children

[<PassGenerics>]
let createProvider<'TStore when 'TStore :> Redux.IUntypedStore> (store : 'TStore)  (app : React.ReactElement) =
    Fable.Helpers.React.com<ReactRedux.Provider<_, _>, _, _> (createObj [ "store" ==> store ]) [ app ]
