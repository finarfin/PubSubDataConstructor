﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace PubSubDataConstructor
{
    public class FluentPublisher<TData, TTarget>
    {
        public class MapBuilder<TDataProperty, TTargetProperty>
        {
            private readonly Expression<Func<TData, TDataProperty>> sourceExpr;
            private readonly Expression<Func<TTarget, TTargetProperty>> targetExpr;
            private readonly FluentPublisher<TData, TTarget> publisher;
            private Func<TData, DateTime> freshnessExpr;
            private Func<TData, byte> priorityExpr = _ => 10;

            public MapBuilder(FluentPublisher<TData, TTarget> publisher, Expression<Func<TData, TDataProperty>> dataExpr, Expression<Func<TTarget, TTargetProperty>> targetExpr)
            {
                if (publisher == null)
                    throw new ArgumentNullException("publisher");

                if (dataExpr == null)
                    throw new ArgumentNullException("dataExpr");

                if (targetExpr == null)
                    throw new ArgumentNullException("targetExpr");

                this.publisher = publisher;
                this.sourceExpr = dataExpr;
                this.targetExpr = targetExpr;
            }

            public MapBuilder<TDataProperty, TTargetProperty> HighPriority()
            {
                return Priority(255);
            }

            public MapBuilder<TDataProperty, TTargetProperty> LowPriority()
            {
                return Priority(10);
            }

            public MapBuilder<TDataProperty, TTargetProperty> Priority(byte value)
            {
                return Priority(x => value);
            }

            public MapBuilder<TDataProperty, TTargetProperty> Priority(Func<TData, byte> expression)
            {
                if (expression == null)
                    throw new ArgumentNullException("expression");

                this.priorityExpr = expression;

                return this;
            }

            public MapBuilder<TDataProperty, TTargetProperty> Freshness(Func<TData, DateTime> expression)
            {
                if (expression == null)
                    throw new ArgumentNullException("expression");

                this.freshnessExpr = expression;

                return this;
            }

            internal Func<TData, DataCandidate> Compile()
            {
                var sourceField = ExpressionHelper.GetPropertyName(sourceExpr.Body);
                var targetField = ExpressionHelper.GetPropertyName(targetExpr.Body);

                return x => new DataCandidate
                {
                    SourceField = sourceField,
                    SourceId = publisher.sourceIdExpression.Invoke(x),
                    TargetType = typeof(TTarget).Name,
                    TargetId = publisher.targetIdExpression.Invoke(x),
                    TargetField = targetField,
                    Priority = priorityExpr.Invoke(x),
                    Freshness = (freshnessExpr ?? publisher.freshnessExpression ?? (_ => DateTime.Now)).Invoke(x),
                    Value = sourceExpr.Compile().Invoke(x)
                };
            }

        }

        private readonly List<Func<TData, DataCandidate>> mappings;
        private Func<TData, string> targetIdExpression;
        private Func<TData, string> sourceIdExpression;
        private Func<TData, DateTime> freshnessExpression;

        public FluentPublisher()
        {
            this.mappings = new List<Func<TData, DataCandidate>>();
            this.sourceIdExpression = _ => GetType().Name;
        }

        public void Freshness(Func<TData, DateTime> expression)
        {
            freshnessExpression = expression;
        }

        public void Target(Func<TData, string> expression)
        {
            targetIdExpression = expression;
        }

        public void Source(Func<TData, string> expression)
        {
            sourceIdExpression = expression;
        }

        public void Source(string value)
        {
            sourceIdExpression = _ => value;
        }

        public void Id(Func<TData, string> expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            Target(expression);
        }

        public MapBuilder<TDataProperty, TTargetProperty> Map<TDataProperty, TTargetProperty>(Expression<Func<TData, TDataProperty>> dataExpr, Expression<Func<TTarget, TTargetProperty>> assetExpr)
        {
            var builder = new MapBuilder<TDataProperty, TTargetProperty>(this, dataExpr, assetExpr);
            mappings.Add(builder.Compile());
            return builder;
        }

        public void Publish(IClient client, object obj)
        {
            Publish(client, (TData)obj);
        }

        public void Publish(IClient client, TData data)
        {
            client.Publish(mappings.Select(x => x.Invoke(data)));
        }
    }
}
