﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Happil.Fluent;
using System.Reflection.Emit;

namespace Happil
{
	public class HappilFactory
	{
	    private AssemblyBuilder m_AssemblyBuilder ;
	    private ModuleBuilder m_ModuleBuilder;
        private IDictionary<string,HappilClass> dic= new Dictionary<string,HappilClass>();

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

	    public HappilFactory(string fileName, AssemblyBuilderAccess access = AssemblyBuilderAccess.RunAndSave)
	    {
	        m_AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(fileName), access);
	        m_ModuleBuilder = m_AssemblyBuilder.DefineDynamicModule("DefaultModule");
	    }

	    //-----------------------------------------------------------------------------------------------------------------------------------------------------
	    
        public IClass DefineClass(string classFullName,TypeAttributes attributes=TypeAttributes.Public |TypeAttributes.Public)
        {
            TypeBuilder tb = m_ModuleBuilder.DefineType(classFullName, attributes);
            return new HappilClass(tb);
        }
	}
}
