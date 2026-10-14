#nullable enable

using System;
using System.Reflection;
using UnityEngine;
using Oculus.Avatar2;

namespace VaSiLi.MetaAvatar
{
    /// <summary>
    /// Decorator face pose provider that pulls the live tracked face pose from an
    /// underlying SDK provider and multiplies the expression blendshape weights by a
    /// configurable factor to exaggerate facial expressions.
    ///
    /// Expression confidences are passed through untouched. Weights are clamped to the
    /// SDK's valid [0, 1] range after scaling so the exaggeration cannot push a blendshape
    /// out of range.
    /// </summary>
    public sealed class ExaggeratedFacePoseProvider : OvrAvatarFacePoseProviderBase
    {
        private readonly OvrAvatarFacePoseProviderBase _source;
        private readonly OvrAvatarFacePose _sourcePose = new OvrAvatarFacePose();
        private readonly Func<OvrAvatarFacePose, bool> _sourceGetFacePose;

        /// <summary>
        /// Multiplier applied to every expression weight. 1.0 is neutral (no change),
        /// values above 1.0 exaggerate expressions.
        /// </summary>
        public float ExpressionMultiplier { get; set; }

        public ExaggeratedFacePoseProvider(OvrAvatarFacePoseProviderBase source, float expressionMultiplier = 1.5f)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            ExpressionMultiplier = expressionMultiplier;

            // GetFacePose is protected on the SDK base class, so the live tracking provider's
            // implementation can't be called directly. Bind to it once via reflection and reuse
            // the SDK's own native marshaling instead of re-implementing the P/Invoke.
            MethodInfo? method = typeof(OvrAvatarFacePoseProviderBase).GetMethod(
                "GetFacePose",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (method == null)
            {
                throw new InvalidOperationException(
                    "Could not bind to OvrAvatarFacePoseProviderBase.GetFacePose; the Avatars SDK API may have changed.");
            }

            _sourceGetFacePose =
                (Func<OvrAvatarFacePose, bool>)Delegate.CreateDelegate(
                    typeof(Func<OvrAvatarFacePose, bool>), _source, method);
        }

        protected override bool GetFacePose(OvrAvatarFacePose facePose)
        {
            if (!_sourceGetFacePose(_sourcePose))
            {
                return false;
            }

            float multiplier = ExpressionMultiplier;
            float[] sourceWeights = _sourcePose.expressionWeights;
            float[] sourceConfidence = _sourcePose.expressionConfidence;
            float[] outWeights = facePose.expressionWeights;
            float[] outConfidence = facePose.expressionConfidence;

            for (int i = 0; i < outWeights.Length; i++)
            {
                outWeights[i] = Mathf.Clamp01(sourceWeights[i] * multiplier);
            }

            for (int i = 0; i < outConfidence.Length; i++)
            {
                outConfidence[i] = sourceConfidence[i];
            }

            facePose.sampleTimeNS = _sourcePose.sampleTimeNS;
            return true;
        }
    }

    /// <summary>
    /// MonoBehaviour wrapper so an <see cref="ExaggeratedFacePoseProvider"/> can be passed to
    /// <c>OvrAvatarEntity.SetFacePoseProvider</c>, which expects an <see cref="OvrAvatarFacePoseBehavior"/>.
    /// </summary>
    public sealed class ExaggeratedFacePoseBehavior : OvrAvatarFacePoseBehavior
    {
        private ExaggeratedFacePoseProvider? _provider;

        public override OvrAvatarFacePoseProviderBase FacePoseProvider => _provider!;

        /// <summary>
        /// Wraps the given source behavior's provider with an exaggeration decorator.
        /// </summary>
        /// <returns>True if a source provider was available and the decorator was created.</returns>
        public bool Initialize(OvrAvatarFacePoseBehavior source, float expressionMultiplier)
        {
            OvrAvatarFacePoseProviderBase? sourceProvider = source != null ? source.FacePoseProvider : null;
            if (sourceProvider == null)
            {
                return false;
            }

            _provider = new ExaggeratedFacePoseProvider(sourceProvider, expressionMultiplier);
            return true;
        }

        /// <summary>
        /// Adjusts the exaggeration multiplier at runtime.
        /// </summary>
        public void SetExpressionMultiplier(float multiplier)
        {
            if (_provider != null)
            {
                _provider.ExpressionMultiplier = multiplier;
            }
        }
    }
}
