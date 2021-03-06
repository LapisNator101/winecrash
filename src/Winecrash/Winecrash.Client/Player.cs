﻿using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Winecrash.Engine;

namespace Winecrash.Game
{
    public delegate void PlayerChangeChunkDelegate(Chunk chunk);

    public class Player : Module
    {
        public static event PlayerChangeChunkDelegate OnChangeChunk;

        public static Player Instance { get; private set; }

        public Camera FPSCamera;

        public float WalkMaxSpeed = 4.3F;
        public float RunMaxSpeed = 5.6F;
        public float SneakSpeed = 1.30F;
        public float AirWalkMaxSpeed = 4.5F;
        public float AirRunMaxSpeed = 6.0F;
        public float FallMaxSpeed = -78.0F;
        

        public float WalkForce = 30F;
        public float RunForce = 40F;
        public float AirWalkForce = 20F;
        public float AirRunForce = 30F;
        public float JumpForce = 8.2F;

        public float TurnFactor = 0.0F;
        public float SlowFactor = 20.0F;

        public float JumpCooldown = 0.25F;
        public float WaitTimeBeforeJump = 0.06F;

        private float JumpWaitTime = 0.0F;
        private float TimeSinceLastJump = 0.0F;

        public float CameraRotationSpeed = 5.0F;

        public bool Grounded { get; private set; } = false;

        public Vector2D Angles = new Vector2D();

        private RigidBody _Rb;
        private BoxCollider _Bc;

        public Vector3D CameraShift = new Vector3D(0, 0.7D, 0);

        public RaycastChunkHit? ViewRayHit { get; set; } = null;

        public double HitRange = 4.5D;

        public Chunk CurrentChunk { get; private set; }

        public WObject Cursor3D;

        public void ForceInvokeChunkChange()
        {
            World.GlobalToLocal(this.WObject.Position, out Vector3I cpos, out _);

            Ticket tck = Ticket.GetTicket(cpos.XY);

            if (tck != null)
            {
                Chunk c = tck.Chunk;

                if (c != null)
                {
                    if (previousChunk != null && (previousChunk != c))
                    {
                        OnChangeChunk?.Invoke(c);
                    }
                }

                previousChunk = c;
            }

            else
            {
                tck = Ticket.CreateTicket(cpos.X, cpos.Y, 30, TicketTypes.Player, TicketPreviousDirection.None, true, -1);
            }
        }
        //public WObject HitSphere;

        public static string[] debug_items = new[]
        {
            "winecrash:direction",
            "winecrash:air",
            "winecrash:grass",
            "winecrash:dirt",
            "winecrash:stone",
            "winecrash:bedrock",
            "winecrash:sand",
            "winecrash:leaves",
            "winecrash:log"
        };
        
        public static Texture[] break_textures = new Texture[]
        {
            new Texture("assets/textures/break/break_0.png"),
            new Texture("assets/textures/break/break_1.png"),
            new Texture("assets/textures/break/break_2.png"),
            new Texture("assets/textures/break/break_3.png"),
            new Texture("assets/textures/break/break_4.png"),
            new Texture("assets/textures/break/break_5.png"),
            new Texture("assets/textures/break/break_6.png"),
            new Texture("assets/textures/break/break_7.png"),
            new Texture("assets/textures/break/break_8.png"),
            new Texture("assets/textures/break/break_9.png")
        };

        public int SelectedIndex { get; set; } = 0;

