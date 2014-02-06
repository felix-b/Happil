﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Happil.Expressions;
using Happil.Selectors;
using Happil.Statements;

namespace Happil.Fluent
{
	public interface IHappilMethodBodyBase
	{
		IHappilIfBody If(IHappilOperand<bool> condition);
		HappilOperand<T> Iif<T>(IHappilOperand<bool> condition, IHappilOperand<T> onTrue, IHappilOperand<T> onFalse);
		IHappilWhileSyntax While(IHappilOperand<bool> condition);
		HappilForShortSyntax For(HappilConstant<int> from, IHappilOperand<int> to, int increment = 1);
		HappilForShortSyntax For(IHappilOperand<int> from, HappilConstant<int> to, int increment = 1);
		HappilForShortSyntax For(IHappilOperand<int> from, IHappilOperand<int> to, int increment = 1);
		IHappilForWhileSyntax For(Action precondition);
		IHappilForeachInSyntax<T> Foreach<T>(HappilLocal<T> element);
		IHappilForeachDoSyntax<T> ForeachElementIn<T>(IHappilOperand<IEnumerable<T>> collection);
		IHappilUsingSyntax Using(IHappilOperand<IDisposable> disposable);
		IHappilLockSyntax Lock(IHappilOperand<object> syncRoot, int millisecondsTimeout);
		IHappilCatchSyntax Try(Action body);
		IHappilSwitchSyntax<T> Switch<T>(IHappilOperand<T> value);
		HappilOperand<TBase> This<TBase>();
		HappilLocal<T> Local<T>();
		HappilLocal<T> Local<T>(IHappilOperand<T> initialValue);
		HappilLocal<T> Local<T>(T initialValueConst);
		HappilConstant<T> Const<T>(T value);
		HappilOperand<T> Default<T>();
		IHappilOperand<TObject> New<TObject>(params IHappilOperand[] constructorArguments);
		IHappilOperand<TElement[]> NewArray<TElement>(IHappilOperand<int> length);
		void Throw<TException>(string message) where TException : Exception;
		void Throw();
		HappilOperand<Func<TArg1, TReturn>> Delegate<TArg1, TReturn>(Action<IHappilMethodBody<TReturn>, HappilArgument<TArg1>> body);
		HappilOperand<Func<TArg1, TReturn>> Delegate<TArg1, TReturn>(ref IHappilDelegate site, Action<IHappilMethodBody<TReturn>, HappilArgument<TArg1>> body);
		HappilOperand<TMethod> MakeDelegate<TTarget, TMethod>(IHappilOperand<TTarget> target, Expression<Func<TTarget, TMethod>> methodSelector);
		HappilOperand<Func<TArg1, TResult>> Lambda<TArg1, TResult>(Func<HappilOperand<TArg1>, IHappilOperand<TResult>> expression);
		HappilOperand<Func<TArg1, TResult>> Lambda<TArg1, TResult>(ref IHappilDelegate site, Func<HappilOperand<TArg1>, IHappilOperand<TResult>> expression);
		void EmitFromLambda(Expression<Action> lambda);
		HappilArgument<T> Argument<T>(string name);
		HappilArgument<T> Argument<T>(byte index);
		MethodInfo MethodInfo { get; }
		int ArgumentCount { get; }
		Type ReturnType { get; }
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------

	public interface IHappilMethodBodyTemplate : IHappilMethodBodyBase
	{
		void Return(IHappilOperand<TypeTemplate.TReturn> operand);
		void Return();
		IHappilAttributes ReturnAttributes { get; }
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------

	public interface IHappilMethodBody<TReturn> : IHappilMethodBodyBase
	{
		void Return(IHappilOperand<TReturn> operand);
		void ReturnConst(TReturn constantValue);
		IHappilAttributes ReturnAttributes { get; }
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------

	public interface IVoidHappilMethodBody : IHappilMethodBodyBase
	{
		void Return();
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------

	public interface IHappilConstructorBody : IHappilMethodBodyBase
	{
		void Base();
		void Base<TArg1>(IHappilOperand<TArg1> arg1);
		void Base<TArg1, TArg2>(IHappilOperand<TArg1> arg1, IHappilOperand<TArg2> arg2);
		void Base<TArg1, TArg2, TArg3>(IHappilOperand<TArg1> arg1, IHappilOperand<TArg2> arg2, IHappilOperand<TArg3> arg3);
		void This();
		void This<TArg1>(IHappilOperand<TArg1> arg1);
		void This<TArg1, TArg2>(IHappilOperand<TArg1> arg1, IHappilOperand<TArg2> arg2);
		void This<TArg1, TArg2, TArg3>(IHappilOperand<TArg1> arg1, IHappilOperand<TArg2> arg2, IHappilOperand<TArg3> arg3);
	}
}
