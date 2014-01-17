﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Happil.Fluent;

namespace Happil
{
	public static class TypeExtensions
	{
		public static object GetDefaultValue(this Type type)
		{
			if ( type.IsValueType )
			{
				return Activator.CreateInstance(type);
			}
			else
			{
				return null;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public static int GetIntegralTypeSize(this Type type)
		{
			if ( type.IsIntegralType() )
			{
				var underlyingType = (type.IsEnum ? type.GetEnumUnderlyingType() : type);

				switch ( Type.GetTypeCode(underlyingType) )
				{
					case TypeCode.Boolean:
						return sizeof(bool);
					case TypeCode.Char:
						return sizeof(char);
					case TypeCode.SByte:
						return sizeof(sbyte);
					case TypeCode.Byte:
						return sizeof(byte);
					case TypeCode.Int16:
						return sizeof(short);
					case TypeCode.UInt16:
						return sizeof(ushort);
					case TypeCode.Int32:
						return sizeof(int);
					case TypeCode.UInt32:
						return sizeof(uint);
					case TypeCode.Int64:
						return sizeof(long);
					case TypeCode.UInt64:
						return sizeof(ulong);
				}
			}

			throw new ArgumentException("Not an integral type.", paramName: "type");
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public static bool IsIntegralType(this Type type)
		{
			if ( type.IsEnum )
			{
				return true;
			}

			if ( !type.IsPrimitive )
			{
				return false;
			}

			switch ( Type.GetTypeCode(type) )
			{
				case TypeCode.Boolean:
				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return true;
				default:
					return false;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public static bool IsNullableValueType(this Type type)
		{
			return (
				type.IsValueType &&
				type.IsGenericType && 
				!type.IsGenericTypeDefinition && 
				type.GetGenericTypeDefinition() == typeof(Nullable<>));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public static Type[] GetTypeHierarchy(this Type type)
		{
			var baseTypes = new HashSet<Type>();
			GetAllBaseTypes(type, baseTypes);
			return baseTypes.ToArray();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private static void GetAllBaseTypes(Type currentType, HashSet<Type> baseTypes)
		{
			if ( baseTypes.Add(currentType) )
			{
				if ( currentType.IsClass && currentType.BaseType != null )
				{
					GetAllBaseTypes(currentType.BaseType, baseTypes);
				}
				
				foreach ( var baseInterface in currentType.GetInterfaces() )
				{
					GetAllBaseTypes(baseInterface, baseTypes);
				}
			}
		}
	}
}