        protected override void Creation()
        {
            Instance = this;
            this.FPSCamera = Camera.Main;
            this._Rb = this.WObject.GetModule<RigidBody>();
            this._Bc = this.WObject.GetModule<BoxCollider>();


            Cursor3D = new WObject("Block Cursor");
            MeshRenderer mr = Cursor3D.AddModule<MeshRenderer>();
            mr.Material = new Material(Shader.Find("Cursor"));
            mr.Material.SetData<Vector4>("color", new Color256(0.0D,0.0D,0.0D,1.0D));
            mr.Material.SetData<float>("border", 0.1F);
            mr.Material.SetData<float>("opacity", 0.85F);
            mr.Mesh = Mesh.LoadFile("assets/models/Cube.obj", MeshFormats.Wavefront);
            //mr.Wireframe = true;
            Cursor3D.Scale *= 1.005F;

            /*HitSphere = new WObject("Debug hit sphere");
            MeshRenderer hmr = HitSphere.AddModule<MeshRenderer>();
            hmr.Material = new Material(Shader.Find("Unlit"));
            hmr.Material.SetData<Vector4>("color", new Color256(0, 1, 0, 1));
            hmr.Mesh = Mesh.LoadFile("assets/models/Skysphere.obj", MeshFormats.Wavefront);
            hmr.Wireframe = true;
            HitSphere.Scale *= 0.05F;*/

            Cursor3D.Enabled /*= HitSphere.Enabled*/ = false;

            for (int i = 0; i < 10; i++)
            {
                mr.Material.SetData<Texture>("albedo" + i, break_textures[i]);
            }
        }

        private void CameraRotation()
        {
            Vector2D deltas = Input.MouseDelta;

            double ax = (Angles.X + (deltas.X * Input.MouseSensivity * Time.DeltaTime)) % 360.0D;
            double ay = WMath.Clamp((Angles.Y + (deltas.Y * Input.MouseSensivity * Time.DeltaTime)), -89.9D, 89.9D);

            Angles = new Vector2D(ax, ay);

            this.FPSCamera.WObject.Rotation = new Engine.Quaternion(-ay, ax, 0.0F);
        }

        private void Move()
        {
            if (!FreeCam.FreeCTRL)
            {
                Vector3D walkdir = Vector3D.Zero;

                Vector3D lookDir = FPSCamera.WObject.Forward;

                Vector3D walkForward = new Vector3D(lookDir.X, 0, lookDir.Z).Normalize();

                Vector3D rightDir = FPSCamera.WObject.Right;

                Vector3D walkRight = new Vector3D(rightDir.X, 0, rightDir.Z).Normalize();

                bool run = false;



                if (Input.IsPressed(GameInput.Key("Forward")))
                {
                    if (Input.IsPressed(GameInput.Key("Run")))
                    {
                        run = true;
                    }
                    walkdir += walkForward;
                }
                if (Input.IsPressed(GameInput.Key("Backward")))
                {
                    walkdir -= walkForward;
                }

                if (Input.IsPressed(GameInput.Key("Right")))
                {
                    walkdir -= walkRight;
                }
                if (Input.IsPressed(GameInput.Key("Left")))
                {
                    walkdir += walkRight;
                }

                Vector3D walkDirBase = walkdir.Normalize();

                float force = 0.0F;
                CheckGrounded();
                if (Grounded)
                {
                    force = run ? RunForce : WalkForce;
                }
                else
                {

                    force = run ? AirRunForce : AirWalkForce;

                }

                this._Rb.Velocity += walkdir * force * Time.DeltaTime;

                float MaxHorizontalSpeed = 0.0F;

                if (Grounded)
                {
                    MaxHorizontalSpeed = run ? RunMaxSpeed : WalkMaxSpeed;
                }

                else
                {
                    MaxHorizontalSpeed = run ? AirRunMaxSpeed : AirWalkMaxSpeed;
                }


                if (this._Rb.Velocity.XZ.Length > MaxHorizontalSpeed)
                {
                    double velY = this._Rb.Velocity.Y;
                    this._Rb.Velocity = new Vector3D(this._Rb.Velocity.X, 0, this._Rb.Velocity.Z).Normalized * MaxHorizontalSpeed;

                    this._Rb.Velocity += Vector3D.Up * velY;

                }

                if (this._Rb.Velocity.Y < this.FallMaxSpeed)
                {
                    double fallSpeed = this._Rb.Velocity.Y;

                    this._Rb.Velocity *= new Vector3D(1, 0, 1);
                    this._Rb.Velocity += new Vector3D(0, fallSpeed, 0);
                }


                Vector3D flattenVel = new Vector3D(this._Rb.Velocity.X, 0, this._Rb.Velocity.Z);

                //slowdown
                if (force == 0.0F || walkDirBase == Vector3D.Zero)
                {
                    Vector2D flatVel = this._Rb.Velocity.XZ;

                    Vector3D horVel = new Vector3D(flatVel.X, 0, flatVel.Y);

                    if (flatVel.Length < 1.0F)
                    {
                        this._Rb.Velocity *= Vector3D.Up;
                    }

                    else
                    {
                        this._Rb.Velocity -= horVel.Normalized * SlowFactor * Time.DeltaTime;
                    }
                }

                else
                {
                    double angleVelFwd = Vector3D.Angle(walkDirBase, _Rb.Velocity.Normalized);

                    this._Rb.Velocity.RotateAround(Vector3D.Zero, Vector3D.Up, (float)angleVelFwd * TurnFactor * (float)Time.DeltaTime);
                }
            }
        }

