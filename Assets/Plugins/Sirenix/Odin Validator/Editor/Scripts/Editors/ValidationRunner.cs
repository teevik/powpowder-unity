//-----------------------------------------------------------------------
// <copyright file="ValidationRunner.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinValidator.Editor
{
    using Sirenix.OdinInspector.Editor;
    using Sirenix.OdinInspector.Editor.Validation;
    using System;
    using System.Collections.Generic;

    public class ValidationRunner : IDisposable
    {
        private Dictionary<Type, PropertyTree> cachedPropertyTrees;

        public void Dispose()
        {
            if (this.cachedPropertyTrees != null)
            {
                foreach (var tree in this.cachedPropertyTrees.Values)
                {
                    tree.Dispose();
                }

                this.cachedPropertyTrees = null;
            }
        }

        public virtual List<ValidationResult> ValidateObjectRecursively(object value)
        {
            List<ValidationResult> results = new List<ValidationResult>();
            this.ValidateObjectRecursively(value, ref results);
            return results;
        }

        public virtual void ValidateObjectRecursively(object value, ref List<ValidationResult> results)
        {
            if (results == null) results = new List<ValidationResult>();

            PropertyTree tree;

            if (this.cachedPropertyTrees == null)
            {
                this.cachedPropertyTrees = new Dictionary<Type, PropertyTree>(FastTypeComparer.Instance);
            }

            if (!this.cachedPropertyTrees.TryGetValue(value.GetType(), out tree))
            {
                tree = PropertyTree.Create(value).SetUpForValidation();
                this.cachedPropertyTrees.Add(value.GetType(), tree);
            }
            else
            {
                tree.SetTargets(value);
                tree.SetUpForValidation();
            }

            try
            {
                {
                    var root = tree.RootProperty;

                    var validationComponent = root.GetComponent<ValidationComponent>();

                    if (validationComponent != null && validationComponent.ValidatorLocator.PotentiallyHasValidatorsFor(root))
                    {
                        validationComponent.ValidateProperty(ref results);
                    }
                }

                foreach (var property in tree.EnumerateTree(true, true))
                {
                    var validationComponent = property.GetComponent<ValidationComponent>();

                    if (validationComponent == null) continue;
                    if (!validationComponent.ValidatorLocator.PotentiallyHasValidatorsFor(property)) continue;

                    validationComponent.ValidateProperty(ref results);
                }
            }
            finally
            {
                tree.CleanForCachedReuse();
            }
        }

        public virtual List<ValidationResult> ValidateUnityObjectRecursively(UnityEngine.Object value)
        {
            List<ValidationResult> results = new List<ValidationResult>();
            this.ValidateObjectRecursively(value, ref results);
            return results;
        }

        public virtual void ValidateUnityObjectRecursively(UnityEngine.Object value, ref List<ValidationResult> results)
        {
            this.ValidateObjectRecursively(value, ref results);
        }
    }
}
