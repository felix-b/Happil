﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Hapil.Applied.ApiContracts.Impl;
using Hapil.Operands;
using Hapil.Writers;

namespace Hapil.Applied.ApiContracts
{
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
	public class ItemsNotNullAttribute : ApiCheckAttribute
	{
		public override void ContributeChecks(ICustomAttributeProvider info, ApiMemberDescription member)
		{
			var parameterInfo = (ParameterInfo)info;
			var parameterType = parameterInfo.ParameterType.UnderlyingType();

			if ( typeof(System.Collections.IEnumerable).IsAssignableFrom(parameterType) )
			{
				member.AddCheck(new CollectionItemsNotNullCheckWriter(parameterInfo));
			}
			else
			{
				throw new NotSupportedException(string.Format("ItemsNotNull is not supported on parameter of type [{0}].", parameterType.FullName));
			}
		}
		
		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private class CollectionItemsNotNullCheckWriter : ApiArgumentCheckWriter, ICheckCollectionTypes
		{
			public CollectionItemsNotNullCheckWriter(ParameterInfo parameterInfo)
				: base(parameterInfo)
			{
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			protected override void OnWriteArgumentCheck(MethodWriterBase writer, Operand<TypeTemplate.TArgument> argument, bool isOutput)
			{
				Static.Void(ApiContract.ItemsNotNull, argument.CastTo<System.Collections.IEnumerable>(), writer.Const(ParameterName), writer.Const(isOutput));
			}
		}
	}
}