        private Chunk previousChunk = null;

        private void ViewHit()
        {
            if (RaycastChunk(new Ray(this.FPSCamera.WObject.Position, this.FPSCamera.WObject.Forward), HitRange, out RaycastChunkHit hit, 0.1))
            {
                ViewRayHit = new RaycastChunkHit?(hit);
            }
            else
            {
                ViewRayHit = null;
            }
        }

        public double TimeBreaking = 0.0D;
        private Vector3I? previousFrameLPos = null;

        private void MainInteraction()
        {
            Cursor3D.Enabled = /*HitSphere.Enabled =*/ ViewRayHit != null;
            if (Cursor3D.Enabled)
            {
                Cursor3D.Position = Vector3F.One * 0.5F + (Vector3F)(ViewRayHit.Value.LocalPosition + new Vector3I(ViewRayHit.Value.Chunk.Position.X * 16, 0, ViewRayHit.Value.Chunk.Position.Y * 16));

                if(previousFrameLPos != ViewRayHit.Value.LocalPosition)
                {
                    TimeBreaking = 0.0D;
                }

                if (Input.IsPressed(Keys.MouseLeftButton))
                {
                    TimeBreaking += Time.DeltaTime;
                }
                else
                {
                    TimeBreaking = 0.0D;
                }

                if (Input.IsPressing(Keys.MouseRightButton))
                {
                    Vector3F normal = ViewRayHit.Value.Normal;

                    World.GlobalToLocal(ViewRayHit.Value.GlobalPosition + normal, out Vector3I cpos, out Vector3I bpos);
                    Ticket tck = Ticket.GetTicket(new Vector2I(cpos.X, cpos.Y));

                    tck?.Chunk.Edit(bpos.X, bpos.Y, bpos.Z, ItemCache.Get<Block>(debug_items[SelectedIndex]));
                }

                Cursor3D.GetModule<MeshRenderer>().Material.SetData<float>("breakPct", (float)(TimeBreaking / ViewRayHit.Value.Block.DigTime));

                if(ViewRayHit.Value.Block.DigTime != -1 && TimeBreaking >= ViewRayHit.Value.Block.DigTime)
                {
                    Vector3I p = ViewRayHit.Value.LocalPosition;
                    ViewRayHit.Value.Chunk.Edit(p.X, p.Y, p.Z, ItemCache.Get<Block>("winecrash:air"));
                    Block.PlayerTickNeighbors(ViewRayHit.Value.Chunk, new Vector3I(p.X, p.Y, p.Z));
                }

                previousFrameLPos = ViewRayHit.Value.LocalPosition;
            }
            else
            {
                previousFrameLPos = null;
                TimeBreaking = 0.0D;
            }
        }

