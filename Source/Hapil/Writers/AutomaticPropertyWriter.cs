﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hapil.Members;

namespace Hapil.Writers
{
	public class AutomaticPropertyWriter : PropertyWriterBase
	{
		public AutomaticPropertyWriter(PropertyMember ownerProperty)
			: base(ownerProperty)
		{
			if ( OwnerProperty.GetterMethod != null )
			{
				var getter = new TemplateMethodWriter(
					OwnerProperty.GetterMethod,
					w => w.Return(OwnerProperty.BackingField.AsOperand<TypeTemplate.TReturn>()));
			}

			if ( OwnerProperty.SetterMethod != null )
			{
				var setter = new TemplateMethodWriter(
					OwnerProperty.SetterMethod,
					w => OwnerProperty.BackingField.AsOperand<TypeTemplate.TProperty>().Assign(w.Arg1<TypeTemplate.TProperty>()));
			}
		}
	}
}
