﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winecrash.Engine.GUI
{
    public sealed class Canvas : GUIModule
    {
        public Camera UICamera { get; private set; }

        public Vector2I Size { get; private set; } = new Vector2I(1, 1);

        public static Canvas Main { get; private set; }

        public Vector2I Extents
        {
            get
            {
                return Size / 2;
            }
        }



        protected internal override void Creation()
        {
            if (Canvas.Main == null) Canvas.Main = this;
            Camera cam = this.UICamera = this.WObject.AddModule<Camera>();
            cam.Name = "Canvas Camera";
            cam.ProjectionType = CameraProjectionType.Orthographic;

            cam._FarClip = 200.0F;
            cam.NearClip = 0.0F;
            this.WObject.Position -= Vector3D.Forward * 100.0D;

            //UI Layer: 48;
            cam.RenderLayers = 1L << 48;

            cam.Depth = 10.0D;


        }

        protected internal override void PreUpdate()
        {
            this.Size = Graphics.Window.SurfaceResolution;

            UICamera.OrthographicSize = new Vector2F(this.Size.X, this.Size.Y);
        }

        public static Vector2D ScreenToUISpace(Vector2F screenCoords)
        {
            Vector2D extents = Canvas.Main.Extents;

            Vector2D remapped = WMath.Remap(screenCoords, Vector2D.Zero, Vector2D.One, -Canvas.Main.Extents, Canvas.Main.Extents);
            remapped.X *= -1D;
            return remapped;
        }
    }
}