        protected override void FixedUpdate()
        {
            if (FreeCam.FreeCTRL)
            {
                this._Rb.UseGravity = false;
                this._Rb.Velocity = Vector3D.Zero;

                /*if (Input.IsPressing(Keys.MouseMiddleButton))
                {
                    this.WObject.Position += Vector3F.Left * 12_550_821;
                }*/

               

                World.GlobalToLocal(this.WObject.Position, out Vector3I cpos, out _);

                Ticket tck = Ticket.GetTicket(cpos.XY);

                if (tck != null)
                {
                    Chunk c = tck.Chunk;

                    if (c != null)
                    {
                        if (previousChunk != null && (previousChunk != c))
                        {
                            OnChangeChunk?.Invoke(c);
                        }
                    }

                    previousChunk = c;
                }

                return;
            }

            else
            {
                this._Rb.UseGravity = true;
            }
            Move();

            if (CurrentChunk == null || !CurrentChunk.BuiltOnce || Grounded)
            {
                this._Rb.Velocity *= new Vector3D(1, 0, 1);
            }

            if (Grounded)
            {
                JumpWaitTime += (float)Time.FixedDeltaTime;
            }

            TimeSinceLastJump += (float)Time.FixedDeltaTime;

            Collisions();


        }
        protected override void Update()
        {
            int scroll = (int)Input.MouseScrollDelta;
            if (!FreeCam.FreeCTRL && scroll != 0.0D)
            {
                SelectedIndex -= (int)Input.MouseScrollDelta;

                if (SelectedIndex < 0) SelectedIndex += 9;
                else if (SelectedIndex > 8) SelectedIndex -= 9;


                Engine.GUI.Image img = WObject.Find("Item Cursor").GetModule<Engine.GUI.Image>();

                const float shift = 0.1093F;

                img.MinAnchor = new Vector2F(SelectedIndex * shift, 0.0F);
                img.MaxAnchor = new Vector2F(0.125F + SelectedIndex * shift, 1.0F);
            }
            

            //if (!FreeCam.FreeCTRL)
            CameraRotation();

            

            ViewHit();

            MainInteraction();

            if (!FreeCam.FreeCTRL)
            if (Input.IsPressed(GameInput.Key("Jump")))
            {
                if (Grounded && JumpWaitTime >= WaitTimeBeforeJump && TimeSinceLastJump >= JumpCooldown)
                {
                    JumpWaitTime = 0.0F;
                    TimeSinceLastJump = 0.0F;
                    this._Rb.Velocity += Vector3D.Up * JumpForce;
                }
            }

            

            if (Input.IsPressed(Keys.F3) && Input.IsPressing(Keys.A))
            {
                Debug.Log("Reconstructing " + Chunk.Chunks.Count + " chunks");
                foreach (Chunk chunk in Chunk.Chunks)
                {
                    chunk.BuildEndFrame = true;
                }
            }
        }

        private void Collisions()
        {
            CheckTop();
            CheckRight();
            CheckLeft();
            CheckForward();
            ChechBackward();
            //CheckLeft();
        }

#region Rays
        private void ChechBackward()
        {
            if (this._Rb.Velocity.Z > 0.0D) return;

            if (
            //right up
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Right * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Backward * _Bc.Extents.Z),
                Vector3D.Backward), 0.05D, out RaycastChunkHit hit, 0.05D)

