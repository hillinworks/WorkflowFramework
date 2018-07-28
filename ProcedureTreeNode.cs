﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Hillinworks.WorkflowFramework
{
	internal class ProcedureTreeNode
	{
		private object _context;

		public ProcedureTreeNode(Procedure procedure, Type inputType, Type outputType)
		{
			this.Procedure = procedure;
			this.InputType = inputType;
			this.OutputType = outputType;

			this.Procedure.Completed += this.OnProcedureCompleted;
			if (this.OutputType != null)
			{
				var outputInterface = (IProcedureOutput<object>) this.Procedure;
				outputInterface.Output += this.OnProcedureOutput;
			}
		}

		private List<ProcedureTreeNode> Successors { get; } = new List<ProcedureTreeNode>();

		private List<ProcedureTreeNode> ProductConsumers { get; } = new List<ProcedureTreeNode>();

		private IEnumerable<ProcedureTreeNode> Children => this.Successors.Union(this.ProductConsumers);
		private ProcedureTreeNode Parent { get; set; }

		private List<IPredecessorComplete> PredecessorCompleteHandlers { get; } = new List<IPredecessorComplete>();

		public Procedure Procedure { get; }
		public Type InputType { get; }
		public Type OutputType { get; }

		public bool IsCompleted { get; private set; }

		public object Context
		{
			get => _context ?? this.Parent?.Context;
			set => _context = value;
		}

		public event EventHandler Completed;

		internal void Start()
		{
			if (this.Procedure is IProductConsumerStartTime startTimeInterface)
			{
				if (startTimeInterface.StartTime == ProcedureStartTime.WhenWorkflowStarts)
				{
					this.Procedure.InternalStart();
				}
			}

			foreach (var child in this.Children)
			{
				child.Start();
			}
		}

		private void OnProcedureOutput(Procedure procedure, object output)
		{
			foreach (var consumer in this.ProductConsumers)
			{
				var startTimeInterface = (IProductConsumerStartTime) consumer.Procedure;
				if (startTimeInterface.StartTime == ProcedureStartTime.OnFirstInput && !consumer.Procedure.IsStarted)
				{
					consumer.Procedure.InternalStart();
				}

				consumer.Procedure.InvokeProcessInput(output);
			}
		}

		private void OnProcedureCompleted(object sender, EventArgs e)
		{
			foreach (var successor in this.Successors)
			{
				successor.Procedure.InternalStart();
			}

			foreach (var predecessorCompleteHandler in this.PredecessorCompleteHandlers)
			{
				predecessorCompleteHandler.OnPredecessorCompleted();
			}

			this.UpdateCompletion();
		}

		private void UpdateCompletion()
		{
			if (this.Procedure.IsCompleted && this.Children.All(c => c.IsCompleted))
			{
				this.IsCompleted = true;
				this.Completed?.Invoke(this, EventArgs.Empty);
			}
		}

		public void AddSuccessor(ProcedureTreeNode node)
		{
			node.Procedure.Predecessor = this.Procedure;
			this.Successors.Add(node);
			node.Parent = this;
			node.Completed += this.OnChildCompleted;
		}

		private void OnChildCompleted(object sender, EventArgs e)
		{
			this.UpdateCompletion();
		}

		public void AddProductConsumer(ProcedureTreeNode node)
		{
			if (this.OutputType == null)
			{
				throw new InvalidOperationException("this node does not have an output");
			}

			if (node.InputType == null)
			{
				throw new ArgumentException("specified node does not accept an input", nameof(node));
			}

			if (!node.InputType.IsAssignableFrom(this.OutputType))
			{
				throw new ArgumentException($"specified node does not accept an input of type {this.OutputType.Name}",
					nameof(node));
			}

			this.ProductConsumers.Add(node);
			node.Parent = this;
			node.Completed += this.OnChildCompleted;

			var predecessorCompleteInterface = node.Procedure as IPredecessorComplete;
			if (predecessorCompleteInterface != null)
			{
				this.PredecessorCompleteHandlers.Add(predecessorCompleteInterface);
			}
		}

		public void Initialize()
		{
			this.Procedure.Context = this.Context;
			this.Procedure.Initialize();

			foreach (var child in this.Children)
			{
				child.Initialize();
			}
		}
	}
}