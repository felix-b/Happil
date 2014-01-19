﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Happil.Statements
{
	internal class ThrowStatement : IHappilStatement
	{
		private readonly Type m_ExceptionType;
		private readonly string m_Message;
		private readonly ConstructorInfo m_Constructor;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public ThrowStatement(Type exceptionType, string message)
		{
			m_ExceptionType = exceptionType;
			m_Message = message;
			m_Constructor = exceptionType.GetConstructor(new[] { typeof(string) });

			if ( m_Constructor == null )
			{
				throw new ArgumentException("Could not find .ctor(string) on specified exception type.");
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		#region IHappilStatement Members

		public void Emit(ILGenerator il)
		{
			il.Emit(OpCodes.Ldstr, m_Message);
			il.Emit(OpCodes.Newobj, m_Constructor);
			il.Emit(OpCodes.Throw);
		}

		#endregion
	}
}