﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

namespace Winecrash.Engine
{
    /// <summary>
    /// WObject are at the base of the game logic, they can hold multiple <see cref="Module"/> allowing scripting.
    /// <br>To add a module, use <see cref="WObject.AddModule{T}"/>. Remove and find also have their own methods.</br>
    /// </summary>
    public class WObject : BaseObject
    {
        internal readonly static object wobjectLocker = new object();
        // all the loaded wobjects
        internal static List<WObject> _WObjects { get; set; } = new List<WObject>(1);
        // modules linked to this wobject
        internal List<Module> _Modules { get; set; } = new List<Module>(1);

        /// <summary>
        /// Create an empty <see cref="WObject"/>
        /// </summary>
        public WObject() : base()
        {
            lock(wobjectLocker)
                _WObjects.Add(this);
        }

        /// <summary>
        /// Create an empty <see cref="WObject"/>
        /// </summary>
        /// <param name="name">The name of the WObject.</param>
        public WObject(string name) : base(name) 
        {
            lock(wobjectLocker)
                _WObjects.Add(this);
        }

        /// <summary>
        /// Render layer of the WObject. The render layer
        /// 0 = none
        /// Int64.MaxValue = everything
        /// 
        /// ex: 1<<32 = skybox
        /// </summary>
        public Int64 Layer { get; set; } = 1L; // Default layer = 1;

        

        private WObject _Parent = null;
        public WObject Parent
        {
            get
            {
                return this._Parent;
            }

            set
            {
                //Debug.Log("setting " + this.Name + "'s parent as " + value.Name);

                Vector3F oldGlobalPosition = this.Position;
                Quaternion oldGlobalRotation = this.Rotation;
                Vector3F oldGlobalScale = this.Scale;

                this._Parent = value;
                this._Parent._Children.Add(this);

                this.Position = oldGlobalPosition;
                this.Rotation = oldGlobalRotation;
                this.Scale = oldGlobalScale;

                //Debug.Log(this.Name + "'s parent is now " + value.Name);
            }
        }

        private List<WObject> _Children { get; set; } = new List<WObject>();
        public WObject[] Children
        {
            get
            {
                return _Children.ToArray();
            }
        }



        public Vector3F Position
        {
            get
            {   //                                      first set to scale                                     then rotate                          then add parent's position
                return this._Parent ? (this.LocalPosition * this._Parent.Scale).RotateAround(Vector3F.Zero, this._Parent.Rotation.Inverted) + this._Parent.Position : this.LocalPosition;
            }

            set
            {   //                                      first get relative position                              then rotate by invert                                 then scale
                this.LocalPosition = this._Parent ? (value - this._Parent.Position).RotateAround(Vector3F.Zero, this._Parent.Rotation) * (Vector3F.One / this._Parent.Scale) : value;
            }
        }
        public Vector3F LocalPosition { get; set; } = Vector3F.Zero;

        public Quaternion Rotation
        {
            get
            {
                return this._Parent ? this._Parent.Rotation * this.LocalRotation : this.LocalRotation;
            }

            set
            {
                this.LocalRotation = this._Parent ? this._Parent.Rotation.Inverted * value : value;
            }
        }
        public Quaternion LocalRotation { get; set; } = Quaternion.Identity;

        public Vector3F Scale
        {
            get
            {
                return this._Parent ? this._Parent.Scale * this.LocalScale : this.LocalScale;
            }

            set
            {
                this.LocalScale = this._Parent ? value / this._Parent.Scale : value;
            }
        }
        public Vector3F LocalScale { get; set; } = Vector3F.One;

