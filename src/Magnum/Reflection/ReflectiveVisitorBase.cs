// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Magnum.Reflection
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using CollectionExtensions;
	using Collections;

	public abstract class ReflectiveVisitorBase<TVisitor>
		where TVisitor : class
	{
		private const BindingFlags _methodBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private static readonly Expression<Func<ReflectiveVisitorBase<TVisitor>, bool>> _defaultMemberName = x => x.Visit(null);
		private static readonly Dictionary<int, Func<TVisitor, object, bool>> _withArgs;
		private static readonly MultiDictionary<string, MethodInfo> MethodNameCache;
		private readonly string _methodName;

		static ReflectiveVisitorBase()
		{
			_withArgs = new Dictionary<int, Func<TVisitor, object, bool>>();

			MethodNameCache = new MultiDictionary<string, MethodInfo>(false);

			typeof (TVisitor).GetMethods(_methodBindingFlags).Each(method => MethodNameCache.Add(method.Name, method));
		}

		protected ReflectiveVisitorBase()
			: this("Visit")
		{
		}

		protected ReflectiveVisitorBase(Expression<Func<TVisitor, object>> expression)
			: this(expression.MemberName())
		{
		}

		protected ReflectiveVisitorBase(string methodName)
		{
			_methodName = methodName;
		}

		public virtual bool Visit(object obj)
		{
			return DispatchVisit(obj);
		}

		public virtual bool Visit(object obj, Func<bool> action)
		{
			bool result = DispatchVisit(obj);
			if (!result)
				return false;

			IncreaseDepth();
			result = action();
			DecreaseDepth();

			return result;
		}

		protected virtual void IncreaseDepth()
		{
		}

		protected virtual void DecreaseDepth()
		{
		}

		protected bool DispatchVisit(object obj)
		{
			var target = this as TVisitor;

			int key = obj.GetType().GetHashCode();

			Func<TVisitor, object, bool> invoker = GetInvoker(key, () =>
				{
					var args = new[] {obj};

					MethodInfo method = MethodNameCache[_methodName]
						.Where(x => x.ReturnType == typeof (bool))
						.MatchingArguments(args)
						.FirstOrDefault();

					if (method == null)
						return null;

					if (method.GetParameters().First().ParameterType == typeof (object))
						return null;

					return method.ToSpecializedMethod(args);
				});

			return invoker(target, obj);
		}

		private static Func<TVisitor, object, bool> GetInvoker(int key, Func<MethodInfo> getMethodInfo)
		{
			return _withArgs.Retrieve(key, () =>
				{
					MethodInfo method = getMethodInfo();
					if (method == null)
						return (x, y) => true;

					ParameterExpression instanceParameter = Expression.Parameter(typeof (TVisitor), "target");
					ParameterExpression argParameter = Expression.Parameter(typeof (object), "arg");

					ParameterInfo parameter = method.GetParameters().First();

					UnaryExpression arg = (parameter.ParameterType.IsValueType)
					                      	? Expression.Convert(argParameter, parameter.ParameterType)
					                      	: Expression.TypeAs(argParameter, parameter.ParameterType);

					MethodCallExpression call = Expression.Call(instanceParameter, method, arg);

					return Expression.Lambda<Func<TVisitor, object, bool>>(call, new[] {instanceParameter, argParameter}).Compile();
				});
		}
	}
}