            ||
            //right down
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Right * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Backward * _Bc.Extents.Z),
                Vector3D.Backward), 0.05D, out hit, 0.05D)
            ||
            //left down
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Left * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Backward * _Bc.Extents.Z),
                Vector3D.Backward), 0.05D, out hit, 0.05D)
            ||
            //left up
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Left * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Backward * _Bc.Extents.Z),
                Vector3D.Backward), 0.05D, out hit, 0.05D))
            {
                this._Rb.Velocity *= new Vector3D(1.0D, 1.0D, 0);

                this.WObject.Position *= new Vector3D(1.0, 1.0D, 0);

                this.WObject.Position -= Vector3D.Backward * (hit.GlobalPosition.Z + _Bc.Extents.Z);
            }
        }
        private void CheckForward()
        {
            if (this._Rb.Velocity.Z < 0.0) return;

            if (
            //right up
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Right * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Forward * _Bc.Extents.Z),
                Vector3D.Forward), 0.05D, out RaycastChunkHit hit, 0.05D)

            ||
            //right down
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Right * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Forward * _Bc.Extents.Z),
                Vector3D.Forward), 0.05D, out hit, 0.05D)
            ||
            //left down
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Left * _Bc.Extents.X) * 0.8F +
                    (Vector3D.Forward * _Bc.Extents.Z),
                Vector3D.Forward), 0.05D, out hit, 0.05D)
            ||
            //left up
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * (float)_Bc.Extents.Y) * 0.8D +
                    (Vector3D.Left * _Bc.Extents.X) * 0.8F +
                    (Vector3D.Forward * _Bc.Extents.Z),
                Vector3D.Forward), 0.05D, out hit, 0.05D))
            {
                
                this._Rb.Velocity *= new Vector3D(1.0D, 1.0D, 0.0D);

                this.WObject.Position *= new Vector3D(1.0D, 1.0D, 0.0D);

                this.WObject.Position += Vector3D.Forward * (hit.GlobalPosition.Z - _Bc.Extents.Z);
            }
        }
        private void CheckLeft()
        {
            if (this._Rb.Velocity.X < 0.0D) return;

            if (
            //front up
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Right * _Bc.Extents.X) +
                    (Vector3D.Forward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Left), 0.05D, out RaycastChunkHit hit)

            ||
            //front down
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Right * _Bc.Extents.X) +
                    (Vector3D.Forward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Left), 0.05D, out hit)
            ||
            //back down
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Right * _Bc.Extents.X) +
                    (Vector3D.Backward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Left), 0.05D, out hit)
            ||
            //back up
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Right * _Bc.Extents.X) +
                    (Vector3D.Backward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Left), 0.05D, out hit))
            {

                this._Rb.Velocity *= new Vector3D(0.0D, 1.0D, 1.0D);

                this.WObject.Position *= new Vector3D(0.0D, 1.0D, 1.0D);

                this.WObject.Position -= Vector3D.Left * (hit.GlobalPosition.X - _Bc.Extents.X);
            }
        }
        private void CheckRight()
        {
            if (this._Rb.Velocity.X > 0.0D) return;

            if (
            //front up
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Left * _Bc.Extents.X) +
                    (Vector3D.Forward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Right), 0.05D, out RaycastChunkHit hit)

            ||
            //front down
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Left * _Bc.Extents.X) +
                    (Vector3D.Forward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Right), 0.05D, out hit)
            ||
            //back down
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Left * _Bc.Extents.X) +
                    (Vector3D.Backward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Right), 0.05D, out hit)
            ||
            //back up
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) * 0.8D +
                    (Vector3D.Left * _Bc.Extents.X) +
                    (Vector3D.Backward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Right), 0.05D, out hit))
            {

                this._Rb.Velocity *= new Vector3D(0.0D, 1.0D, 1.0D);

                this.WObject.Position *= new Vector3D(0.0D, 1.0D, 1.0D);

                this.WObject.Position += Vector3D.Right * (hit.GlobalPosition.X + _Bc.Extents.X);
            }
        }
        private void CheckTop()
        {
            if(
            //front right
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) +
                    (Vector3D.Right * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Forward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Up), 0.05D, out RaycastChunkHit hit)

            ||
            //front left
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) +
                    (Vector3D.Left * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Forward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Up), 0.05D, out hit)
            ||
            //back left
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) +
                    (Vector3D.Left * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Backward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Up), 0.05D, out hit)
            ||
            //back right
            RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Up * _Bc.Extents.Y) +
                    (Vector3D.Right * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Backward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Up), 0.05D, out hit))
            {
                this._Rb.Velocity *= new Vector3D(1, 0, 1);

                this.WObject.Position *= new Vector3D(1, 0, 1);

                this.WObject.Position += Vector3D.Up * (hit.LocalPosition.Y - _Bc.Extents.Y - 0.05D);
            }
        }
        private void CheckGrounded()
        {
            if (this._Rb.Velocity.Y > 0)
            {
                Grounded = false;
                return;
            }
            //front right
            Grounded = RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) +
                    (Vector3D.Right * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Forward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Down), 0.05D, out RaycastChunkHit hit);


            //front left
            if (!Grounded)
                Grounded = RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) +
                    (Vector3D.Left * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Forward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Down), 0.05D, out hit);

            //back left
            if (!Grounded)
                Grounded = RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) +
                    (Vector3D.Left * _Bc.Extents.X) * 0.8D +
                    (Vector3D.Backward * _Bc.Extents.Z) * 0.8D,
                Vector3D.Down), 0.05D, out hit);

            //back right
            if (!Grounded)
                Grounded = RaycastChunk(
                new Ray(
                    this.WObject.Position + (Vector3D.Down * _Bc.Extents.Y) +
                    (Vector3D.Right * _Bc.Extents.X) * 0.9D +
                    (Vector3D.Backward * _Bc.Extents.Z) * 0.9D,
                Vector3D.Down), 0.05D, out hit);


            if (Grounded)
            {
                double ypos = this.WObject.Position.Y;

                this._Rb.Velocity *= new Vector3D(1, 0, 1);
                this.WObject.Position *= new Vector3D(1, 0, 1);
                this.WObject.Position += Vector3D.Up * (hit.LocalPosition.Y + _Bc.Extents.Y * 2 + 0.05D);

                
            }
            CurrentChunk = hit.Chunk;
        }
