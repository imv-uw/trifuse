using Core.Interfaces;
using Core.Tools;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Quick.Pattern
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MirrorAtom : IAtom
    {
        [JsonProperty] IAtom _template;
        [JsonProperty] bool _root;

        [JsonConstructor]
        MirrorAtom() { }

        public MirrorAtom(IAtom template, bool root, ITransformNode parent)
        {
            _template = template;
            _root = root;
            Parent = parent;
        }

        public float X
        {
            get { return Xyz.X; }
            set
            {
                Vector3 desired = Xyz;
                desired.X = value;
                Xyz = desired;
            }
        }

        public float Y
        {
            get { return Xyz.Y; }
            set
            {
                Vector3 desired = Xyz;
                desired.Y = value;
                Xyz = desired;
            }
        }

        public float Z
        {
            get { return Xyz.Z; }
            set
            {
                Vector3 desired = Xyz;
                desired.Z = value;
                Xyz = desired;
            }
        }

        public Vector3 Xyz
        {
            get => Vector3.Transform(_template.RawXyz, Parent.TotalTransform);
            set => _template.RawXyz = Vector3.Transform(value, Matrix.Invert(Parent.TotalTransform));
        }

        public string Name => _template.Name;

        public float Mass => _template.Mass;

        public Element Element => _template.Element;

        public bool IsHydrogen => _template.IsHydrogen;

        public bool IsSidechain => _template.IsSidechain;

        public bool IsMainchain => _template.IsMainchain;

        public ITransformNode Parent { get; set; }

        public bool IsHeavy => _template.IsHeavy;

        public Vector3 RawXyz
        {
            get => _template.RawXyz;
            set => _template.RawXyz = Vector3.Transform(value, Matrix.Invert(Parent.TotalTransform));
        }

        public object DeepCopy()
        {
            DeepCopyObjectGraph graph = new DeepCopyObjectGraph();
            return DeepCopyFindOrCreate(graph);
        }

        public object DeepCopyFindOrCreate(IDeepCloneObjectGraph context)
        {
            if (context.TryGetClone(this, out object clone))
                return (IAtom)clone;

            MirrorAtom atom = new MirrorAtom();
            context.Add(this, atom);
            DeepCopyPopulateFields(context, atom);
            return atom;
        }

        public void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            MirrorAtom atom = (MirrorAtom)clone;
            atom._template = _template == null? null : (IAtom)_template.DeepCopyFindOrCreate(graph);
            atom.Parent = Parent == null? null : (ITransformNode) Parent.DeepCopyFindOrCreate(graph);
            atom._root = _root;
        }

        public IAtom GetMirroredElement(bool root, ITransformNode parent)
        {
            return new MirrorAtom(this, root, parent);
        }

        public IAtom GetMirrorTemplate()
        {
            return _template.GetMirrorTemplate();
        }

        public void Rotate(Quaternion rotation, Vector3 origin)
        {
            Matrix transform = MatrixUtil.GetRotation(ref rotation, ref origin);
            Xyz = Vector3.Transform(Xyz, transform);
        }

        public void RotateNode(Quaternion rotation, Vector3 origin)
        {
            throw new InvalidOperationException("atom is transformable, but not a node itself that stores a transform matrix");
        }

        public void Transform(Matrix transform)
        {
            Xyz = Vector3.Transform(Xyz, transform);
        }

        public void TransformNode(Matrix transform)
        {
            throw new InvalidOperationException("atom is transformable, but not a node itself that stores a transform matrix");
        }

        public void Translate(Vector3 translation)
        {
            Matrix transform = Matrix.Identity;
            transform.Translation = translation;
            Xyz = Vector3.Transform(Xyz, transform);
        }

        public void TranslateNode(Vector3 translation)
        {
            throw new InvalidOperationException("atom is transformable, but not a node itself that stores a transform matrix");
        }
    }
}
