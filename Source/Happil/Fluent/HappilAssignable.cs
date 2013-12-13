﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Happil.Expressions;

namespace Happil.Fluent
{
	public abstract class HappilAssignable<T> : HappilOperand<T>
	{
		internal HappilAssignable(HappilMethod ownerMethod)
			: base(ownerMethod)
		{
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<T> Assign(HappilOperand<T> operand)
		{
			throw new NotImplementedException();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------
		
		public HappilOperand<T> Assign(HappilConstant<T> operand)
		{
			throw new NotImplementedException();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<T> PostfixPlusPlus()
		{
			return new HappilUnaryExpression<T, T>(
				base.OwnerMethod,
				@operator: new UnaryOperators.OperatorPlusPlus<T>(),
				operand: this,
				position: UnaryOperatorPosition.Postfix);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<T> PostfixMinusMinus()
		{
			return new HappilUnaryExpression<T, T>(
				base.OwnerMethod,
				@operator: new UnaryOperators.OperatorMinusMinus<T>(),
				operand: this,
				position: UnaryOperatorPosition.Postfix);
		}
	}
}
