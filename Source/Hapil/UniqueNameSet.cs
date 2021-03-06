﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Hapil
{
	internal class UniqueNameSet
	{
        private readonly object m_SyncRoot;
        private readonly HashSet<string> m_NamesInUse;
		private int m_UniqueNumericSuffix;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public UniqueNameSet()
		{
            m_SyncRoot = new object();
			m_NamesInUse = new HashSet<string>();
			m_UniqueNumericSuffix = 0;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public string TakeUniqueName(string proposedName, bool mustUseThisName = false)
		{
			string uniqueName;

		    lock ( m_SyncRoot )
		    {
		        if ( m_NamesInUse.Add(proposedName) || mustUseThisName )
		        {
		            uniqueName = proposedName;
		        }
		        else
		        {
		            uniqueName = proposedName + Interlocked.Increment(ref m_UniqueNumericSuffix);
		            m_NamesInUse.Add(uniqueName);
		        }
		    }

		    return uniqueName;
		}
	}
}
