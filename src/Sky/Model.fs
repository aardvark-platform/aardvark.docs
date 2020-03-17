namespace Sky.Model

open Aardvark.UI.Primitives
open Aardvark.Physics.Sky
open Adaptify
open Sky.Shaders

type SunPosAlgorithm = Strous// | NREL

type SkyModel = Preetham | CIE | HosekWilkie
    
[<ModelType>]
type Model =
    {
        // location & time
        altitude        : float
        gpsLong         : float
        gpsLat          : float
        timezone        : int
        time            : int64

        // sky
        model           : SkyModel
        turbidity       : float // [1.9, 10]
        cieType         : CIESkyType
        lightPollution  : float
        sunPosAlgo      : SunPosAlgorithm
        res             : int

        starSigns       : bool
        planetScale     : float
        magBoost        : float
        objectNames     : bool
        objectNameThreshold : float

        // tonemapping
        exposureMode    : ExposureMode
        exposure        : float
        key             : float

        cameraState     : CameraControllerState

        fov             : float
    }