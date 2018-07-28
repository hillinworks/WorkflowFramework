﻿namespace Hillinworks.WorkflowFramework
{
	internal static partial class WorkflowBuilder
	{
		public class Initiator : IWorkflowInitiator
		{
			public Initiator(Workflow workflow)
			{
				this.Workflow = workflow;
			}

			private Workflow Workflow { get; }

			private object Context { get; set; }

			public IWorkflowInitiator SetContext(object context)
			{
				this.Context = context;
				return this;
			}

			public IWorkflowBuilder StartWith<TProcedure>()
				where TProcedure : Procedure, new()
			{
				return this.StartWith(new TProcedure());
			}

			public IWorkflowBuilder StartWith<TProcedure>(TProcedure procedure)
				where TProcedure : Procedure
			{
				var node = new ProcedureTreeNode(procedure, null, null)
				{
					Context = this.Context
				};
				this.Workflow.ProcedureTree = node;
				return new Builder(node);
			}

			public IWorkflowBuilder<TOutput> StartWith<TProcedure, TOutput>()
				where TProcedure : Procedure, IProcedureOutput<TOutput>, new()
			{
				return this.StartWith<TProcedure, TOutput>(new TProcedure());
			}

			public IWorkflowBuilder<TOutput> StartWith<TProcedure, TOutput>(TProcedure procedure)
				where TProcedure : Procedure, IProcedureOutput<TOutput>
			{
				var node = new ProcedureTreeNode(procedure, null, typeof(TOutput))
				{
					Context = this.Context
				};
				this.Workflow.ProcedureTree = node;
				return new Builder<TOutput>(node);
			}
		}
	}
}