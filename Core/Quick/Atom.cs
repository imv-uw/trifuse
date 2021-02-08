using Core.Interfaces;
using Core.Quick.Pattern;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Atom : IAtom
    {
        [JsonProperty] Vector3 _xyz = new Vector3();
        [JsonProperty] Vector3 _cachedXyz;
        [JsonProperty] Vector3 _cachedXyzPostTransform;
        [JsonProperty] Matrix _cachedTotalParentTransform;


        [JsonProperty] AtomDefinition _definition;

        [JsonConstructor]
        Atom() { }

        public Atom(AtomDefinition definition)
        {
            _definition = definition;
            _xyz.X = definition.X;
            _xyz.Y = definition.Y;
            _xyz.Z = definition.Z; 
        }

        public Atom(AtomDefinition definition, Vector3 xyz)
        {
            _definition = definition;
            Xyz = xyz;
        }

        public Atom(Atom other)
        {
            _xyz = other._xyz;
            _definition = other._definition;
        }

        public string Name { get { return _definition.Name; } }
        public float Mass  { get { return _definition.Mass; } }
        public Element Element { get { return _definition.Element; } }
        public bool IsHydrogen { get { return _definition.Element == Element.H; } }
        public bool IsHeavy { get { return _definition.Element != Element.H; } }
        public bool IsSidechain { get { return _definition.IsSidechain; } }
        public bool IsMainchain { get { return !_definition.IsSidechain; } }

        public Vector3 RawXyz { get => _xyz; set => _xyz = value; }

        public Vector3 Xyz
        {
            get
            {
                if(Parent == null)
                    return _xyz;

                Matrix totalParentTransform = Parent.TotalTransform;
                if(_cachedXyz != _xyz || _cachedTotalParentTransform != totalParentTransform)
                {
                    _cachedXyz = _xyz;
                    _cachedTotalParentTransform = totalParentTransform;
                    _cachedXyzPostTransform = Vector3.Transform(_cachedXyz, _cachedTotalParentTransform);
                }
                else
                {

                }
                return _cachedXyzPostTransform;                
            }

            set
            {
                if (Parent == null)
                {
                    _xyz = value;
                }
                else
                {
                    _xyz = Vector3.Transform(value, Matrix.Invert(Parent.TotalTransform));
                }
            }
        }

        public float X
        {
            get
            {
                return Xyz.X;
            }
            set
            {
                ITransformNode parent = Parent;
                if (Parent == null)
                {
                    _xyz.X = value;
                }
                else
                {
                    Vector3 xyz = Xyz;
                    xyz.X = value;
                    _xyz = Vector3.Transform(xyz, Matrix.Invert(parent.TotalTransform));
                }
            }
        }

        public float Y
        {
            get
            {
                return Xyz.Y;
            }
            set
            {
                if (Parent == null)
                {
                    _xyz.X = value;
                }
                else
                {
                    Vector3 xyz = Xyz;
                    xyz.Y = value;
                    _xyz = Vector3.Transform(xyz, Matrix.Invert(Parent.TotalTransform));
                }
            }
        }

        public float Z
        {
            get
            {
                return Xyz.Z;
            }
            set
            {
                if (Parent == null)
                {
                    _xyz.X = value;
                }
                else
                {
                    Vector3 xyz = Xyz;
                    xyz.Z = value;
                    _xyz = Vector3.Transform(xyz, Matrix.Invert(Parent.TotalTransform));
                }
            }
        }

        [JsonProperty]
        public ITransformNode Parent { get; set; }

        public void RotateNode(Quaternion rotation, Vector3 origin)
        {
            throw new InvalidOperationException("Nodes are an operational unit on XTree but not Tree. All tree manipulation must occur in the World coordinate system.");
        }

        public void Rotate(Quaternion rotation, Vector3 origin)
        {
            Trace.Assert(Parent == null);

            _xyz -= origin;
            _xyz = Vector3.Transform(_xyz, rotation);
            _xyz += origin;
        }

        public void TransformNode(Matrix transform)
        {
            throw new InvalidOperationException("Nodes are an operational unit on XTree but not Tree. All tree manipulation must occur in the World coordinate system.");
        }

        public void Transform(Matrix transform)
        {
            Trace.Assert(Parent == null);
            _xyz = Vector3.Transform(_xyz, transform);
        }

        public void TranslateNode(Vector3 translation)
        {
            throw new InvalidOperationException("Nodes are an operational unit on XTree but not Tree. All tree manipulation must occur in the World coordinate system.");
        }

        public void Translate(Vector3 translation)
        {
            Trace.Assert(Parent == null);
            _xyz += translation;
        }

        public IAtom GetMirroredElement(bool root, ITransformNode parent)
        {
            return new MirrorAtom(this, root, parent);
        }


        public IAtom GetMirrorTemplate()
        {
            return this;
        }

        public virtual object DeepCopy()
        {
            IDeepCloneObjectGraph context = null;
            return DeepCopyFindOrCreate(context);
        }

        public virtual object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph)
        {
            if (graph.TryGetClone(this, out object clone))
                return (IAtom) clone;

            Atom atom = new Atom();
            DeepCopyPopulateFields(graph, atom);
            return atom;
        }

        public virtual void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone)
        {
            Atom atom = (Atom) clone;
            atom._xyz = _xyz;
            atom._cachedXyz = _cachedXyz;
            atom._cachedXyzPostTransform = _cachedXyzPostTransform;
            atom._cachedTotalParentTransform = _cachedTotalParentTransform;
            atom.Parent = Parent == null ? null : (ITransformNode)Parent.DeepCopyFindOrCreate(graph);
            atom._definition = _definition;
        }
    }
}
