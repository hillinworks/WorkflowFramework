﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hillinworks.WorkflowFramework
{
	public interface IPredecessorComplete
	{
		void OnPredecessorCompleted();
	}
}
