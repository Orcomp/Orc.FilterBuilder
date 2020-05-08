﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilterCustomizationService.cs" company="WildGums">
//   Copyright (c) 2008 - 2014 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FilterBuilder
{
    using System.Collections.Generic;
    using Catel;
    using System.Linq;

    public class FilterCustomizationService : IFilterCustomizationService
    {
        public virtual void CustomizeInstanceProperties(IPropertyCollection instanceProperties)
        {
            Argument.IsNotNull(() => instanceProperties);

            var catelProperties = new HashSet<string> {
                "BusinessRuleErrorCount",
                "BusinessRuleWarningCount",
                "FieldErrorCount",
                "FieldWarningCount",
                "HasErrors",
                "HasWarnings",
                "IsDirty",
                "IsEditable",
                "IsInEditSession",
                "IsReadOnly"
            };

            // Remove Catel properties
            instanceProperties.Properties.RemoveAll(x => catelProperties.Contains(x.Name));

            // Remove unsupported type properties
            instanceProperties.Properties.RemoveAll(x => !InstancePropertyHelper.IsSupportedType(x));
        }
    }
}
