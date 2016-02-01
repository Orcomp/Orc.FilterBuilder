﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyMetadata.cs" company="Orcomp development team">
//     Copyright (c) 2008 - 2014 Orcomp development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Orc.FilterBuilder.Models
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using Catel;
    using Catel.Data;

    [DebuggerDisplay("{OwnerType}.{Name}")]
    public class PropertyMetadata : IPropertyMetadata
    {
        private readonly PropertyData _propertyData;
        private readonly PropertyInfo _propertyInfo;
        private string _displayName;

        #region Constructors
        public PropertyMetadata(Type ownerType, PropertyInfo propertyInfo)
        {
            Argument.IsNotNull(() => ownerType);
            Argument.IsNotNull(() => propertyInfo);

            _propertyInfo = propertyInfo;

            OwnerType = ownerType;
            Name = propertyInfo.Name;
            DisplayName = propertyInfo.GetDisplayName() ?? Name;
            Type = propertyInfo.PropertyType;
        }

        public PropertyMetadata(Type ownerType, PropertyData propertyData)
        {
            Argument.IsNotNull(() => ownerType);
            Argument.IsNotNull(() => propertyData);

            _propertyData = propertyData;

            OwnerType = ownerType;
            Name = propertyData.Name;
            DisplayName = ownerType.GetProperty(Name).GetDisplayName() ?? Name;
            Type = propertyData.Type;
        }
        #endregion Constructors

        #region Properties
        public string DisplayName
        {
            get
            {
                if (_displayName!=null)
                {
                    return _displayName; 
                }
                return Name;
            }
            set { _displayName = value; }
        }

        public string Name { get; private set; }

        public Type OwnerType { get; private set; }

        public Type Type { get; private set; }
        #endregion Properties

        #region Methods
        public override bool Equals(object obj)
        {
            var propertyMetaData = obj as PropertyMetadata;
            if (propertyMetaData == null)
            {
                return false;
            }

            if (!string.Equals(Name, propertyMetaData.Name))
            {
                return false;
            }

            if (Type != propertyMetaData.Type)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public object GetValue(object instance)
        {
            return GetValue<object>(instance);
        }

        public TValue GetValue<TValue>(object instance)
        {
            Argument.IsNotNull(() => instance);

            if (_propertyInfo != null)
            {
                return (TValue)_propertyInfo.GetValue(instance, null);
            }

            if (_propertyData != null)
            {
                var modelEditor = instance as IModelEditor;
                if (modelEditor != null)
                {
                    return modelEditor.GetValue<TValue>(_propertyData.Name);
                }
            }

            return default(TValue);
        }

        public void SetValue(object instance, object value)
        {
            Argument.IsNotNull(() => instance);

            if (_propertyInfo != null)
            {
                _propertyInfo.SetValue(instance, value, null);
            }

            if (_propertyData != null)
            {
                var modelEditor = instance as IModelEditor;
                if (modelEditor != null)
                {
                    modelEditor.SetValue(_propertyData.Name, value);
                }
            }
        }
        #endregion Methods
    }
}