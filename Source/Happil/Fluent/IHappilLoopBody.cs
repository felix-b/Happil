﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Happil.Fluent
{
	public interface IHappilLoopBody
	{
		void Continue();
		void Break();
	}
}