        public Vector3F Right
        {
            get
            {
                return this.Rotation * Vector3F.Right;
            }
        }
        public Vector3F Left
        {
            get
            {
                return this.Rotation * Vector3F.Left;
            }
        }
        public Vector3F Up
        {
            get
            {
                return this.Rotation * Vector3F.Up;
            }

            set
            {
                this.Rotation = Quaternion.Identity;

                this.Rotation *= new Quaternion(Vector3D.Up, Vector2D.Angle(Vector2D.Right, value.XZ) * WMath.RadToDeg);

                this.Rotation *= new Quaternion(Vector3D.Right, Vector2D.Angle(Vector2D.Up, value.YZ) * WMath.RadToDeg);

                this.Rotation *= new Quaternion(Vector3D.Forward, Vector2D.Angle(Vector2D.Up, value.XY) * WMath.RadToDeg);
            }
        }
        public Vector3F Down
        {
            get
            {
                return this.Rotation * Vector3F.Down;
            }
        }
        public Vector3F Forward
        {
            get
            {
                return this.Rotation * Vector3F.Forward;
            }
        }
        public Vector3F Backward
        {
            get
            {
                return this.Rotation * Vector3F.Down;
            }
        }

        internal protected virtual Matrix4 TransformMatrix
        {
            get
            {
                Vector3F tra = this.Position;
                Quaternion rot = this.Rotation;
                Vector3F sca = this.Scale;

                Matrix4 transform =
                    Matrix4.CreateScale(sca.X, sca.Y, sca.Z) *
                    Matrix4.CreateFromQuaternion(new OpenTK.Quaternion((float)rot.X, (float)rot.Y, (float)rot.Z, (float)rot.W)) *
                    Matrix4.CreateTranslation(tra.X, tra.Y, tra.Z) *
                    Matrix4.Identity;

                return transform;
            }
        }

        public static WObject Find(string name)
        {
            List<WObject> wobjs;
            lock (wobjectLocker)
                wobjs = _WObjects.ToList(); 
            return wobjs.FirstOrDefault(w => w.Name == name);
        }

        public T AddModule<T>() where T : Module
        {
            T mod = (T)Activator.CreateInstance(typeof(T));

            if(mod.Undeletable && !this.Undeletable)
            {
                Debug.LogError("Cannot add undeletable module on deletable WObject !");

                Group.GetGroup(0).RemoveModule(mod);
                return null;
            }

            mod.Name = mod.GetType().Name;
            mod.WObject = this;

            this._Modules.Add(mod);

            mod.Creation();

            if(this.Enabled)
            {
                mod.OnEnable();
            }
            else
            {
                mod.OnDisable();
            }

            return mod;
        }

        public T GetModule<T>() where T : Module
        {
            foreach (Module mod in this._Modules) if (mod is T module) return module;
            return null;
        }

        public List<T> GetModules<T>() where T : Module
        {
            List<T> modules = new List<T>();

            foreach (Module mod in _Modules) if (mod is T module) modules.Add(module);

            if (modules.Count == 0) return null;
            else return modules;
        }

        public T AddOrGetModule<T>() where T : Module
        {
            return this.GetModule<T>() ?? this.AddModule<T>();
        }

        internal sealed override void SetEnable(bool status)
        {
            for (int i = 0; i < _Children.Count; i++)
                _Children[i].Enabled = status;

            Module[] modules = this._Modules.ToArray();
            for (int i = 0; i < modules.Length; i++)
            {
                modules[i].Enabled = status;
            }

            if (status == true && this._Parent && !this._Parent.Enabled)
                this._Parent.Enabled = true;

            base.SetEnable(status);
        }

        internal sealed override void ForcedDelete()
        {
            for (int i = 0; i < _Modules.Count; i++)
            {
                _Modules[i].ForcedDelete();
            }

            for (int i = 0; i < _Children.Count; i++)
            {
                _Children[i].ForcedDelete();
            }

            this._Parent = null;
            _Children.Clear();
            _Children = null;

            _Modules.Clear();
            _Modules = null;
            lock (wobjectLocker)
                _WObjects.Remove(this);

            base.Delete();
        }
        public sealed override void Delete()
        {
            if (this.Undeletable)
            {
                Debug.LogWarning("Unable to delete " + this + " : wobject undeletable.");
                return;
            }

            for (int i = 0; i < _Modules.Count; i++)
            {
                _Modules[i].Delete();
            } 

            for (int i = 0; i < _Children.Count; i++)
            {
                _Children[i].Delete();
            }

            this._Parent = null;
            _Children.Clear();
            _Children = null;

            _Modules.Clear();
            _Modules = null;
            lock (wobjectLocker)
                _WObjects.Remove(this);

            base.Delete();
        }
    }
}
