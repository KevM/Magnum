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
	using System.Linq.Expressions;
	using System.Reflection;

	public class FastProperty
	{
		public Func<object, object> GetDelegate;
		public Action<object, object> SetDelegate;

		public FastProperty(PropertyInfo property)
		{
			Property = property;
			GetDelegate = InitializeGet(Property);
			SetDelegate = InitializeSet(Property, false);
		}

		public FastProperty(PropertyInfo property, BindingFlags bindingFlags)
		{
			Property = property;
			GetDelegate = InitializeGet(Property);
			SetDelegate = InitializeSet(Property, (bindingFlags & BindingFlags.NonPublic) == BindingFlags.NonPublic);
		}

		public PropertyInfo Property { get; set; }

		public object Get(object instance)
		{
			return GetDelegate(instance);
		}

		public void Set(object instance, object value)
		{
			SetDelegate(instance, value);
		}

		private static Action<object, object> InitializeSet(PropertyInfo property, bool includeNonPublic)
		{
			var instance = Expression.Parameter(typeof (object), "instance");
			var value = Expression.Parameter(typeof (object), "value");

			// value as T is slightly faster than (T)value, so if it's not a value type, use that
			UnaryExpression instanceCast;
			if (property.DeclaringType.IsValueType)
				instanceCast = Expression.Convert(instance, property.DeclaringType);
			else
				instanceCast = Expression.TypeAs(instance, property.DeclaringType);
			
			UnaryExpression valueCast;
			if (property.PropertyType.IsValueType)
				valueCast = Expression.Convert(value, property.PropertyType);
			else
				valueCast = Expression.TypeAs(value, property.PropertyType);

			var call = Expression.Call(instanceCast, property.GetSetMethod(includeNonPublic), valueCast);

			return Expression.Lambda<Action<object, object>>(call, new[] {instance, value}).Compile();
		}

		private static Func<object, object> InitializeGet(PropertyInfo property)
		{
			var instance = Expression.Parameter(typeof (object), "instance");
			UnaryExpression instanceCast;
			if (property.DeclaringType.IsValueType)
				instanceCast = Expression.Convert(instance, property.DeclaringType);
			else
				instanceCast = Expression.TypeAs(instance, property.DeclaringType);

			var call = Expression.Call(instanceCast, property.GetGetMethod());
			var typeAs = Expression.TypeAs(call, typeof (object));

			return Expression.Lambda<Func<object, object>>(typeAs, instance).Compile();
		}
	}

	public class FastProperty<T>
	{
		public Func<T, object> GetDelegate;
		public Action<T, object> SetDelegate;

		public FastProperty(PropertyInfo property)
		{
			Property = property;
			GetDelegate = InitializeGet(Property);
			SetDelegate = InitializeSet(Property, false);
		}

		public FastProperty(PropertyInfo property, BindingFlags bindingFlags)
		{
			Property = property;
			GetDelegate = InitializeGet(Property);
			SetDelegate = InitializeSet(Property, (bindingFlags & BindingFlags.NonPublic) == BindingFlags.NonPublic);
		}

		public PropertyInfo Property { get; set; }

		public object Get(T instance)
		{
			return GetDelegate(instance);
		}

		public void Set(T instance, object value)
		{
			SetDelegate(instance, value);
		}

		private static Action<T, object> InitializeSet(PropertyInfo property, bool includeNonPublic)
		{
			var instance = Expression.Parameter(typeof (T), "instance");
			var value = Expression.Parameter(typeof (object), "value");
			UnaryExpression valueCast;
			if (property.PropertyType.IsValueType)
				valueCast = Expression.Convert(value, property.PropertyType);
			else
				valueCast = Expression.TypeAs(value, property.PropertyType);

			var call = Expression.Call(instance, property.GetSetMethod(includeNonPublic), valueCast);

			return Expression.Lambda<Action<T, object>>(call, new[] {instance, value}).Compile();
		}

		private static Func<T, object> InitializeGet(PropertyInfo property)
		{
			var instance = Expression.Parameter(typeof (T), "instance");
			var call = Expression.Call(instance, property.GetGetMethod());
			var typeAs = Expression.TypeAs(call, typeof (object));
			return Expression.Lambda<Func<T, object>>(typeAs, instance).Compile();
		}
	}

	public class FastProperty<T, P>
	{
		public Func<T, P> GetDelegate;
		public Action<T, P> SetDelegate;

		public FastProperty(PropertyInfo property)
		{
			Property = property;
			GetDelegate = InitializeGet(Property);
			SetDelegate = InitializeSet(Property, false);
		}

		public FastProperty(PropertyInfo property, BindingFlags bindingFlags)
		{
			Property = property;
			GetDelegate = InitializeGet(Property);
			SetDelegate = InitializeSet(Property, (bindingFlags & BindingFlags.NonPublic) == BindingFlags.NonPublic);
		}

		public PropertyInfo Property { get; set; }

		public P Get(T instance)
		{
			return GetDelegate(instance);
		}

		public void Set(T instance, P value)
		{
			SetDelegate(instance, value);
		}

		private static Action<T, P> InitializeSet(PropertyInfo property, bool includeNonPublic)
		{
			var instance = Expression.Parameter(typeof (T), "instance");
			var value = Expression.Parameter(typeof (P), "value");
			var call = Expression.Call(instance, property.GetSetMethod(includeNonPublic), value);

			return Expression.Lambda<Action<T, P>>(call, new[] {instance, value}).Compile();

			// roughly looks like Action<T,P> a = new Action<T,P>((instance,value) => instance.set_Property(value));
		}

		private static Func<T, P> InitializeGet(PropertyInfo property)
		{
			var instance = Expression.Parameter(typeof (T), "instance");
			return Expression.Lambda<Func<T, P>>(Expression.Call(instance, property.GetGetMethod()), instance).Compile();

			// roughly looks like Func<T,P> getter = instance => return instance.get_Property();
		}
	}
}