﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using System.Diagnostics;

namespace Winecrash.Engine
{
    public sealed class Camera : Module
    {
        internal static List<Camera> Cameras { get; set; } = new List<Camera>(1);

        public static Camera Main { get; set; }

        public Int64 RenderLayers { get; set; } = Int64.MaxValue;

        public double Depth { get; set; } = 0.0D;


        public CameraProjectionType ProjectionType { get; set; } = CameraProjectionType.Perspective;

        public Vector2F OrthographicSize { get; set; } = Vector2F.One;

        /// <summary>
        /// -Vertical- Field of View
        /// </summary>
        public float FOV { get; set; } = 45.0F;


        public Vector2I Resolution
        {
            get
            {
                return Graphics.Window.SurfaceResolution;//Viewport.Instance == null ? new Vector2I(1024, 1024) : new Vector2I(Viewport.Instance.ClientSize.Width, Viewport.Instance.ClientSize.Height);
            }
        }

        public float AspectRatio
        {
            get
            {
                Vector2I res = this.Resolution;
                return (float)res.X / (float)res.Y;
            }
        }

        public Vector2F ClipPlanes
        {
            get
            {
                return new Vector2F(this._NearClip, this._FarClip);
            }
        }

        public float _NearClip = 0.1F;
        public float NearClip
        {
            get
            {
                return this._NearClip;
            }

            set
            {
                this._NearClip = WMath.Clamp(value, 0.01F, this._FarClip - 0.01F);
            }
        }
        public float _FarClip = 100.0F;
        public float FarClip
        {
            get
            {
                return this._FarClip;
            }

            set
            {
                this._FarClip = WMath.Clamp(value, this._NearClip + 0.01F, 10000.0F);
            }
        }

        private Color256 _ClearColor = new Color256(1.0, 1.0, 1.0, 1.0);
        public Color256 ClearColor
        {
            get
            {
                return this._ClearColor;
            }

            set
            {
                Color256 col = this._ClearColor = value;
                Graphics.Window.InvokeRender(() => GL.ClearColor((float)col.R, (float)col.G, (float)col.B, 255));
            }
        }

        internal Matrix4 ViewMatrix
        {
            get
            {
                Vector3F p = this.WObject.Position;
                Vector3F t = this.WObject.Forward;
                Vector3F u = this.WObject.Up;

                return Matrix4.LookAt(new Vector3(p.X, p.Y, p.Z), new Vector3(p.X + t.X, p.Y + t.Y, p.Z + t.Z), new Vector3(u.X, u.Y, u.Z));
            }
        }

        internal Matrix4 ProjectionMatrix
        {
            get
            {
                return this.ProjectionType == CameraProjectionType.Perspective ? 
                    Matrix4.CreatePerspectiveFieldOfView(this.FOV * (float)WMath.DegToRad, this.AspectRatio, this._NearClip, this._FarClip):
                    Matrix4.CreateOrthographic(OrthographicSize.X, OrthographicSize.Y, this._NearClip, this._FarClip);
            }
        }

        protected internal override void Creation()
        {
            //this.ClearColor = new Color256(0.11D, 0.11D, 0.11D, 1.0D);

            Cameras.Add(this);
        }

        protected internal override void OnDelete()
        {
            Cameras.Remove(this);

            if(Main != null && Main == this)
            {
                Main = null;
            }
        }

        protected internal override void OnRender()
        {
            MeshRenderer[] mrs = MeshRenderer.ActiveMeshRenderers.ToArray();

            foreach (MeshRenderer mr in mrs.Where(mr => (mr.WObject.Layer & this.RenderLayers) != 0))
            {
                mr.Use(this);
            }
        }
    }
}