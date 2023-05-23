namespace Boxes

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.UI.Primitives

[<DomainType>]
type VisibleBox = {
    geometry : Box3d
    color    : C4b    

    [<TreatAsValue; PrimaryKey>]
    id       : string
}