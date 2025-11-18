namespace Aardvark.Docs.Utils

open Aardvark.Base
open Aardvark.Rendering
open FShade

module ThickLine = 

    type ThickLineVertex = {
        [<Position>]                pos     : V4f
        [<Color>]                   c       : V4f
        [<Semantic("LineCoord")>]   lc      : V2f
        [<Semantic("LineWidth")>]   w       : float32
    }

    let internal thickLine (line : Line<ThickLineVertex>) =
        triangle {
            let t = uniform.LineWidth
            let sizeF = V3f(float32 uniform.ViewportSize.X, float32 uniform.ViewportSize.Y, 1.0f)

            let pp0 = line.P0.pos
            let pp1 = line.P1.pos

            let p0 = pp0.XYZ / pp0.W
            let p1 = pp1.XYZ / pp1.W

            let fwp = (p1.XYZ - p0.XYZ) * sizeF

            let fw = V3f(fwp.XY * 2.0f, 0.0f) |> Vec.normalize
            let r = V3f(-fw.Y, fw.X, 0.0f) / sizeF
            let d = fw / sizeF
            let p00 = p0 - r * t - d * t
            let p10 = p0 + r * t - d * t
            let p11 = p1 + r * t + d * t
            let p01 = p1 - r * t + d * t

            let rel = t / (Vec.length fwp)

            yield { line.P0 with pos = V4f(p00, 1.0f); lc = V2f(-1.0f, -rel); w = rel }
            yield { line.P0 with pos = V4f(p10, 1.0f); lc = V2f( 1.0f, -rel); w = rel }
            yield { line.P1 with pos = V4f(p01, 1.0f); lc = V2f(-1.0f, 1.0f + rel); w = rel }
            yield { line.P1 with pos = V4f(p11, 1.0f); lc = V2f( 1.0f, 1.0f + rel); w = rel }
        }

    let Effect = 
        toEffect thickLine