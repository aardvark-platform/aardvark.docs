/*
 * This example shows how to use the C# to construct a scene graph from first principles in order to finally
 * render a quad.
 * Note that this is for sake of demonstration in order to show the expressive but composable primitives.
 * In real-world scenarios one would use convience functions which internally use this low level
 * scene graph API.
 */
using System;
using Aardvark.Base;
using Aardvark.Rendering;
using CSharp.Data.Adaptive;
using Aardvark.SceneGraph;
using Aardvark.SceneGraph.CSharp;
using Effects = Aardvark.Rendering.Effects;
using Aardvark.Application.Slim;

namespace HelloWorldCSharp
{
    class HelloWorld
    {
        public static void Main(string[] args)
        {
            Aardvark.Base.Aardvark.Init();
            using (var app = /*new VulkanApplication() */ new OpenGlApplication())
            {
                var win = app.CreateGameWindow(samples: 8);
                // build object from indexgeometry primitives
                var cone = 
                        IndexedGeometryPrimitives.Cone.solidCone(
                            V3d.OOO, V3d.OOI, 1.0, 
                            0.2, 48, C4b.Red
                         ).ToSg();
                // or directly using scene graph
                var cube = SgPrimitives.Sg.box(
                    AdaptiveValue.Constant(C4b.Blue),
                    AdaptiveValue.Constant(Box3d.FromCenterAndSize(V3d.Zero, V3d.III))
                 ); 
                var initialViewTrafo = CameraView.LookAt(new V3d(0.2,1.2,0.9) * 3.0, V3d.OOO, V3d.OOI);
                var controlledViewTrafo =
                    Aardvark.Application.DefaultCameraController.control(win.Mouse, win.Keyboard,
                        win.Time, initialViewTrafo);
                var frustum = 
                    win.Sizes.Map(size => 
                        FrustumModule.perspective(60.0, 0.1, 10.0, size.X / (float)size.Y)
                    );

                // of course constructing scene graph nodes manually is tedious. therefore we use 
                // convinience extension functions which can be chaned together, each
                // wrapping a node around the previously constructed scene graph
                var scene =
                    cube
                    // next, we apply the shaders (this way, the shader becomes the root node -> all children now use
                    // this so called effect (a pipeline shader which combines all shader stages into one object)
                    .WithEffects(new[] { 
                            Effects.Trafo.Effect,
                            Effects.VertexColor.Effect,
                            Effects.SimpleLighting.Effect
                    })
                    .ViewTrafo(controlledViewTrafo.Map(vt => vt.ViewTrafo))
                    .ProjTrafo(frustum.Map<Frustum,Trafo3d>(f => f.ProjTrafo()));

                // next we use the aardvark scene graph compiler to construct a so called render task,
                // an optimized representation of the scene graph.
                var renderTask = app.Runtime.CompileRender(win.FramebufferSignature, scene);

                // next, we assign the rendertask to our render window.
                win.RenderTask = renderTask;

                win.Run();
            }
        }
    }

}
