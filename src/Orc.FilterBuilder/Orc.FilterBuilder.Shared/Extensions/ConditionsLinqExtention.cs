﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConditionExtensions.cs" company="WildGums">
//   Copyright (c) 2008 - 2015 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FilterBuilder.Conditions
{
    using Catel;
    using Orc.FilterBuilder.Models;
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public static class ConditionsLinqExtention
    {
        public static Expression<Func<T, bool>> MakeFunction<T>(this ConditionTreeItem conditionTreeItem)
        {
            var type = typeof(T);
            var pe = Expression.Parameter(type, "item");
            Expression expression = conditionTreeItem.MakeExpression(pe);
            if (expression == null)
            {
                return null;
            }
            var lambda = Expression.Lambda<Func<T, bool>>(expression, pe);
            return lambda;
        }

        private static Expression MakeExpression(this ConditionTreeItem conditionTreeItem, ParameterExpression parametr)
        {
            if (conditionTreeItem.GetType() == typeof(ConditionGroup))
            {
                return ((ConditionGroup)conditionTreeItem).MakeExpression(parametr);
            }
            if (conditionTreeItem.GetType() == typeof(PropertyExpression))
            {
                return ((PropertyExpression)conditionTreeItem).MakeExpression(parametr);
            }
            return null;
        }

        private static Expression MakeExpression(this ConditionGroup conditionGroup, ParameterExpression parametr)
        {
            if (!conditionGroup.Items.Any())
            {
                return null;
            }

            Expression final = null;
            Expression left = null;
            foreach (var item in conditionGroup.Items)
            {
                var curExp = item?.MakeExpression(parametr);
                if (curExp == null) { continue; }
                if (left == null)
                {
                    left = curExp;
                }
                else
                {
                    var rigth = curExp;
                    if (conditionGroup.Type == ConditionGroupType.And)
                    {
                        final = Expression.AndAlso(left, rigth);
                    }
                    else
                    {
                        final = Expression.OrElse(left, rigth);
                    }
                    left = final;
                }
            }

            return final ?? left;
        }

        private static Expression MakeExpression(this PropertyExpression propertyExpression, ParameterExpression pe)
        {
            return propertyExpression.DataTypeExpression.MakeExpression(pe, propertyExpression.Property);
        }

        private static Expression MakeExpression(this DataTypeExpression dataTypeExpression, ParameterExpression pe, IPropertyMetadata propertyMetadata)
        {

            if (dataTypeExpression.GetType() == typeof(BooleanExpression))
            {
                return ((BooleanExpression)dataTypeExpression).MakeExpression(pe, propertyMetadata.Name);
            }

            if (dataTypeExpression.GetType() == typeof(StringExpression))
            {
                return ((StringExpression)dataTypeExpression).MakeExpression(pe, propertyMetadata.Name);
            }

            if (
                    dataTypeExpression.GetType() == typeof(DateTimeExpression) ||
                    (
                        dataTypeExpression.GetType().BaseType.IsGenericType &&
                        dataTypeExpression.GetType().BaseType.GetGenericTypeDefinition() == typeof(NumericExpression<>)
                    ))
            {
                return dataTypeExpression.MakeNumericExpression(pe, propertyMetadata.Name);
            }

            return null;
        }

        private static Expression MakeNumericExpression(this DataTypeExpression expression, ParameterExpression pe, string propertyName)

        {

            var valueInfo = expression.GetType().GetProperty("Value");
            var value = valueInfo?.GetValue(expression);
            Expression nullExp;
            Expression e;
            switch (expression.SelectedCondition)
            {
                case Condition.EqualTo:
                    nullExp = IsNotNullExpression(pe, propertyName);
                    e = Expression.Equal(PropertyExpression(pe, propertyName), Expression.Constant(value));
                    return Expression.AndAlso(nullExp, e);
                case Condition.NotEqualTo:
                    nullExp = IsNullExpression(pe, propertyName);
                    e = Expression.NotEqual(PropertyExpression(pe, propertyName), Expression.Constant(value));
                    return Expression.OrElse(nullExp, e);
                case Condition.GreaterThan:
                    nullExp = IsNotNullExpression(pe, propertyName);
                    e = Expression.GreaterThan(PropertyExpression(pe, propertyName), Expression.Constant(value));
                    return Expression.AndAlso(nullExp, e);
                case Condition.GreaterThanOrEqualTo:
                    nullExp = IsNotNullExpression(pe, propertyName);
                    e = Expression.GreaterThanOrEqual(PropertyExpression(pe, propertyName), Expression.Constant(value));
                    return Expression.AndAlso(nullExp, e);
                case Condition.LessThan:
                    nullExp = IsNotNullExpression(pe, propertyName);
                    e = Expression.LessThan(PropertyExpression(pe, propertyName), Expression.Constant(value));
                    return Expression.AndAlso(nullExp, e);
                case Condition.LessThanOrEqualTo:
                    nullExp = IsNotNullExpression(pe, propertyName);
                    e = Expression.LessThanOrEqual(PropertyExpression(pe, propertyName), Expression.Constant(value));
                    return Expression.AndAlso(nullExp, e);
                case Condition.IsNull:
                    return IsNullExpression(pe, propertyName);
                case Condition.NotIsNull:
                    return IsNotNullExpression(pe, propertyName);
                default:
                    throw new NotSupportedException(string.Format(LanguageHelper.GetString("FilterBuilder_Exception_Message_ConditionIsNotSupported_Pattern"), expression.SelectedCondition));
            }
        }

        private static Expression MakeExpression(this BooleanExpression expression, ParameterExpression pe, string propertyName)
        {
            var Value = expression.Value;
            var SelectedCondition = expression.SelectedCondition;
            Expression notNullExp;
            Expression e;
            Expression final;
            switch (SelectedCondition)
            {
                case Condition.EqualTo:
                    notNullExp = Expression.Not(IsNullExpression(pe, propertyName));
                    e = Expression.Equal(PropertyExpression(pe, propertyName), Expression.Constant(Value));
                    final = Expression.AndAlso(notNullExp, e);
                    return final;
                default:
                    throw new NotSupportedException(string.Format(LanguageHelper.GetString("FilterBuilder_Exception_Message_ConditionIsNotSupported_Pattern"), SelectedCondition));
            }
        }

        private static Expression MakeExpression(this StringExpression expression, ParameterExpression pe, string propertyName)
        {
            var Value = expression.Value;
            var SelectedCondition = expression.SelectedCondition;

            Expression notNulExp;
            Expression left;
            Expression rigth;
            Expression e;
            Expression final;
            switch (SelectedCondition)
            {
                case Condition.Contains:
                    notNulExp = NotIsNullOrEmptyExpression(pe, propertyName);
                    left = Expression.Property(pe, propertyName);
                    rigth = Expression.Constant(Value);
                    e = Expression.Call(left, typeof(string).GetMethod("Contains", new Type[] { typeof(string) }), rigth);
                    final = Expression.AndAlso(notNulExp, e);
                    return final;
                case Condition.DoesNotContain:
                    e = (new StringExpression()
                    {
                        SelectedCondition = Condition.Contains,
                        Value = expression.Value
                    }).MakeExpression(pe, propertyName);
                    return Expression.Not(e);
                case Condition.StartsWith:
                    notNulExp = NotIsNullOrEmptyExpression(pe, propertyName);
                    left = Expression.Property(pe, propertyName);
                    rigth = Expression.Constant(Value);
                    e = Expression.Call(left, typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) }), rigth);
                    final = Expression.AndAlso(notNulExp, e);
                    return final;
                case Condition.DoesNotStartWith:
                    e = (new StringExpression()
                    {
                        SelectedCondition = Condition.StartsWith,
                        Value = expression.Value
                    }).MakeExpression(pe, propertyName);
                    return Expression.Not(e);
                case Condition.EndsWith:
                    notNulExp = NotIsNullOrEmptyExpression(pe, propertyName);
                    left = Expression.Property(pe, propertyName);
                    rigth = Expression.Constant(Value);
                    e = Expression.Call(left, typeof(string).GetMethod("EndsWith", new Type[] { typeof(string) }), rigth);
                    final = Expression.AndAlso(notNulExp, e);
                    return final;
                case Condition.DoesNotEndWith:
                    e = (new StringExpression()
                    {
                        SelectedCondition = Condition.EndsWith,
                        Value = expression.Value
                    }).MakeExpression(pe, propertyName);
                    return Expression.Not(e);
                case Condition.EqualTo:
                    notNulExp = NotIsNullOrEmptyExpression(pe, propertyName);
                    e = Expression.Equal(Expression.Property(pe, propertyName), Expression.Constant(Value));
                    return Expression.AndAlso(notNulExp, e);
                case Condition.NotEqualTo:
                    e = (new StringExpression()
                    {
                        SelectedCondition = Condition.EqualTo,
                        Value = expression.Value
                    }).MakeExpression(pe, propertyName);
                    return Expression.Not(e);
                case Condition.GreaterThan:
                    e = CompareStringExpression(pe, propertyName, expression.Value);
                    return Expression.GreaterThan(e, Expression.Constant(0));
                case Condition.GreaterThanOrEqualTo:
                    e = CompareStringExpression(pe, propertyName, expression.Value);
                    return Expression.GreaterThanOrEqual(e, Expression.Constant(0));
                case Condition.LessThan:
                    e = CompareStringExpression(pe, propertyName, expression.Value);
                    return Expression.LessThan(e, Expression.Constant(0));
                case Condition.LessThanOrEqualTo:
                    e = CompareStringExpression(pe, propertyName, expression.Value);
                    return Expression.LessThanOrEqual(e, Expression.Constant(0));
                case Condition.IsNull:
                    return Expression.Equal(Expression.Property(pe, propertyName), Expression.Constant(null));
                case Condition.NotIsNull:
                    e = Expression.Equal(Expression.Property(pe, propertyName), Expression.Constant(null));
                    return Expression.Not(e);
                case Condition.IsEmpty:
                    e = Expression.Equal(Expression.Property(pe, propertyName), Expression.Constant(string.Empty));
                    return e;
                case Condition.NotIsEmpty:
                    e = Expression.Equal(Expression.Property(pe, propertyName), Expression.Constant(string.Empty));
                    final = Expression.Not(e);
                    return final;
                default:
                    throw new NotSupportedException(string.Format(LanguageHelper.GetString("FilterBuilder_Exception_Message_ConditionIsNotSupported_Pattern"), expression.SelectedCondition));
            }
        }

        private static Expression CompareStringExpression(ParameterExpression pe, string propertyName, string value)
        {
            var arg0 = Expression.Property(pe, propertyName);
            var arg1 = Expression.Constant(value);
            var method = typeof(string).GetMethod("Compare", new Type[] { typeof(string), typeof(string), typeof(StringComparison) });
            return Expression.Call(method, arg0, arg1, Expression.Constant(StringComparison.InvariantCultureIgnoreCase));
        }

        private static Expression IsNullOrEmptyExpression(ParameterExpression pe, string propertyName)
        {
            var method = typeof(string).GetMethod("IsNullOrEmpty", new Type[] { typeof(string) });
            Expression arg = Expression.Property(pe, propertyName);
            return Expression.Call(method, arg);
        }

        private static Expression NotIsNullOrEmptyExpression(ParameterExpression pe, string propertyName)
        {
            Expression e = IsNullOrEmptyExpression(pe, propertyName);
            return Expression.Not(e);
        }

        private static Expression IsNullExpression(ParameterExpression pe, string propertyName)
        {
            var type = Expression.Property(pe, propertyName).Type;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return Expression.Equal(Expression.Property(pe, propertyName), Expression.Constant(null));
            }
            else
            {
                return Expression.Constant(false);
            }
        }
        private static Expression IsNotNullExpression(ParameterExpression pe, string propertyName)
        {
            var type = Expression.Property(pe, propertyName).Type;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return Expression.NotEqual(Expression.Property(pe, propertyName), Expression.Constant(null));
            }
            else
            {
                return Expression.Constant(true);
            }
        }

        private static Expression PropertyExpression(ParameterExpression pe, string propertyName)
        {
            var type = Expression.Property(pe, propertyName).Type;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var prop = Expression.Property(pe, propertyName);
                return Expression.Property(prop, "Value");
            }
            else
            {
                return Expression.Property(pe, propertyName);
            }
        }
    }
}