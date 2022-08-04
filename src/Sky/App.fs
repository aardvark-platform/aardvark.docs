namespace Sky

open System
open Aardvark.Base
open Aardvark.SceneGraph
open Aardvark.UI
open Aardvark.UI.Primitives
open Aardvark.Rendering
open Aardvark.Rendering.Text
open FSharp.Data.Adaptive
open Shaders
open Aardvark.Physics.Sky
open Sky.Model

[<AutoOpen>]
module GuiExtensions = 

    let timePicker (time: aval<DateTime>) (setTime: DateTime -> 'msg) : DomNode<'msg> = 
        Incremental.input <| AttributeMap.ofListCond [
            always <| attribute "type" "time"
            ("value", (time |> AVal.map (fun t -> Some (AttributeValue.String (t.ToString("HH:mm"))))))
            always <| onChange (fun str -> setTime (DateTime.Parse str))
        ]

    let datePicker (time: aval<DateTime>) (setDate: DateTime -> 'msg) : DomNode<'msg> = 
        Incremental.input <| AttributeMap.ofListCond [
            always <| attribute "type" "date"
            ("value", (time |> AVal.map (fun t -> Some (AttributeValue.String (t.ToString("yyyy-MM-dd"))))))
            always <| onChange (fun str -> setDate (DateTime.Parse str))
        ]

    let dateTimePicker (time: aval<DateTime>) (setDateTime: DateTime -> 'msg) : DomNode<'msg> = 
        Incremental.input <| AttributeMap.ofListCond [
            always <| attribute "type" "datetime-local"
            ("value", (time |> AVal.map (fun t -> Some (AttributeValue.String (t.ToString("yyyy-MM-ddTHH:mm"))))))
            always <| onChange (fun str -> setDateTime (DateTime.Parse str))
        ]

    let tableStriped (rows : list<DomNode<'a>>) : DomNode<'a> = 
        table [clazz "ui inverted striped small unstackable table"] [ tbody [] rows ]

    let accordionStrechted text' iconName active content' =
        let title = if active then "title active inverted" else "title inverted"
        let content = if active then "content active" else "content"
                               
        onBoot "$('#__ID__').accordion();" (
            div [clazz "ui inverted segment"; style "display:block"] [
                div [clazz "ui inverted accordion"] [
                    div [clazz title; style "background-color: rgb(40,40,40); min-width: 250px"] [
                            i [clazz "dropdown icon"] []
                            text text'                                
                            div[style "float:right"][
                                i [clazz (sprintf "%s icon" iconName)] []
                            ]
                           
                    ]
                    div [clazz content;  style "overflow-y: visible"] content'
                ]
            ]
        )

type GeoAction = 
    | SetLat of float
    | SetLong of float
    | SetTimeZone of float
    | SetTime of DateTime
    | SetDate of DateTime
    | SetDateTime of DateTime
    | SetNow

module GeoApp = 
    
    let update (msg: GeoAction) (m: GeoInfo) : GeoInfo = 
        match msg with 
        | SetLat lat -> { m with gpsLat = lat }
        | SetLong long -> { m with gpsLong = long }
        | SetTimeZone zone -> { m with timeZone = int zone }
        | SetTime hhmm -> { m with time = DateTime(m.time.Year, m.time.Month, m.time.Day, hhmm.Hour, hhmm.Minute, 0) }
        | SetDate yyyymmdd -> { m with time = DateTime(yyyymmdd.Year, yyyymmdd.Month, yyyymmdd.Day, m.time.Hour, m.time.Minute, 0) }
        | SetDateTime time -> { m with time = time }
        | SetNow -> { m with time = DateTime.Now }

    let viewDetail' (m: AdaptiveGeoInfo) : list<DomNode<GeoAction>> =
        [
            Html.row "Latitude" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.gpsLat; update SetLat; step 0.1; largeStep 1.0; min -90.0; max 90.0; }]
            Html.row "Longitude" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.gpsLong; update SetLong; step 0.1; largeStep 1.0; min -180.0; max 180.0; }]
            Html.row "TimeZone" [ simplenumeric { attributes [clazz "ui inverted input"]; value (m.timeZone |> AVal.map float); update SetTimeZone; step 0.1; largeStep 1.0; min -12.0; max 12.0; }]
            Html.row "Time" [ timePicker m.time SetTime ]
            Html.row "Date" [ datePicker m.time SetDate ]
            Html.row "DateTime" [dateTimePicker m.time SetDateTime]
            Html.row "" [ button [clazz "ui button"; onClick (fun _ -> SetNow)] [text "Now"] ]
            Html.row "SunPos" [ Incremental.text (m.SunPosition ||> AVal.map2 (fun coord d -> sprintf "azimuth: %.2f° zenith: %.2f° dist: %.2f million km" (coord.Phi.DegreesFromRadians()) (coord.Theta.DegreesFromRadians()) (d / 1000000000.0))) ]
            Html.row "SunDir" [ Incremental.text (m.SunDirection |> AVal.map (sprintf "%A")) ]
        ]

    let viewDetail (m: AdaptiveGeoInfo) : DomNode<GeoAction> =
        accordionStrechted "Time & Location" "Content" true [ 
            tableStriped <| (viewDetail' m)
        ]
        
type Message =
    | CameraMessage of FreeFlyController.Message
    | SetTurbidity of float
    | SetLightPollution of float
    | SetPlanetScale of float
    | SetResolution of int
    | SetExposure of float
    | SetKey of float
    | SetExposureMode of Option<ExposureMode>
    | AdjustFoV of V2d
    | SetObjectNames of bool
    | ToggleObjectNames 
    | SetObjectNameThreshold of float
    | SetStarSigns of bool
    | ToggleStarSigns
    | SetSkyType of Option<SkyType>
    | SetCIEType of Option<CIESkyType>
    | SetMagBoost of float
    | GeoMessage of GeoAction
    | Nop
    
module App =
    open Microsoft.FSharp.Reflection

    [<AutoOpen>]
    module SgExtensions = 
    
        open Aardvark.Base.Ag
        open Aardvark.SceneGraph
        open Aardvark.SceneGraph.Semantics
        
        type ViewSpaceTrafoApplicator(child : IAdaptiveValue<ISg>) =
            inherit Sg.AbstractApplicator(child)

        [<Rule>]
        type ShapeSem() =
    
            member x.ModelTrafoStack(b : ViewSpaceTrafoApplicator, scope : Ag.Scope) =
                let trafo =
                    scope.ViewTrafo
                        |> AVal.map (fun view ->
                            let camPos = view.Backward.TransformPos V3d.Zero
                            Trafo3d.Translation(camPos)
                        )

                b.Child?ModelTrafoStack <- trafo::scope.ModelTrafoStack

    module RenderPass = 
        let skyPass2 = RenderPass.before "skyPass2" RenderPassOrder.Arbitrary RenderPass.main
        let skyPass1 = RenderPass.before "skyPass1" RenderPassOrder.Arbitrary skyPass2

    let update (m : Model) (msg : Message) =
        match msg with
        | CameraMessage msg -> { m with cameraState = FreeFlyController.update m.cameraState msg }
        | SetTurbidity v -> { m with skyParams = { m.skyParams with turbidity = v }}
        | SetLightPollution v -> { m with skyParams = { m.skyParams with lightPollution = v }}
        | SetResolution v -> { m with skyParams = { m.skyParams with res = v }}
        | SetSkyType o ->  match o with | Some v -> { m with skyParams = { m.skyParams with skyType = v }} | None -> m
        | SetCIEType o -> match o with | Some v -> { m with skyParams = { m.skyParams with cieType = v }} | None -> m
        | SetPlanetScale v -> { m with planetScale = v }
        | SetExposure v -> { m with exposure = v }
        | SetKey v -> { m with key = v }
        | SetExposureMode o -> match o with | Some v -> { m with exposureMode = v } | None -> m
        | SetMagBoost v -> { m with starParams = { m.starParams with magBoost = v }}
        | SetStarSigns v -> { m with starParams = { m.starParams with starSigns = v }}
        | ToggleStarSigns -> { m with starParams = { m.starParams with starSigns = not m.starParams.starSigns }}
        | SetObjectNames v -> { m with starParams = { m.starParams with objectNames = v }}
        | ToggleObjectNames -> { m with starParams = { m.starParams with objectNames = not m.starParams.objectNames }}
        | SetObjectNameThreshold v -> { m with starParams = { m.starParams with objectNameThreshold = v }}
        | AdjustFoV v -> 
            let newFov = clamp 0.1 170.0 (m.fov * (pow 1.05 -v.Y))
            let sens = newFov / 70.0 / 100.0
            { m with fov = newFov; cameraState = { m.cameraState with freeFlyConfig = { m.cameraState.freeFlyConfig with lookAtMouseSensitivity = sens } } }
        | GeoMessage msg -> { m with geoInfo = m.geoInfo |> GeoApp.update msg }
        | Nop -> m

    let resourcePath = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "data", "sky")  // Moon-Texture And Star-Map

    module Planets = 
        // visible planets Mercury, Venus, Mars, Jupiter and Saturn
        // [Planet, Color & Albedo, Mean Radius in km]
        // color is an average color of pictures in sRGB with geometric albedo in A
        let sg (geoInfo : AdaptiveGeoInfo) (planetScale: aval<float>) (magBoost: aval<float>) (cameraFov: aval<V2d>) (spaceVisible: aval<bool>) : ISg<_> = 

            let planets = 
                [| 
                    (Planet.Mercury, C4d(0.58, 0.57, 0.57, 0.142), 2439.7) 
                    (Planet.Venus, C4d(0.59, 0.39, 0.1, 0.689), 6051.8)
                    (Planet.Mars, C4d(0.50, 0.43, 0.32, 0.17), 3389.5)
                    (Planet.Jupiter, C4d(0.54, 0.50, 0.39, 0.538), 69911.0)
                    (Planet.Saturn, C4d(0.43, 0.43, 0.38, 0.499), 58232.0) 
                |]
    
            planets 
            |> Array.map (fun (p, c, r) -> 
                
                let dirAndDistance = 
                    geoInfo.Current 
                    |> AVal.map (fun info -> Astronomy.PlanetDirectionAndDistance(p, info.time, info.timeZone, info.gpsLong, info.gpsLat))

                let dir = dirAndDistance |> AVal.map (fun (struct (phi, theta, _)) -> Sky.V3dFromPhiTheta(phi, theta))

                let size = 
                    (planetScale, dirAndDistance) ||> AVal.map2 (fun ps (struct (_, _, distance)) -> 
                        let distKm = Astronomy.AU / 1000.0 * distance // convert distance from AU to km
                        let radius = atan (r / distKm)
                        radius * (pow ps 2.5) // diameter in radians * user factor
                    ) 

                let size = (cameraFov, size) ||> AVal.map2 (fun fovRad rRad -> rRad / fovRad.X) 
    
                let lum = geoInfo.JulianDayUTC |> AVal.map (fun jd -> 
                    let rc = Astronomy.RectangularHeliocentricEclipticCoordinates(p, jd)
                    let distKm = rc.Length * Astronomy.AU / 1000.0 // convert to km
                    let sunRadiusKm = 695700.0 // km
                    let sunLuminance = 1.6e9
                    let a = atan (sunRadiusKm / distKm)
                    let sr = Constant.PiTimesTwo * (1.0 - cos a)
                    let i = sunLuminance * sr / Constant.PiTimesTwo
    
                    let colorLum = c.ToV3d().Dot(lumVector)
                    let colorNorm = c / colorLum
                    let albedo = colorNorm.RGB * c.A
                    let lum = i * albedo // lum if planet is 1px -> the actual cover depending on viewport resolution and fov
                    lum)
    
                let sg = 
                    DrawCallInfo(1) 
                    |> Sg.render IndexedGeometryMode.PointList
                    |> Sg.effect [ Shaders.planetEffect ]
                    |> Sg.cullMode' CullMode.None
                    |> Sg.uniform "MagBoost" magBoost
                    |> Sg.uniform "PlanetColor" lum// (AVal.constant(c * 1000.0)) // TODO calculate brightness depending on sun position and color and albedo
                    |> Sg.uniform "PlanetDir" dir
                    |> Sg.uniform "PlanetSize" size
                    |> Sg.uniform "SunDirection" dir // TODO calculate direction of sun relative to this planet
                    |> Sg.uniform "CameraFov" cameraFov
                    |> Sg.uniform "RealSunDirection" geoInfo.SunDirection 
                    |> Sg.writeBuffers' (Set.ofList [WriteBuffer.Color DefaultSemantic.Colors])
                    |> Sg.blendMode' { BlendMode.Add with SourceAlphaFactor = BlendFactor.Zero }
                    |> Sg.pass RenderPass.skyPass2
                    |> Sg.onOff spaceVisible

                sg)

            |> Sg.ofSeq

    module Stars = 
        
        let sg (geoInfo: AdaptiveGeoInfo) (starParams: AdaptiveStarParams) (cameraFov: aval<V2d>) (spaceVisible: aval<bool>) : ISg<_> * ISg<_> = 

            Log.startTimed "reading star database"
            let stars = Hip.readHip311Database (Path.combine [ resourcePath; "hip2.dat" ])
            Log.stop()
    
            let starDict = stars |> Seq.map (fun x -> (x.HIP, x)) |> Dictionary.ofSeq
            
            //let namedStars = Hip.NamedStars |> Array.map (fun (_, i) -> starDict.[i])
            //let ursaMajorStars = ursaMajorStars |> Array.map (fun (_, i) -> starDict.[i])
            
            // stars to show
            //let stars = ursaMajorStars
            let dirs = stars |> Array.map (fun s -> V3f(Conversion.CartesianFromSpherical(s.RArad, s.DErad)))
    
            let intScalePerMag = float32 (Fun.Pow(100.0, 1.0/5.0)) // intensity scale per magnitude ~2.511
            let i0 = 0.0325f // luminance of star with mag 0.0 / ~luminance of vega

            let colors = 
                stars 
                |> Array.map (fun s -> 
                    let l = i0 / (pow intScalePerMag s.Hpmag) // mag -26.7 -> 1.6e9 (per solar disc size), mag -12.7 average full moon -> 2.5e3
                    // luminance the star would have if sun was 1px -> scale with viewportSize and fov (currently in shader)
                    C4f(l, l, l, 1.0f)
                )
            
            let starTrafo = adaptive {
                let! jd = geoInfo.JulianDayUTC
                let t1 = Astronomy.ICRFtoCEP(jd)
                let t2 = Astronomy.CEPtoITRF(jd, 0.0, 0.0)
                let! gpsLong = geoInfo.gpsLong
                let! gpsLat = geoInfo.gpsLat
                let t3 = Astronomy.ITRFtoLocal(gpsLong, gpsLat)
                let m33d = t3 * t2 * t1
                let m44d = M44d.FromRows(m33d.R0.XYZO, m33d.R1.XYZO, m33d.R2.XYZO, V4d.OOOI)
                return Trafo3d(m44d, m44d.Inverse)
            }
                
            //Astronomy.StarDirectionJ2000(s.RArad, s.Drad, time, tz, long, lat)) stars)
            let starSg = 
                DrawCallInfo(stars.Length) 
                |> Sg.render IndexedGeometryMode.PointList
                |> Sg.vertexAttribute DefaultSemantic.Positions (AVal.constant dirs)
                |> Sg.vertexAttribute DefaultSemantic.Colors (AVal.constant colors)
                |> Sg.effect [ Shaders.starEffect ]
                |> Sg.uniform "CameraFov" cameraFov
                |> Sg.uniform "MagBoost" starParams.magBoost
                |> Sg.depthTest' DepthTest.None
                |> Sg.blendMode' { BlendMode.Add with SourceAlphaFactor = BlendFactor.Zero }
                |> Sg.pass RenderPass.skyPass2
                |> Sg.trafo starTrafo
                |> Sg.onOff spaceVisible
    
            let starSignLines = 
                Constellations.all 
                |> Array.collect id 
                |> Array.collect (fun (i0, i1) -> 
                    let s0 = starDict.[i0]
                    let s1 = starDict.[i1]
            
                    let v0 = V3f(Conversion.CartesianFromSpherical(s0.RArad, s0.DErad))
                    let v1 = V3f(Conversion.CartesianFromSpherical(s1.RArad, s1.DErad))
    
                    [| v0; v1 |]
                )
    
            let starSignSg = 
                DrawCallInfo(starSignLines.Length) 
                |> Sg.render IndexedGeometryMode.LineList
                |> Sg.vertexAttribute DefaultSemantic.Positions (AVal.constant starSignLines)
                |> Sg.effect [ Shaders.starSignEffect ]
                //|> Sg.uniform "LineWidth" (AVal.constant(0.5))
                |> Sg.uniform' "Color" (C4b(115, 194, 251, 96).ToC4f())
                |> Sg.blendMode' BlendMode.Blend
                |> Sg.trafo starTrafo
                |> Sg.onOff starParams.starSigns
    
            let objNames = 
                starParams.objectNameThreshold 
                |> ASet.bind (fun t -> (Hip.NamedStars |> Array.choose (fun (str, hip) -> 
                    let s = starDict.[hip]
                    if s.Hpmag < float32 t then 
                        // distance = size
                        // TODO/ISSUE: angle offset would get rotated by star trafo
                        let t = Trafo3d.Scale(0.15) * Trafo3d.Translation(10.0 * Conversion.CartesianFromSpherical(s.RArad, s.DErad))
                        Some (AVal.constant(t), AVal.constant(str))
                    else 
                        None) |> ASet.ofArray))
    
            let cfg = { font = FontSquirrel.Arimo.Regular; color = C4b(11, 102, 35, 128); align = TextAlignment.Center; flipViewDependent = false; renderStyle = RenderStyle.Billboard }
            
            let objNameSg = 
                ViewSpaceTrafoApplicator(AVal.constant (Sg.textsWithConfig cfg objNames |> Aardvark.SceneGraph.SgFSharp.Sg.trafo starTrafo))
                |> Sg.noEvents
                |> Sg.blendMode' BlendMode.Blend
                |> Sg.onOff starParams.objectNames

            let overlay = [ starSignSg; objNameSg ] |> Sg.ofSeq

            starSg, overlay

    module Markers = 
        
        let sg() : ISg<_> = 
            // closing circle / line strip
            let circle = 
                Array.init 13 (fun i -> 
                    let a = (float)i / 12.0 * Constant.PiTimesTwo
                    V3f(cos a, sin a, 0.0)
                )
    
            // north, east, south, west markings / line list
            let marks = 
                Array.init 8 (fun i ->
                    let a = (float)(i/2) / 4.0 * Constant.PiTimesTwo
                    let u = ((float)(i%2) - 0.5) * 2.0 * Constant.RadiansPerDegree
                    V3f(cos a, sin a, u)
                )
    
            let equatorSg = 
                DrawCallInfo(13) 
                |> Sg.render IndexedGeometryMode.LineStrip
                |> Sg.vertexAttribute DefaultSemantic.Positions (AVal.constant circle)
                |> Sg.effect [ Shaders.markerEffect] 
                |> Sg.uniform' "LineWidth" 2.0
                |> Sg.uniform' "Color" C4f.Red
    
            let marksSg = 
                DrawCallInfo(8) 
                |> Sg.render IndexedGeometryMode.LineList
                |> Sg.vertexAttribute DefaultSemantic.Positions (AVal.constant marks)
                |> Sg.effect [ Shaders.markerEffect] 
                |> Sg.uniform' "LineWidth" 2.0
                |> Sg.uniform' "Color" C4f.Red
    
            let cfg = { font = FontSquirrel.Arimo.Regular; color = C4b.Red; align = TextAlignment.Center; flipViewDependent = false; renderStyle = RenderStyle.Billboard }
    
            let markLabelStr = [| "N"; "O"; "S"; "W" |]

            let markLabels = 
                markLabelStr 
                |> Array.mapi (fun i str ->      
                    let label = Aardvark.Rendering.Text.Sg.textWithConfig cfg (AVal.init str)
                    let ang = float i / 4.0 * Constant.PiTimesTwo
                    let dir = V3d(sin ang, cos ang, -0.06) // clockwise, starting with [0, 1, 0] / North
                    ViewSpaceTrafoApplicator(AVal.constant label)
                        |> Sg.noEvents
                        |> Sg.trafo (AVal.constant (Trafo3d.Translation(dir * 20.0)))
                )
                                
            [
                equatorSg
                marksSg
                Sg.ofArray(markLabels)
            ] |> Sg.ofSeq

    module Moon = 
        
        let sg (geoInfo: AdaptiveGeoInfo) (turbidity: aval<float>) (cameraFov: aval<V2d>) (spaceVisible: aval<bool>) : ISg<_> = 

            let moonPos, moonDistance = geoInfo.MoonPosition

            let moonDiameter = 
                moonDistance |> AVal.map (fun d -> Astronomy.AngularDiameter(Astronomy.MoonDiameter, d))

            let moonDir = 
                moonPos |> AVal.map (fun sc -> Sky.V3dFromPhiTheta(sc.Phi, sc.Theta))

            let moonColor = 
                (moonPos, turbidity) ||> AVal.map2 (fun mp tu ->
                    //let (p,t) = x.moonPT
                    let moonColorXYZ = SunLightScattering(mp.Phi, mp.Theta, tu).GetRadiance().ToC3f() // assume spectrum as sun for now // NOTE: will also attenuate the moon below the horizon
                    let moonColorRgb = moonColorXYZ.XYZinC3fToLinearSRGB().Clamped(0.0f, float32 1e30).ToC3d()
                    let moonLuminance = moonColorRgb * 2.5e3 / 1.6e9
                    // Wiki: 2.5k cd/m^2 Moon surface vs 1.6e9 cd/m^2 of sun // NOTE: other paper states 4.9-5.4k average of moon disk near perigee (super moon)
                    // Geometric albedo of moon 0.12 -> could also calculate illumination from solar disk with this albedo
                    let srSun = Constant.PiTimesTwo * (1.0 - cos (Constant.RadiansPerDegree * 0.533 * 0.5))
                    let i = srSun * 1.6e9
                    let lum = i * 0.12 / Constant.PiTimesTwo
                    moonLuminance)
    
            let sgMoon =
                DrawCallInfo(1) 
                |> Sg.render IndexedGeometryMode.PointList
                |> Sg.effect [ Shaders.moonEffect ]
                |> Sg.cullMode' CullMode.None
                |> Sg.uniform "MoonColor" moonColor
                |> Sg.uniform "MoonDirection" moonDir
                |> Sg.uniform "MoonSize" moonDiameter
                |> Sg.uniform "SunDirection" moonDir // this is the fake sun direction for the sunSpirteGS
                |> Sg.uniform "SunSize" moonDiameter // this is the fake sun size for the sunSpriteGS
                |> Sg.uniform "CameraFov" cameraFov
                |> Sg.uniform "RealSunDirection" geoInfo.SunDirection
                |> Sg.texture' (Symbol.Create "MoonTexture") (FileTexture(Path.combine [ resourcePath; "8k_moon.jpg"], { wantSrgb = true; wantCompressed = false; wantMipMaps = true }) :> ITexture)
                |> Sg.writeBuffers' (Set.ofList [WriteBuffer.Color DefaultSemantic.Colors])
                |> Sg.blendMode' { BlendMode.Add with SourceAlphaFactor = BlendFactor.Zero }
                |> Sg.pass RenderPass.skyPass2
                |> Sg.onOff spaceVisible
        
            sgMoon

    module Sky = 
        
        let sg (geoInfo: AdaptiveGeoInfo) (skyParams: AdaptiveSkyParams) : ISg<_> = 
            
            // luminance of natural sky back during moonless night without light pollution is 22 magnitude per square arcsecond (mag/arcsec2) -> 1.7e-4 cd/m2
            // in cities 13 and 15 mag/arcsec2 -> 0.1 to 0.68 cd/m^2 = x1000
            // mag 20 = ~1.0 mcd/m2
            // mag 22 = 174 μcd/m2
            let skyBackLumiance = 1.7e-4
            let lightPol = skyParams.lightPollution |> AVal.map (fun x -> skyBackLumiance + skyBackLumiance * x) // lightPollution is factor relative to natural sky back

            let calcSphereRefl (viewDir : V3d) (lightDir : V3d) = 
                let rnd = HaltonRandomSeries(2, RandomSystem(42))
                let trafo = Trafo3d.FromNormalFrame(V3d.OOO, -viewDir) // the sphere normal is -viewDir
                // 64 sample solution
                let mutable sum = 0.0
                for i in 0..63 do 
                    let uv = RandomSample.Disk(rnd, 0)
                    let len = uv.Length
                    let nz = sqrt (1.0 - len * len) // [0, 1] -> local z vector
                    let n = V3d(uv.X, uv.Y, nz)
                    let nv = trafo.Forward.TransformDir(n)
                    let a = max 0.0 (Vec.dot nv lightDir)
                    sum <- sum + a
    
                let refl = sum / 64.0
                refl
  
            let moonRefl = (geoInfo.MoonDirection, geoInfo.SunDirection) ||> AVal.map2 calcSphereRefl

            let nightTimeFadeout (theta : float) =
                // https://en.wikipedia.org/wiki/Twilight
                let solar_elevation = Constant.PiHalf - theta - 0.05 // sun elevation -> 0 at horizon // subtract 0.05 rad to start fadeout at 3°
                //var fadeout = solar_elevation >= 0.0 ? 1.0 : Fun.Max(0.0, 1.0 - solar_elevation.Square() * 20.0); // total fadeout at ~ -0.22rad ~= 13°
                let fadeout = if solar_elevation >= 0.0 then 1.0 else max 0.0 (1.0 / (solar_elevation.Abs() * 50.0).Exp() - 0.00001)
                fadeout

            let sunPos, sunDist = geoInfo.SunPosition
            let moonPos, sunDist = geoInfo.MoonPosition
            
            let skyImage = adaptive {
                let! (phi, theta) = sunPos |> AVal.map (fun x -> x.Phi, x.Theta)
                let! turb = skyParams.turbidity
                let! res = skyParams.res
                let! moonRefl = moonRefl
                let! pol = lightPol
                let! cie = skyParams.cieType
                let! skyType = skyParams.skyType
                let! (phiMoon, thetaMoon) = moonPos |> AVal.map (fun x -> x.Phi, x.Theta)
    
                //Log.line "sun theta: %d" (90 - int (theta.DegreesFromRadians()))
                    
                let createSky p t =
                    match skyType with
                    | Preetham -> new PreethamSky(p, t, clamp 1.7 10.0 turb) :> IPhysicalSky
                    | CIE -> new CIESky(p, t, cie, -1.0, -1.0) :> IPhysicalSky
                    | HosekWilkie -> new HosekSky(p, t, clamp 1.0 10.0 turb, C3f.Gray50, Col.Format.CieXYZ) :> IPhysicalSky
    
                let skySun = createSky phi theta
                let skyMoon = createSky phiMoon thetaMoon
                    
                let lightPolFun (v : V3d) = 
                    (1.0 - (abs v.Z)) * 0.33 + 0.67
                    
                let pol : C3d = 
                    //let polCol = C3b(255uy, 209uy, 163uy).ToC3f() // 4000k sRGB
                    //let polCol = C3b(255uy, 228uy, 206uy).ToC3f() // 5000k sRGB
                    let polCol = C3b(64uy, 64uy, 96uy).ToC3f()
                    let polColLum = Vec.dot (polCol.ToV3d()) lumVector
                    pol * polCol.SRGBToXYZinC3f().ToC3d() / polColLum
    
                let sunFadeout = nightTimeFadeout theta
                let moonFadeout = nightTimeFadeout thetaMoon
                                       
                let cubeFaces = Array.init 6 (fun i -> 
                    PixImage.CreateCubeMapSide<float32, C4f>(i, res, 4, 
                        fun v ->
                            let mutable xyz = C3f.Black
                            xyz <- xyz + skySun.GetRadiance(v).ToC3d() * sunFadeout
                            xyz <- xyz + skyMoon.GetRadiance(v).ToC3d() * 2.5e3 / 1.6e9 * moonRefl * moonFadeout // TODO: actual amount of reflected light
                            xyz <- xyz + (lightPolFun v) * pol
                            let rgb = xyz.XYZinC3fToLinearSRGB().Clamped(0.0f, Single.MaxValue)
                            if rgb.ToV3f().AnyNaN then 
                                C4f.Black
                            else
                                //let lumTest = rgb.ToV3d().Dot(V3d(0.2, 0.7, 0.1))
                                rgb.ToC4f()
                        ) :> PixImage)
    
                // NOTE: magic face swap
                let cubeImg = 
                    PixImageCube.Create ([
                        CubeSide.PositiveX, cubeFaces.[2]
                        CubeSide.NegativeX, cubeFaces.[0]
                        CubeSide.PositiveY, cubeFaces.[5]
                        CubeSide.NegativeY, cubeFaces.[4]
                        CubeSide.PositiveZ, cubeFaces.[1]
                        CubeSide.NegativeZ, cubeFaces.[3]
                    ] |> Map.ofList)
                                    
                let tex = cubeImg |> PixImageCube.toTexture true
    
                return tex
            }
                
            let sgBkg = 
                DrawCallInfo(4) |> Sg.render IndexedGeometryMode.TriangleStrip
                |> Sg.effect [ Shaders.skyEffect ]
                |> Sg.cullMode' CullMode.None
                |> Sg.writeBuffers' (Set.ofList [WriteBuffer.Color DefaultSemantic.Colors])
                |> Sg.texture (Symbol.Create "SkyImage") skyImage
                |> Sg.pass RenderPass.skyPass1

            sgBkg

    module Sun =    

        let sg (geoInfo: AdaptiveGeoInfo) (turbidity: aval<float>) (cameraFov: aval<V2d>) (spaceVisible: aval<bool>) = 

            let sunPos, sunDist = geoInfo.SunPosition
            //let sunDiameter = Astronomy.AngularDiameter(Astronomy.SunDiameter, Astronomy.AU) |> AVal.constant
            let sunDiameter = sunDist |> AVal.map (fun distance -> Astronomy.AngularDiameter(Astronomy.SunDiameter, distance))

            let sunColor = 
                (sunPos, turbidity) ||> AVal.map2 (fun coord tu -> 
                    let sunColorXYZ = SunLightScattering(coord.Phi, coord.Theta, tu).GetRadiance().ToC3f()
                    let sunColorRgb = sunColorXYZ.XYZinC3fToLinearSRGB().Clamped(0.0f, float32 1e30)
                    //let sunLuminance = sunColorRgb * Constant.Pi // radiance in k lumen/sr (cd) ???
                    let sunLuminance = sunColorRgb
                    // Wiki: 1.6 Gcd / m^2  Solar disk at noon
                    sunLuminance
                ) 
    
            let sgSun = 
                DrawCallInfo(1) 
                |> Sg.render IndexedGeometryMode.PointList
                |> Sg.effect [ Shaders.sunEffect ]
                |> Sg.cullMode' CullMode.None
                |> Sg.uniform "SunColor" sunColor
                |> Sg.uniform "SunDirection" geoInfo.SunDirection
                |> Sg.uniform "SunSize" sunDiameter
                |> Sg.uniform "CameraFov" cameraFov
                |> Sg.writeBuffers' (Set.ofList [WriteBuffer.Color DefaultSemantic.Colors])
                |> Sg.blendMode' { BlendMode.Add with SourceAlphaFactor = BlendFactor.Zero }
                |> Sg.pass RenderPass.skyPass2
                |> Sg.onOff spaceVisible

            sgSun
    
    let skySGs (m: AdaptiveModel) (clientValues: Aardvark.Service.ClientValues) : ISg<_> = 
           
        let spaceVisible = 
            (m.skyParams.skyType, m.skyParams.cieType) ||> AVal.map2 (fun model cie ->
                match model with
                | CIE -> CIESkyExt.IsSunVisible(cie)
                | _ -> true
            )

        let cameraFov = m.fov |> AVal.map (fun fv -> V2d(fv * Constant.RadiansPerDegree, fv * Constant.RadiansPerDegree)) // NOTE: not actual fov of render control
        
        let starSg, starOverlaySg = Stars.sg m.geoInfo m.starParams cameraFov spaceVisible
        
        let sgSky = 
            [ 
                Sky.sg m.geoInfo m.skyParams 
                Sun.sg m.geoInfo m.skyParams.turbidity cameraFov spaceVisible
                Moon.sg m.geoInfo m.skyParams.turbidity cameraFov spaceVisible
                Planets.sg m.geoInfo m.planetScale m.starParams.magBoost cameraFov spaceVisible
                starSg
            ] |> Sg.ofSeq 

        let sgOverlay = 
            [ 
                Markers.sg()
                starOverlaySg 
            ] |> Sg.ofSeq

        let quad =
            let positions = [| V3f(-1,-1,0); V3f(1,-1,0); V3f(-1,1,0); V3f(1,1,0) |]
            let texcoords = [| V2f(0,0); V2f(1,0); V2f(0,1); V2f(1,1) |]
    
            DrawCallInfo(FaceVertexCount = 4, InstanceCount = 1)
            |> Sg.render IndexedGeometryMode.TriangleStrip 
            |> Sg.vertexAttribute' DefaultSemantic.Positions positions
            |> Sg.vertexAttribute' DefaultSemantic.DiffuseColorCoordinates texcoords

        let runtime = clientValues.runtime
                            
        let hdrColorSig = runtime.CreateFramebufferSignature([
                DefaultSemantic.Colors, TextureFormat.Rgba32f; 
                DefaultSemantic.DepthStencil, TextureFormat.Depth24Stencil8
                ]
            )       
                                
        let lumSig = runtime.CreateFramebufferSignature([
                DefaultSemantic.Colors, TextureFormat.R32f; 
                ]
            )    
    
        let sceneTex = 
            sgSky 
            |> Sg.viewTrafo clientValues.viewTrafo
            |> Sg.projTrafo clientValues.projTrafo
            |> Aardvark.SceneGraph.RuntimeSgExtensions.Sg.compile runtime hdrColorSig
            |> RenderTask.renderToColor clientValues.size
                                        
        let lumInitTask = 
            quad 
            |> Sg.effect [ Shaders.lumInitEffect ]
            |> Sg.uniform "SceneTexture" sceneTex
            |> Aardvark.SceneGraph.RuntimeSgExtensions.Sg.compile runtime lumSig
    
        let lumAtt =
            let size = clientValues.size
            let levels = size |> AVal.map (Vec.NormMax >> Fun.Log2Int)
            runtime.CreateTextureAttachment(
                runtime.CreateTexture2D(size, TextureFormat.R32f, levels, samples = AVal.constant 1), 0, 0
            )
    
        let lumFbo = runtime.CreateFramebuffer(lumSig, [DefaultSemantic.Colors, lumAtt])
    
        let lumTex =
            RenderTask.renderTo lumFbo lumInitTask
            |> AdaptiveResource.mapNonAdaptive (fun fbo ->
                let out = fbo.Attachments.[DefaultSemantic.Colors] :?> ITextureLevel
                runtime.GenerateMipMaps(out.Texture)
                out.Texture
            )
    
        let sgFinal =
            quad
            |> Sg.effect [ Shaders.tonemapEffect ]
            |> Sg.uniform "SceneTexture" sceneTex
            |> Sg.uniform "LumTexture" lumTex
            |> Sg.uniform "ExposureMode" m.exposureMode
            |> Sg.uniform "MiddleGray" m.key
            |> Sg.uniform "Exposure" m.exposure
            |> Sg.writeBuffers' (Set.ofList [WriteBuffer.Color DefaultSemantic.Colors])
            |> Sg.depthTest' DepthTest.None
    
        let sgOverlay =
            sgOverlay
            |> Sg.viewTrafo clientValues.viewTrafo
            |> Sg.projTrafo clientValues.projTrafo
    
        Sg.ofSeq [ 
            sgFinal |> Sg.pass RenderPass.skyPass1
            sgOverlay |> Sg.pass RenderPass.skyPass2
        ]

    let settingsUi (m: AdaptiveModel) : DomNode<Message> = 

        let skyModelCases = FSharpType.GetUnionCases typeof<SkyType>
        let skyModelValues = AMap.ofSeq( skyModelCases |> Seq.map (fun c -> (FSharpValue.MakeUnion(c, [||]) :?> SkyType, text (c.Name))) )
        let skyOptionMod : IAdaptiveValue<Option<SkyType>> = m.skyParams.skyType |> AVal.map Some
    
        //let cieModelCases = Enum.GetValues typeof<CIESkyType>
        let cieValues = AMap.ofArray(Array.init 15 (fun i -> (EnumHelpers.GetValue(i), text (Enum.GetName(typeof<CIESkyType>, i))))) // TODO: ToDescription
        let cieOptionMod : IAdaptiveValue<Option<CIESkyType>> = m.skyParams.cieType |> AVal.map Some
    
        let exposureModeCases = Enum.GetValues typeof<ExposureMode> :?> (ExposureMode [])
        let exposureModeValues = AMap.ofArray( exposureModeCases |> Array.map (fun c -> (c, text (Enum.GetName(typeof<ExposureMode>, c)) )))
        let exposureModeOptionMod : IAdaptiveValue<Option<ExposureMode>> = m.exposureMode |> AVal.map Some
        
        div [style "position: fixed; width:260pt; margin:0px; border-radius:10px; padding:12px; background:DarkSlateGray; color: white"] [ // sidebar 
            
            GeoApp.viewDetail m.geoInfo |> UI.map GeoMessage
            
            h4 [style "color:white"] [text "Sky"]
            Html.table [                    
                Html.row "Model" [ dropdown { allowEmpty = false; placeholder = "" } [ clazz "ui inverted selection dropdown" ] skyModelValues skyOptionMod SetSkyType ]
                Html.row "Turbidity" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.skyParams.turbidity; update SetTurbidity; step 0.1; largeStep 1.0; min 1.9; max 10.0; }]
                Html.row "Type" [ dropdown { allowEmpty = false; placeholder = "" } [ clazz "ui inverted selection dropdown" ] cieValues cieOptionMod SetCIEType ] 
                Html.row "Light Pollution" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.skyParams.lightPollution; update SetLightPollution; step 5.0; largeStep 50.0; min 0.0; max 10000.0; }]      
                // Html.row "Sun Position" [ dropdown { allowEmpty = false; placeholder = "" } [ clazz "ui inverted selection dropdown" ] spAlgoValues spOptionMod SetSunPosAlgo ]
                // Html.row "Resolution" [ text "TODO" ]
                Html.row "mag Boost" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.starParams.magBoost; update SetMagBoost; step 0.1; largeStep 1.0; min 0.0; max 10.0; }]
                Html.row "Planet Scale" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.planetScale; update SetPlanetScale; step 0.1; largeStep 1.0; min 1.0; max 10.0; }]
                Html.row "Star Signs" [ checkbox [clazz "ui inverted toggle checkbox"] m.starParams.starSigns ToggleStarSigns "" ]
                Html.row "Object Names" [ checkbox [clazz "ui inverted toggle checkbox"] m.starParams.objectNames ToggleObjectNames "" ]
                Html.row "mag Threshold" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.starParams.objectNameThreshold; update SetObjectNameThreshold; step 0.1; largeStep 1.0; min -20.0; max 20.0; }]
                    //Simple.labeledFloatInput' "Turbidity" 1.9 10.0 0.1 SetTurbidity m.turbidity (AttributeMap.ofList [ clazz "ui small labeled input"; style "width: 140pt; color : black"]) (AttributeMap.ofList [ clazz "ui label"; style "width: 70pt"]) 
                    //br []
                    //p [] [ text "Model: TODO" ]
                    //Simple.labeledIntegerInput "Resolution" 16 4096 SetResolution m.res
            ]
    
            h4 [style "color:white"] [text "Tonemapping"]
            Html.table [
                Html.row "Mode" [ dropdown { allowEmpty = false; placeholder = "" } [ clazz "ui inverted selection dropdown" ] exposureModeValues exposureModeOptionMod SetExposureMode ]
                Html.row "Exposure" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.exposure; update SetExposure; step 0.1; largeStep 1.0; min -20.0; max 10.0; }]
                Html.row "Middle Gray" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.key; update SetKey; step 0.001; largeStep 0.01; min 0.001; max 1.0; }]
            ]
        ]

    let view (m: AdaptiveModel) = 
        
        let frustum = m.fov |> AVal.map (fun fv -> Frustum.perspective fv 0.1 100.0 1.0)

        let att =
            [
                style "position: fixed; left: 0; top: 0; width: 100%; height: 100%"
                onWheel (fun w -> AdjustFoV w)
                attribute "data-samples" "8"
            ]
        
        let rc = 
            FreeFlyController.controlledControlWithClientValues m.cameraState CameraMessage frustum (AttributeMap.ofList att) RenderControlConfig.standard (fun clientValues ->
                    skySGs m clientValues
            )

        require Html.semui (
            body [] [
                rc
                settingsUi m
            ]
        )
    
    let threads (model : Model) = 
        FreeFlyController.threads model.cameraState |> ThreadPool.map CameraMessage

    let app =
        {
            initial = Model.initial
            update = update
            view = view
            threads = threads
            unpersist = Unpersist.instance
        }