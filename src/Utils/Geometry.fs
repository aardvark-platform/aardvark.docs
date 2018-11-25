namespace Aardvark.Docs.Utils

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.Base.Rendering
open Aardvark.SceneGraph
open FShade
open System

module Geometry = 

    let private r = Random()
    
    let points n pointsize (bounds : Box2d) =
        let positions = Mod.constant [| for x in 1..n do yield bounds.Min.XYO + bounds.Size.XYO * V3d(r.NextDouble(), r.NextDouble(), 0.0) |]
        let colors = Mod.constant [| for x in 1..n do yield C4b(r.Next(256), r.Next(256), r.Next(256)) |]
        DrawCallInfo(FaceVertexCount = n, InstanceCount = 1)
            |> Sg.render IndexedGeometryMode.PointList 
            |> Sg.vertexAttribute DefaultSemantic.Positions positions
            |> Sg.vertexAttribute DefaultSemantic.Colors colors
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.pointSprite |> toEffect
               ]
            |> Sg.uniform "PointSize" (Mod.constant pointsize)

    let grid (bounds : Box2d) (color : C4b) =
        let lines =
            [
                [| for x in bounds.Min.X..0.5..bounds.Max.X do yield Line3d(V3d(x, bounds.Min.Y, 0.0), V3d(x, bounds.Max.Y, 0.0)) |]
                [| for y in bounds.Min.Y..0.5..bounds.Max.Y do yield Line3d(V3d(bounds.Min.X, y, 0.0), V3d(bounds.Max.X, y, 0.0)) |]
            ]
            |> Array.concat
        Sg.lines (Mod.constant color) (Mod.constant lines)
        |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.constantColor (C4f(color)) |> toEffect
                ThickLine.Effect
               ]
        |> Sg.uniform "LineWidth" (Mod.constant 0.5)

    let aardvark (color : C4b) (lineWidth : float) =
        let ps = [|
            V2f(0.001055966f, 0.3294612f); V2f(0.03801486f, 0.3241816f); V2f(0.08447732f, 0.3157345f); V2f(0.12566f, 0.3062304f);
            V2f(0.1520591f, 0.2977834f); V2f(0.1636748f, 0.2956711f); V2f(0.2038023f, 0.276663f); V2f(0.2344249f, 0.2587118f);
            V2f(0.2724394f, 0.225977f); V2f(0.2375923f, 0.2734956f); V2f(0.2354801f, 0.2819426f); V2f(0.2365372f, 0.2977834f);
            V2f(0.2386475f, 0.3199571f); V2f(0.2397046f, 0.3516368f); V2f(0.2882783f, 0.3505816f); V2f(0.2756068f, 0.3125652f);
            V2f(0.2734946f, 0.2935589f); V2f(0.2840557f, 0.2756078f); V2f(0.3072865f, 0.2544874f); V2f(0.3305173f, 0.2375933f);
            V2f(0.3379091f, 0.2354811f); V2f(0.3590277f, 0.2280892f); V2f(0.3843725f, 0.2407607f); V2f(0.4044359f, 0.2565996f);
            V2f(0.4149952f, 0.2745507f); V2f(0.42133f, 0.2988386f); V2f(0.4308341f, 0.3146774f); V2f(0.4445626f, 0.328406f);
            V2f(0.44773f, 0.3400204f); V2f(0.4508974f, 0.3495245f); V2f(0.4530097f, 0.3516368f); V2f(0.5205916f, 0.3505816f);
            V2f(0.5079201f, 0.3389653f); V2f(0.4910241f, 0.3294612f); V2f(0.4772975f, 0.3189019f); V2f(0.4677934f, 0.303063f);
            V2f(0.4593445f, 0.28511f); V2f(0.4540648f, 0.2692711f); V2f(0.4540648f, 0.2629363f); V2f(0.4815201f, 0.2354811f);
            V2f(0.4952486f, 0.2249199f); V2f(0.5047527f, 0.2185851f); V2f(0.5110875f, 0.2122484f); V2f(0.5205916f, 0.2122484f);
            V2f(0.540655f, 0.2185851f); V2f(0.5649428f, 0.2228096f); V2f(0.592398f, 0.2280892f); V2f(0.5765572f, 0.2534322f);
            V2f(0.5575509f, 0.2840548f); V2f(0.5427672f, 0.3020059f); V2f(0.5322079f, 0.3104549f); V2f(0.5364305f, 0.3125652f);
            V2f(0.5395979f, 0.3167897f); V2f(0.5395979f, 0.3368531f); V2f(0.5417101f, 0.3442449f); V2f(0.540655f, 0.3505816f);
            V2f(0.5892287f, 0.3505816f); V2f(0.5902858f, 0.3431897f); V2f(0.5828939f, 0.328406f); V2f(0.5786694f, 0.3157345f);
            V2f(0.5818369f, 0.3062304f); V2f(0.591341f, 0.2956711f); V2f(0.6019002f, 0.2872222f); V2f(0.6187962f, 0.27772f);
            V2f(0.6494188f, 0.2639915f); V2f(0.7032741f, 0.2375933f); V2f(0.7138333f, 0.2386485f); V2f(0.7328415f, 0.2470955f);
            V2f(0.7486804f, 0.2671589f); V2f(0.7571275f, 0.2829996f); V2f(0.7729682f, 0.2988386f); V2f(0.7793031f, 0.3072856f);
            V2f(0.7866949f, 0.3178467f); V2f(0.7951419f, 0.3357978f); V2f(0.7993664f, 0.3484693f); V2f(0.8004216f, 0.3505816f);
            V2f(0.8616687f, 0.3495245f); V2f(0.8310461f, 0.3189019f); V2f(0.8152053f, 0.3020059f); V2f(0.8141502f, 0.276663f);
            V2f(0.8099257f, 0.2555444f); V2f(0.8088705f, 0.2418159f); V2f(0.8247094f, 0.2428729f); V2f(0.8352687f, 0.2460403f);
            V2f(0.8701158f, 0.2650467f); V2f(0.8965158f, 0.2808874f); V2f(0.9123547f, 0.2925019f); V2f(0.9197466f, 0.2946141f);
            V2f(0.9303058f, 0.3009508f); V2f(0.9440344f, 0.3072856f); V2f(0.9662099f, 0.3273489f); V2f(0.9672651f, 0.328406f);
            V2f(0.9757122f, 0.3305182f); V2f(0.9809918f, 0.3326286f); V2f(1.0f, 0.3051734f); V2f(0.9978878f, 0.2988386f);
            V2f(0.9873285f, 0.2914467f); V2f(0.9662099f, 0.2745507f); V2f(0.9461466f, 0.2565996f); V2f(0.9345303f, 0.2196403f);
            V2f(0.9112995f, 0.1900747f); V2f(0.8922914f, 0.1700106f); V2f(0.881732f, 0.1626187f); V2f(0.872228f, 0.1605069f);
            V2f(0.8785647f, 0.1488913f); V2f(0.8775076f, 0.1372756f); V2f(0.8785647f, 0.1055966f); V2f(0.8933484f, 0.06124602f);
            V2f(0.8954588f, 0.04646248f); V2f(0.8954588f, 0.02745505f); V2f(0.8880669f, 0.005279833f); V2f(0.881732f, 0.004223869f);
            V2f(0.8658932f, 0.04223859f); V2f(0.8585013f, 0.0696938f); V2f(0.856389f, 0.09186902f); V2f(0.8542768f, 0.09186902f);
            V2f(0.8489972f, 0.06019009f); V2f(0.8437176f, 0.04012673f); V2f(0.8225971f, 0.01900739f); V2f(0.8194298f, 0.02956709f);
            V2f(0.8057031f, 0.07074973f); V2f(0.804646f, 0.122492f); V2f(0.7697989f, 0.09609292f); V2f(0.7265048f, 0.06546991f);
            V2f(0.676874f, 0.03695876f); V2f(0.6367473f, 0.01900739f); V2f(0.5955654f, 0.008447726f); V2f(0.5469898f, 0.0f);
            V2f(0.4783527f, 0.005279833f); V2f(0.4181626f, 0.01795142f); V2f(0.3653643f, 0.03801488f); V2f(0.336854f, 0.05385434f);
            V2f(0.3093987f, 0.07602955f); V2f(0.2756068f, 0.1119324f); V2f(0.2555435f, 0.1425554f); V2f(0.2312575f, 0.1826821f);
            V2f(0.2196412f, 0.1985217f); V2f(0.1858501f, 0.2312566f); V2f(0.1573389f, 0.2534322f); V2f(0.1182683f, 0.2756078f);
            V2f(0.06863786f, 0.2998937f); V2f(0.03062299f, 0.3167897f); V2f(0.0f, 0.328406f)
            |]
        let lines =
            [
                [| for struct (a, b) in ps.PairChainWrap() do yield Line3d(V3d(float a.X, float a.Y, 0.0), V3d(float b.X, float b.Y, 0.0)) |]
            ]
            |> Array.concat
        Sg.lines (Mod.constant color) (Mod.constant lines)
        |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.constantColor (C4f(color)) |> toEffect
                ThickLine.Effect
               ]
        |> Sg.uniform "LineWidth" (Mod.constant lineWidth)