#endregion


        protected override void LateUpdate()
        {
            this.FPSCamera.WObject.Position = this.WObject.Position + CameraShift;
        }

        public bool RaycastChunk(Ray ray, double length, out RaycastChunkHit hit, double precision = 0.1D)
        {
            Vector3D pos = ray.Origin;
            double distance = 0.0D;

            Vector3I cpos;
            Vector3I bpos;

            Ticket ticket = null;
            Chunk chunk = null;

            Block block = null;

            while (distance < length)
            {
                World.GlobalToLocal(pos, out cpos, out bpos);


                ticket = Ticket.GetTicket(cpos.XY);

                if(ticket != null)
                {
                    chunk = ticket.Chunk;

                    if(chunk != null)
                    {
                        bpos.X = WMath.Clamp(bpos.X, 0, 15);
                        bpos.Y = WMath.Clamp(bpos.Y, 0, 255);
                        bpos.Z = WMath.Clamp(bpos.Z, 0, 15);

                        block = chunk[bpos.X, bpos.Y, bpos.Z];

                        if (!block.Transparent)
                        {
                            Vector3I blockGlobalUnitPosition = new Vector3I(bpos.X + cpos.X * 16, bpos.Y, bpos.Z + cpos.Y * 16);

                            Vector3D blockGlobalPosition = (Vector3D)blockGlobalUnitPosition + Vector3D.One * 0.5D;

                            Vector3D rp = pos - blockGlobalPosition;

                            Vector3D n = new Vector3D();
                            
                            //up
                            if(rp.Y > Math.Abs(rp.X) && rp.Y > Math.Abs(rp.Z))
                            {
                                n.X = 0.0;
                                n.Y = 1.0;
                                n.Z = 0.0;
                            }
                            //down
                            else if(rp.Y < Math.Abs(rp.X) * -1 && rp.Y < Math.Abs(rp.Z) * -1)
                            {
                                n.X = 0.0;
                                n.Y = -1.0;
                                n.Z = 0.0;
                            }
                            //east
                            else if(rp.X > Math.Abs(rp.Z))
                            {
                                n.X = 1.0;
                                n.Y = 0.0;
                                n.Z = 0.0;
                            }
                            //west
                            else if(rp.X < Math.Abs(rp.Z) * -1)
                            {
                                n.X = -1.0;
                                n.Y = 0.0;
                                n.Z = 0.0;
                            }
                            //North
                            else if (rp.Z > 0.0)
                            {
                                n.X = 0.0;
                                n.Y = 0.0;
                                n.Z = 1.0;
                            }
                            //South
                            else
                            {
                                n.X = 0.0;
                                n.Y = 0.0;
                                n.Z = -1.0;
                            }

                            hit = new RaycastChunkHit(pos, n, distance, block, chunk, bpos);

                            return true;
                        }
                    }
                }

                else
                {
                    hit = new RaycastChunkHit();
                    return false;
                }

                pos += ray.Direction * precision;
                distance += precision;
            }

            hit = new RaycastChunkHit(pos, Vector3D.Up, distance, block, chunk, Vector3I.Zero);
            return false;
        }

    }
}
