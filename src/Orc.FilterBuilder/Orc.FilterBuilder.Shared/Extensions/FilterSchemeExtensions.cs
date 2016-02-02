﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilterSchemeExtensions.cs" company="Orcomp development team">
//   Copyright (c) 2008 - 2014 Orcomp development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FilterBuilder
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using Catel;
    using Catel.Collections;
    using Catel.IoC;
    using Catel.Reflection;
    using Catel.Threading;
    using MethodTimer;
    using Models;
    using Services;

    public static class FilterSchemeExtensions
    {
        private const string Separator = "||";

        private static readonly IReflectionService _reflectionService = ServiceLocator.Default.ResolveType<IReflectionService>();
        private static readonly AsyncLock LockFilterScheme = new AsyncLock();
        private static readonly AsyncLock LockPropertyExpression = new AsyncLock();

        public static async Task EnsureIntegrityAsync(this FilterScheme filterScheme)
        {
            Argument.IsNotNull(() => filterScheme);

            var reflectionService = ServiceLocator.Default.ResolveType<IReflectionService>(filterScheme.Tag);

            using (await LockFilterScheme.LockAsync())
            {
                foreach (var item in filterScheme.ConditionItems)
                {
                    await item.EnsureIntegrityAsync(reflectionService);
                }
            }
        }

        public static void EnsureIntegrity(this FilterScheme filterScheme)
        {
            Argument.IsNotNull(() => filterScheme);

            foreach (var item in filterScheme.ConditionItems)
            {
                item.EnsureIntegrity();
            }
        }

        public static async Task EnsureIntegrityAsync(this ConditionTreeItem conditionTreeItem, IReflectionService reflectionService = null)
        {
            Argument.IsNotNull(() => conditionTreeItem);

            var propertyExpression = conditionTreeItem as PropertyExpression;
            if (propertyExpression != null)
            {
                await propertyExpression.EnsureIntegrityAsync(reflectionService);
            }

            foreach (var item in conditionTreeItem.Items)
            {
                await item.EnsureIntegrityAsync(reflectionService);
            }
        }

        public static void EnsureIntegrity(this ConditionTreeItem conditionTreeItem)
        {
            Argument.IsNotNull(() => conditionTreeItem);

            var propertyExpression = conditionTreeItem as PropertyExpression;
            if (propertyExpression != null)
            {
                propertyExpression.EnsureIntegrity();
            }

            foreach (var item in conditionTreeItem.Items)
            {
                item.EnsureIntegrity();
            }
        }

        public static async Task EnsureIntegrityAsync(this PropertyExpression propertyExpression, IReflectionService reflectionService = null)
        {
            Argument.IsNotNull(() => propertyExpression);

            if (reflectionService == null)
            {
                reflectionService = _reflectionService;
            }

            if (propertyExpression.Property == null)
            {
                using (await LockPropertyExpression.LockAsync())
                {
                    var serializationValue = propertyExpression.PropertySerializationValue;
                    if (!string.IsNullOrWhiteSpace(serializationValue))
                    {
                        var splittedString = serializationValue.Split(new[] {Separator}, StringSplitOptions.RemoveEmptyEntries);
                        if (splittedString.Length == 2)
                        {
                            var type = TypeCache.GetType(splittedString[0]);
                            if (type != null)
                            {
                                var typeProperties = await reflectionService.GetInstancePropertiesAsync(type);
                                propertyExpression.Property = typeProperties.GetProperty(splittedString[1]);
                            }
                        }
                    }
                }
            }
        }

        public static void EnsureIntegrity(this PropertyExpression propertyExpression)
        {
            Argument.IsNotNull(() => propertyExpression);

            if (propertyExpression.Property == null)
            {
                var serializationValue = propertyExpression.PropertySerializationValue;
                if (!string.IsNullOrWhiteSpace(serializationValue))
                {
                    var splittedString = serializationValue.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
                    if (splittedString.Length == 2)
                    {
                        var type = TypeCache.GetType(splittedString[0]);
                        if (type != null)
                        {
                            var typeProperties = _reflectionService.GetInstanceProperties(type);
                            propertyExpression.Property = typeProperties.GetProperty(splittedString[1]);
                        }
                    }
                }
            }
        }

        [Time]
        public static void Apply(this FilterScheme filterScheme, IEnumerable rawCollection, IList filteredCollection)
        {
            Argument.IsNotNull(() => filterScheme);
            Argument.IsNotNull(() => rawCollection);
            Argument.IsNotNull(() => filteredCollection);

            IDisposable suspendToken = null;
            var filteredCollectionType = filteredCollection.GetType();
            if (filteredCollectionType.IsGenericTypeEx() && filteredCollectionType.GetGenericTypeDefinitionEx() == typeof(FastObservableCollection<>))
            {
                suspendToken = (IDisposable)filteredCollectionType.GetMethodEx("SuspendChangeNotifications").Invoke(filteredCollection, null);
            }

            filteredCollection.Clear();

            foreach (var item in rawCollection)
            {
                if (filterScheme.CalculateResult(item))
                {
                    filteredCollection.Add(item);
                }
            }

            if (suspendToken != null)
            {
                suspendToken.Dispose();
            }
        }
    }
}