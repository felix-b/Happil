﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Happil.Fluent
{
	public interface IHappilMethodBodyBase
	{
		HappilLocal<T> Local<T>(string name);
		HappilLocal<T> Local<T>(string name, HappilOperand<T> initialValue);
		void Throw<TException>(string message) where TException : Exception;
		void EmitFromLambda(Expression<Action> lambda);
		HappilArgument<T> Argument<T>(string name);
		HappilArgument<T> Argument<T>(int index);
		MethodInfo MethodInfo { get; }
		int ArgumentCount { get; }
		Type ReturnType { get; }
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------

	public interface IHappilMethodBody : IHappilMethodBodyBase
	{
		void Return(IHappilOperand<object> operand);
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------

	public interface IHappilMethodBody<TReturn> : IHappilMethodBodyBase
	{
		void Return(IHappilOperand<TReturn> operand);
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------

	public interface IVoidHappilMethodBody : IHappilMethodBodyBase
	{
		void Return();
	}
}