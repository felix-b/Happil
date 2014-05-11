using System;
using System.Collections.Generic;
using Happil.Members;
using Happil.Statements;
using Happil.Writers;

namespace Happil.Operands
{
	internal class OperandCapture
	{
		private readonly List<MethodMember> m_ConsumerMethods;

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public OperandCapture(IScopedOperand sourceOperand, StatementBlock sourceOperandHome, MethodMember consumerMethod)
		{
			this.SourceOperand = sourceOperand;
			this.SourceOperandHome = sourceOperandHome;
			
			m_ConsumerMethods = new List<MethodMember>() {
				consumerMethod
			};
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public void HoistInClosure(ClosureDefinition closure)
		{
			this.HoistingClosure = closure;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public void DefineHoistedField(ClassWriterBase closureClassWriter)
		{
			this.HoistedField = closureClassWriter.DefineField(
				name: "<hoisted>" + this.Name,
				isStatic: false,
				isPublic: true,
				fieldType: this.OperandType);
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public override string ToString()
		{
			return (HoistedField != null ? HoistedField.ToString() : SourceOperand.ToString());
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public StatementBlock SourceOperandHome { get; private set; }
		public IScopedOperand SourceOperand { get; private set; }
		public FieldMember HoistedField { get; private set; }
		public ClosureDefinition HoistingClosure { get; private set; }

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public string Name
		{
			get
			{
				return SourceOperand.CaptureName;
			}
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public Type OperandType
		{
			get
			{
				return SourceOperand.OperandType;
			}
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodMember[] ConsumerMethods
		{
			get
			{
				return m_ConsumerMethods.ToArray();
			}
		}
	}
}