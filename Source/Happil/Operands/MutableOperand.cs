﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Happil.Expressions;

namespace Happil.Operands
{
	public abstract class MutableOperand<T> : Operand<T>
	{
		internal MutableOperand()
		{
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public Operand<T> Assign(T value)
		{
			return Assign(new ConstantOperand<T>(value));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public Operand<T> Assign(IOperand<T> value)
		{
			if ( OperandType.IsValueType && value is IValueTypeInitializer && this is ICanEmitAddress )
			{
				((IValueTypeInitializer)value).Target = this;
				return (Operand<T>)value;
			}
			else
			{
				return new BinaryExpressionOperand<T, T>(
					@operator: new BinaryOperators.OperatorAssign<T>(),
					left: this,
					right: value.OrNullConstant());
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		//TODO:redesign
		//public Operand<T> AssignConst(T constantValue)
		//{
		//	return new HappilBinaryExpression<T, T>(
		//		base.OwnerMethod,
		//		@operator: new BinaryOperators.OperatorAssign<T>(),
		//		left: this,
		//		right: new HappilConstant<T>(constantValue));
		//}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public Operand<T> PostfixPlusPlus()
		{
			return new UnaryExpressionOperand<T, T>(
				@operator: new UnaryOperators.OperatorIncrement<T>(UnaryOperatorPosition.Postfix, positive: true),
				operand: this,
				position: UnaryOperatorPosition.Postfix);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public Operand<T> PostfixMinusMinus()
		{
			return new UnaryExpressionOperand<T, T>(
				@operator: new UnaryOperators.OperatorIncrement<T>(UnaryOperatorPosition.Postfix, positive: false),
				operand: this,
				position: UnaryOperatorPosition.Postfix);
		}
	}
}
