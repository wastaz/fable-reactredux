# fable-reactredux

Quite opinionated fable bindings and helpers for [react-redux](https://github.com/reactjs/react-redux).

This package is intended to be used together with my redux bindings 
[fable-redux](https://github.com/wastaz/fable-redux) and maybe also the bindings
for redux-thunk [fable-reduxthunk](https://github.com/wastaz/fable-reduxthunk). 

These bindings are quite opinionated because the standard interface that the react-redux package
uses is very javascripty, taking advantage of things that is not so easy to properly map into
F#. These bindings aim to give you a slightly more F#-like way to use this library - thus it's 
a bit...opinionated.

## Installation

    $ npm install --save react-redux redux fable-core
    $ npm install --save-dev fable-reactredux fable-redux

## Usage

In a F# project (.fsproj)

    <ItemGroup>
        <Reference Include="node_modules/fable-core/Fable.Core.dll" />
        <Reference Include="node_modules/fable-redux/Fable.Redux.dll" />
        <Reference Include="node_modules/fable-reactredux/Fable.ReactRedux.dll" />
    </ItemGroup>

## Related projects

Also have a look at my bindings for [redux-thunk](https://github.com/gaearon/redux-thunk) which also contains some 
nice optional integration with these bindings as well as the excellent react bindings [fable-react](https://www.npmjs.com/package/fable-react).

## License 

MIT, feel free to fork and/or send pull requests :)